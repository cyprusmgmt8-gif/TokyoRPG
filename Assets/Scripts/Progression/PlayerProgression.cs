using System;
using UnityEngine;

/// <summary>
/// Tracks the player's XP and level.
/// Put this on the same object as Personal Dictionary. It loads the local save when play starts.
/// </summary>
public class PlayerProgression : MonoBehaviour
{
    public static PlayerProgression Instance { get; private set; }

    public event Action Changed;

    [Tooltip("XP given the first time a vocabulary word is discovered.")]
    [SerializeField] int xpPerDiscovery = 10;

    [Tooltip("XP given the first time a grammar lesson is opened. Opening it again does not add XP.")]
    [SerializeField] int xpPerGrammar = 10;

    [Tooltip("XP required to go up one level. Level 1 starts at 0 XP.")]
    [SerializeField] int xpPerLevel = 50;

    IProgressionStore store;
    PlayerProgressData data = new PlayerProgressData();

    public int XpPerDiscovery => Mathf.Max(0, xpPerDiscovery);

    public int XpPerGrammar => Mathf.Max(0, xpPerGrammar);

    public int XpPerLevel => Mathf.Max(1, xpPerLevel);

    public int TotalXp => data.totalXp;

    public int Level => 1 + data.totalXp / XpPerLevel;

    public int XpUntilNextLevel
    {
        get
        {
            int intoLevel = data.totalXp % XpPerLevel;
            return XpPerLevel - intoLevel;
        }
    }

    void Awake()
    {
        Instance = this;
        store = new LocalProgressionStore();
        data = store.Load();

        if (GetComponent<ProgressionHud>() == null)
        {
            gameObject.AddComponent<ProgressionHud>();
        }
    }

    void OnEnable()
    {
        if (PersonalDictionary.Instance != null)
        {
            PersonalDictionary.Instance.WordDiscovered += OnWordDiscovered;
        }

        if (GrammarProgress.Instance != null)
        {
            GrammarProgress.Instance.LessonDiscovered += OnLessonDiscovered;
        }

        if (RevisionProgress.Instance != null)
        {
            RevisionProgress.Instance.ReviewRewarded += OnReviewRewarded;
        }
    }

    void Start()
    {
        if (PersonalDictionary.Instance != null)
        {
            PersonalDictionary.Instance.WordDiscovered -= OnWordDiscovered;
            PersonalDictionary.Instance.WordDiscovered += OnWordDiscovered;
        }

        if (GrammarProgress.Instance != null)
        {
            GrammarProgress.Instance.LessonDiscovered -= OnLessonDiscovered;
            GrammarProgress.Instance.LessonDiscovered += OnLessonDiscovered;
        }

        if (RevisionProgress.Instance != null)
        {
            RevisionProgress.Instance.ReviewRewarded -= OnReviewRewarded;
            RevisionProgress.Instance.ReviewRewarded += OnReviewRewarded;
        }
    }

    void OnDisable()
    {
        if (PersonalDictionary.Instance != null)
        {
            PersonalDictionary.Instance.WordDiscovered -= OnWordDiscovered;
        }

        if (GrammarProgress.Instance != null)
        {
            GrammarProgress.Instance.LessonDiscovered -= OnLessonDiscovered;
        }

        if (RevisionProgress.Instance != null)
        {
            RevisionProgress.Instance.ReviewRewarded -= OnReviewRewarded;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void OnWordDiscovered(string vocabularyId)
    {
        if (string.IsNullOrWhiteSpace(vocabularyId) || XpPerDiscovery == 0)
        {
            return;
        }

        data.totalXp += XpPerDiscovery;
        store.Save(data);
        Changed?.Invoke();
    }

    void OnLessonDiscovered(string lessonId)
    {
        if (string.IsNullOrWhiteSpace(lessonId) || XpPerGrammar == 0)
        {
            return;
        }

        data.totalXp += XpPerGrammar;
        store.Save(data);
        Changed?.Invoke();
    }

    void OnReviewRewarded(int xp)
    {
        if (xp <= 0)
        {
            return;
        }

        data.totalXp += xp;
        store.Save(data);
        Changed?.Invoke();
    }

    /// <summary>
    /// Adds quest XP once the quest log has decided the reward is due.
    /// Discovery, grammar, and revision XP are unchanged.
    /// </summary>
    public void AwardQuestXp(int xp)
    {
        if (xp <= 0)
        {
            return;
        }

        data.totalXp += xp;
        store.Save(data);
        Changed?.Invoke();
    }
}
