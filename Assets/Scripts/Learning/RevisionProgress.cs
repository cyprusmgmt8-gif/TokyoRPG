using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public enum RevisionRating
{
    Again = 1,
    Hard = 2,
    Good = 3,
    Easy = 4
}

public enum RevisionStatus
{
    New = 0,
    Learning = 1,
    Familiar = 2,
    Mastered = 3
}

/// <summary>
/// Schedule for one discovered word. The Japanese text stays in the vocabulary catalog.
/// </summary>
[Serializable]
public class RevisionCardProgress
{
    public string vocabularyId;
    public int attempts;
    public int successfulReviews;
    public int incorrectReviews;
    public int successStreak;
    public string lastReviewedUtc;
    public string nextReviewUtc;
    public int status;
    public float intervalDays;
    public float ease = 2.5f;
    public string lastRewardLocalDate;
    public int lastRating;
}

[Serializable]
public class RevisionSaveData
{
    public int version = 2;
    public string sessionRewardLocalDate;
    public string statsLocalDate;
    public int correctToday;
    public int incorrectToday;
    public int xpToday;
    public List<RevisionCardProgress> cards = new List<RevisionCardProgress>();
}

public interface IRevisionStore
{
    RevisionSaveData Load();
    void Save(RevisionSaveData data);
}

public class LocalRevisionStore : IRevisionStore
{
    public const string FileName = "tokyo-rpg-revision.json";

    public static string SavePath => SaveSlots.FilePath(FileName);

    public RevisionSaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            return new RevisionSaveData();
        }

        try
        {
            RevisionSaveData data = JsonUtility.FromJson<RevisionSaveData>(File.ReadAllText(SavePath));
            if (data == null)
            {
                return new RevisionSaveData();
            }

            if (data.cards == null)
            {
                data.cards = new List<RevisionCardProgress>();
            }

            return data;
        }
        catch (Exception)
        {
            return new RevisionSaveData();
        }
    }

    public void Save(RevisionSaveData data)
    {
        if (data == null)
        {
            data = new RevisionSaveData();
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }
}

/// <summary>
/// Remembers when each discovered word should be revised.
/// Put this on the same object as Personal Dictionary. It does not change the dictionary file.
/// </summary>
public class RevisionProgress : MonoBehaviour
{
    public const int AgainXp = 0;
    public const int HardXp = 2;
    public const int GoodXp = 3;
    public const int EasyXp = 4;
    public const int SessionXp = 5;
    public const int DailyXpCap = 40;
    const float MaxIntervalDays = 30f;

    public static RevisionProgress Instance { get; private set; }

    public event Action Changed;

    /// <summary>
    /// Raised when a review awards XP. The amount is already limited to one reward per word per day.
    /// </summary>
    public event Action<int> ReviewRewarded;

    IRevisionStore store;
    RevisionSaveData data = new RevisionSaveData();

    void Awake()
    {
        Instance = this;
        store = new LocalRevisionStore();
        data = store.Load();
        MigrateIfNeeded();
        RollDay();

        if (GetComponent<RevisionPanel>() == null)
        {
            gameObject.AddComponent<RevisionPanel>();
        }
    }

    void Start()
    {
        if (PersonalDictionary.Instance != null)
        {
            PersonalDictionary.Instance.WordDiscovered -= OnWordDiscovered;
            PersonalDictionary.Instance.WordDiscovered += OnWordDiscovered;
        }

        SyncDiscoveredWords();
    }

    void OnDestroy()
    {
        if (PersonalDictionary.Instance != null)
        {
            PersonalDictionary.Instance.WordDiscovered -= OnWordDiscovered;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public int DiscoveredCount => PersonalDictionary.Instance == null ? 0 : PersonalDictionary.Instance.GetWords(DictionaryFilter.All).Count;

    public int DueCount
    {
        get
        {
            SyncDiscoveredWords();
            int count = 0;
            List<DictionaryWordProgress> words = DiscoveredWords();
            for (int i = 0; i < words.Count; i++)
            {
                RevisionCardProgress card = Find(words[i].vocabularyId);
                if (card != null && IsDue(card))
                {
                    count++;
                }
            }

            return count;
        }
    }

    public int ReviewedTodayCount
    {
        get
        {
            string today = LocalToday();
            int count = 0;
            for (int i = 0; i < data.cards.Count; i++)
            {
                RevisionCardProgress card = data.cards[i];
                if (card != null && LocalDate(card.lastReviewedUtc) == today)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public int NeedsAttentionCount => CountWhere(IsDifficult);

    public int MasteredCount => CountWhere(card => card.status == (int)RevisionStatus.Mastered);

    public int CorrectToday
    {
        get
        {
            RollDay();
            return data.correctToday;
        }
    }

    public int IncorrectToday
    {
        get
        {
            RollDay();
            return data.incorrectToday;
        }
    }

    public int XpToday
    {
        get
        {
            RollDay();
            return data.xpToday;
        }
    }

    public List<string> GetSessionIds(bool dueOnly)
    {
        return CollectIds(card => !dueOnly || IsDue(card));
    }

    public List<string> GetDifficultIds()
    {
        return CollectIds(IsDifficult);
    }

    public bool IsDue(string vocabularyId)
    {
        RevisionCardProgress card = Find(vocabularyId);
        return card != null && IsDue(card);
    }

    public RevisionCardProgress GetCard(string vocabularyId)
    {
        return Find(vocabularyId);
    }

    /// <summary>
    /// Updates the schedule. Returns the XP awarded, which is 0 when this word was already rewarded today,
    /// the answer was Again, or the word was not due.
    /// </summary>
    public int Rate(string vocabularyId, RevisionRating rating, bool wasDue)
    {
        RevisionCardProgress card = Find(vocabularyId);
        if (card == null)
        {
            return 0;
        }

        RollDay();
        string previousDay = LocalDate(card.lastReviewedUtc);
        int previousRating = card.lastRating;
        bool repeatedSuccess = rating != RevisionRating.Again
            && previousDay == LocalToday()
            && previousRating != (int)RevisionRating.Again
            && previousRating != 0;
        if (repeatedSuccess)
        {
            card.attempts++;
            card.lastReviewedUtc = DateTime.UtcNow.ToString("o");
            data.correctToday++;
            Save();
            return 0;
        }

        card.attempts++;
        card.lastReviewedUtc = DateTime.UtcNow.ToString("o");
        card.lastRating = (int)rating;
        ApplySchedule(card, rating, previousRating);
        if (rating == RevisionRating.Again)
        {
            data.incorrectToday++;
        }
        else
        {
            data.correctToday++;
        }

        int xp = 0;
        string today = LocalToday();
        if (wasDue && rating != RevisionRating.Again && card.lastRewardLocalDate != today)
        {
            xp = Mathf.Min(XpFor(rating), Mathf.Max(0, DailyXpCap - data.xpToday));
            if (xp > 0)
            {
                card.lastRewardLocalDate = today;
                data.xpToday += xp;
            }
        }

        Save();
        if (xp > 0)
        {
            ReviewRewarded?.Invoke(xp);
        }

        return xp;
    }

    /// <summary>
    /// Awards the once-per-day bonus after every due word in the session has been marked.
    /// </summary>
    public int TrySessionBonus(int ratedDueCount, int dueAtStart)
    {
        if (dueAtStart <= 0 || ratedDueCount < dueAtStart)
        {
            return 0;
        }

        string today = LocalToday();
        if (data.sessionRewardLocalDate == today)
        {
            return 0;
        }

        RollDay();
        int xp = Mathf.Min(SessionXp, Mathf.Max(0, DailyXpCap - data.xpToday));
        data.sessionRewardLocalDate = LocalToday();
        if (xp > 0)
        {
            data.xpToday += xp;
        }

        Save();
        if (xp > 0)
        {
            ReviewRewarded?.Invoke(xp);
        }

        return xp;
    }

    public string DescribeWait(string vocabularyId)
    {
        RevisionCardProgress card = Find(vocabularyId);
        if (card == null || string.IsNullOrEmpty(card.nextReviewUtc))
        {
            return "Due now";
        }

        if (!TryParseUtc(card.nextReviewUtc, out DateTime next))
        {
            return "Due now";
        }

        TimeSpan wait = next - DateTime.UtcNow;
        if (wait.TotalSeconds <= 5)
        {
            return "Due now";
        }

        if (wait.TotalMinutes < 2)
        {
            return "Back in 1 minute";
        }

        if (wait.TotalHours < 1)
        {
            return "Back in " + Mathf.CeilToInt((float)wait.TotalMinutes) + " minutes";
        }

        if (wait.TotalDays < 1)
        {
            return "Back later today";
        }

        int days = Mathf.Max(1, Mathf.RoundToInt((float)wait.TotalDays));
        return days == 1 ? "Back tomorrow" : "Back in " + days + " days";
    }

    public static string StatusLabel(int status)
    {
        switch ((RevisionStatus)status)
        {
            case RevisionStatus.Learning:
                return "Learning";
            case RevisionStatus.Familiar:
                return "Familiar";
            case RevisionStatus.Mastered:
                return "Mastered";
            default:
                return "New";
        }
    }

    public static bool IsDifficult(RevisionCardProgress card)
    {
        if (card == null || card.attempts <= 0)
        {
            return false;
        }

        return card.status == (int)RevisionStatus.Learning
            || card.lastRating == (int)RevisionRating.Again
            || card.incorrectReviews > card.successfulReviews;
    }

    void OnWordDiscovered(string vocabularyId)
    {
        if (EnsureCard(vocabularyId))
        {
            Save();
        }
    }

    void SyncDiscoveredWords()
    {
        bool changed = false;
        List<DictionaryWordProgress> words = DiscoveredWords();
        for (int i = 0; i < words.Count; i++)
        {
            if (EnsureCard(words[i].vocabularyId))
            {
                changed = true;
            }
        }

        if (changed)
        {
            Save();
        }
    }

    bool EnsureCard(string vocabularyId)
    {
        if (string.IsNullOrWhiteSpace(vocabularyId) || Find(vocabularyId) != null)
        {
            return false;
        }

        data.cards.Add(new RevisionCardProgress
        {
            vocabularyId = vocabularyId,
            nextReviewUtc = DateTime.UtcNow.ToString("o"),
            status = (int)RevisionStatus.New,
            ease = 2.5f
        });
        return true;
    }

    static void ApplySchedule(RevisionCardProgress card, RevisionRating rating, int previousRating)
    {
        bool recovering = previousRating == (int)RevisionRating.Again;
        if (rating == RevisionRating.Again)
        {
            card.incorrectReviews++;
            card.successStreak = 0;
            card.intervalDays = card.intervalDays <= 1f ? 1f : card.intervalDays * 0.5f;
            card.status = (int)RevisionStatus.Learning;
            card.nextReviewUtc = DateTime.UtcNow.AddMinutes(1).ToString("o");
            return;
        }

        card.successfulReviews++;
        card.successStreak++;
        float startingGap = rating == RevisionRating.Easy ? 7f : rating == RevisionRating.Good ? 3f : 1f;
        if (recovering)
        {
            card.intervalDays = 1f;
        }
        else if (card.intervalDays < startingGap)
        {
            card.intervalDays = startingGap;
        }
        else
        {
            float multiplier = rating == RevisionRating.Easy ? 2f : rating == RevisionRating.Good ? 1.6f : 1.4f;
            card.intervalDays = Mathf.Min(MaxIntervalDays, card.intervalDays * multiplier);
        }

        card.intervalDays = Mathf.Min(MaxIntervalDays, card.intervalDays);
        card.nextReviewUtc = DateTime.UtcNow.AddDays(card.intervalDays).ToString("o");
        RecomputeStatus(card);
    }

    static void RecomputeStatus(RevisionCardProgress card)
    {
        if (card.attempts <= 0)
        {
            card.status = (int)RevisionStatus.New;
            return;
        }

        if (card.lastRating == (int)RevisionRating.Again || card.successfulReviews < 2)
        {
            card.status = (int)RevisionStatus.Learning;
            return;
        }

        if (card.successfulReviews >= 4
            && card.successStreak >= 3
            && card.intervalDays >= 7f
            && card.successfulReviews > card.incorrectReviews)
        {
            card.status = (int)RevisionStatus.Mastered;
            return;
        }

        if (card.intervalDays >= 3f)
        {
            card.status = (int)RevisionStatus.Familiar;
            return;
        }

        card.status = (int)RevisionStatus.Learning;
    }

    void MigrateIfNeeded()
    {
        if (data.version >= 2)
        {
            return;
        }

        for (int i = 0; i < data.cards.Count; i++)
        {
            RevisionCardProgress card = data.cards[i];
            if (card == null)
            {
                continue;
            }

            if (card.status == 3)
            {
                card.status = (int)RevisionStatus.Familiar;
            }

            if (card.lastRating == (int)RevisionRating.Good || card.lastRating == (int)RevisionRating.Easy)
            {
                card.successStreak = Mathf.Min(card.successfulReviews, 3);
            }

            RecomputeStatus(card);
        }

        data.version = 2;
        store.Save(data);
    }

    void RollDay()
    {
        string today = LocalToday();
        if (data.statsLocalDate == today)
        {
            return;
        }

        data.statsLocalDate = today;
        data.correctToday = 0;
        data.incorrectToday = 0;
        data.xpToday = 0;
        store.Save(data);
    }

    List<string> CollectIds(Func<RevisionCardProgress, bool> include)
    {
        SyncDiscoveredWords();
        List<RevisionCardProgress> cards = new List<RevisionCardProgress>();
        List<DictionaryWordProgress> words = DiscoveredWords();
        for (int i = 0; i < words.Count; i++)
        {
            RevisionCardProgress card = Find(words[i].vocabularyId);
            if (card != null && include(card))
            {
                cards.Add(card);
            }
        }

        cards.Sort(CompareForSession);
        List<string> ids = new List<string>();
        for (int i = 0; i < cards.Count; i++)
        {
            ids.Add(cards[i].vocabularyId);
        }

        return ids;
    }

    int CountWhere(Func<RevisionCardProgress, bool> include)
    {
        int count = 0;
        List<DictionaryWordProgress> words = DiscoveredWords();
        for (int i = 0; i < words.Count; i++)
        {
            RevisionCardProgress card = Find(words[i].vocabularyId);
            if (card != null && include(card))
            {
                count++;
            }
        }

        return count;
    }

    static int XpFor(RevisionRating rating)
    {
        switch (rating)
        {
            case RevisionRating.Hard:
                return HardXp;
            case RevisionRating.Good:
                return GoodXp;
            case RevisionRating.Easy:
                return EasyXp;
            default:
                return AgainXp;
        }
    }

    static bool IsDue(RevisionCardProgress card)
    {
        if (card == null || string.IsNullOrEmpty(card.nextReviewUtc))
        {
            return true;
        }

        if (!TryParseUtc(card.nextReviewUtc, out DateTime next))
        {
            return true;
        }

        return next <= DateTime.UtcNow;
    }

    static int CompareForSession(RevisionCardProgress a, RevisionCardProgress b)
    {
        if (a.status == (int)RevisionStatus.Learning && b.status != (int)RevisionStatus.Learning)
        {
            return -1;
        }

        if (b.status == (int)RevisionStatus.Learning && a.status != (int)RevisionStatus.Learning)
        {
            return 1;
        }

        return string.Compare(a.nextReviewUtc, b.nextReviewUtc, StringComparison.Ordinal);
    }

    static List<DictionaryWordProgress> DiscoveredWords()
    {
        if (PersonalDictionary.Instance == null)
        {
            return new List<DictionaryWordProgress>();
        }

        return PersonalDictionary.Instance.GetWords(DictionaryFilter.All);
    }

    RevisionCardProgress Find(string vocabularyId)
    {
        for (int i = 0; i < data.cards.Count; i++)
        {
            RevisionCardProgress card = data.cards[i];
            if (card != null && string.Equals(card.vocabularyId, vocabularyId, StringComparison.OrdinalIgnoreCase))
            {
                return card;
            }
        }

        return null;
    }

    void Save()
    {
        store.Save(data);
        Changed?.Invoke();
    }

    static string LocalToday()
    {
        return DateTime.Now.ToString("yyyy-MM-dd");
    }

    static string LocalDate(string utcText)
    {
        if (!TryParseUtc(utcText, out DateTime utc))
        {
            return "";
        }

        return utc.ToLocalTime().ToString("yyyy-MM-dd");
    }

    static bool TryParseUtc(string text, out DateTime utc)
    {
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out utc))
        {
            utc = utc.ToUniversalTime();
            return true;
        }

        utc = default;
        return false;
    }
}
