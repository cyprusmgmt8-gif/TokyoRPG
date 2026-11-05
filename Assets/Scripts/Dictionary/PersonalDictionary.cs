using System;
using System.Collections.Generic;
using UnityEngine;

public enum DictionaryFilter
{
    All,
    Recent,
    Revision
}

/// <summary>
/// The player's personal dictionary.
/// Put this on the same object as Vocabulary Library. It loads the local save when play starts.
/// </summary>
public class PersonalDictionary : MonoBehaviour
{
    public static PersonalDictionary Instance { get; private set; }

    public event Action Changed;

    /// <summary>
    /// Raised only when a word is saved for the first time.
    /// </summary>
    public event Action<string> WordDiscovered;

    [Tooltip("A word stays in the Recent filter for this many days after it is discovered.")]
    [SerializeField] int recentDays = 7;

    IDictionaryStore store;
    DictionarySaveData data = new DictionarySaveData();

    void Awake()
    {
        Instance = this;
        store = new LocalDictionaryStore();
        data = store.Load();

        if (GetComponent<DictionaryPanel>() == null)
        {
            gameObject.AddComponent<DictionaryPanel>();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool Contains(string vocabularyId)
    {
        return Find(vocabularyId) != null;
    }

    /// <summary>
    /// Saves a newly studied word. Returns false when that word is already saved.
    /// </summary>
    public bool Discover(string vocabularyId)
    {
        if (string.IsNullOrWhiteSpace(vocabularyId) || Contains(vocabularyId))
        {
            return false;
        }

        data.words.Add(new DictionaryWordProgress
        {
            vocabularyId = vocabularyId,
            discoveredAtUtc = DateTime.UtcNow.ToString("o"),
            needsRevision = true
        });
        Save();
        WordDiscovered?.Invoke(vocabularyId);
        return true;
    }

    public void SetNeedsRevision(string vocabularyId, bool needsRevision)
    {
        DictionaryWordProgress word = Find(vocabularyId);
        if (word == null || word.needsRevision == needsRevision)
        {
            return;
        }

        word.needsRevision = needsRevision;
        Save();
    }

    public List<DictionaryWordProgress> GetWords(DictionaryFilter filter)
    {
        List<DictionaryWordProgress> result = new List<DictionaryWordProgress>();
        DateTime recentCutoff = DateTime.UtcNow.AddDays(-Mathf.Max(1, recentDays));

        for (int i = 0; i < data.words.Count; i++)
        {
            DictionaryWordProgress word = data.words[i];
            if (word == null || string.IsNullOrWhiteSpace(word.vocabularyId))
            {
                continue;
            }

            if (filter == DictionaryFilter.Revision && !word.needsRevision)
            {
                continue;
            }

            if (filter == DictionaryFilter.Recent && !WasDiscoveredAfter(word, recentCutoff))
            {
                continue;
            }

            result.Add(word);
        }

        result.Sort(CompareNewestFirst);
        return result;
    }

    void Save()
    {
        store.Save(data);
        Changed?.Invoke();
    }

    DictionaryWordProgress Find(string vocabularyId)
    {
        for (int i = 0; i < data.words.Count; i++)
        {
            DictionaryWordProgress word = data.words[i];
            if (word != null && string.Equals(word.vocabularyId, vocabularyId, StringComparison.OrdinalIgnoreCase))
            {
                return word;
            }
        }

        return null;
    }

    static bool WasDiscoveredAfter(DictionaryWordProgress word, DateTime cutoffUtc)
    {
        if (!DateTime.TryParse(word.discoveredAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime discovered))
        {
            return false;
        }

        return discovered.ToUniversalTime() >= cutoffUtc;
    }

    static int CompareNewestFirst(DictionaryWordProgress a, DictionaryWordProgress b)
    {
        return string.Compare(b.discoveredAtUtc, a.discoveredAtUtc, StringComparison.Ordinal);
    }
}
