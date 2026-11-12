using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spoken phrases that can be saved to the dictionary.
/// The Japanese text lives here once. Dialogue lines only store the phrase id.
/// Any NPC can use the same id, and the player still gets one dictionary entry.
/// </summary>
[CreateAssetMenu(fileName = "PhraseCatalog", menuName = "Tokyo RPG/Phrase Catalog")]
public class PhraseCatalog : ScriptableObject
{
    [SerializeField] List<PhraseRecord> phrases = new List<PhraseRecord>();

    public bool TryResolve(string phraseId, out VocabularyEntry entry)
    {
        entry = null;
        PhraseRecord record = Find(phraseId);
        if (record == null)
        {
            return false;
        }

        entry = new VocabularyEntry
        {
            id = record.id,
            japanese = record.japanese,
            reading = record.reading,
            romaji = record.romaji,
            translation = record.english,
            languageCode = "en"
        };
        return true;
    }

    PhraseRecord Find(string phraseId)
    {
        if (string.IsNullOrWhiteSpace(phraseId) || phrases == null)
        {
            return null;
        }

        for (int i = 0; i < phrases.Count; i++)
        {
            PhraseRecord record = phrases[i];
            if (record != null && string.Equals(record.id, phraseId, StringComparison.OrdinalIgnoreCase))
            {
                return record;
            }
        }

        return null;
    }
}

/// <summary>
/// One spoken phrase. English is the meaning shown in the dictionary and revision cards.
/// </summary>
[Serializable]
public class PhraseRecord
{
    [Tooltip("Stable id, for example phrase.konnichiwa.")]
    public string id;

    [TextArea(1, 3)]
    public string japanese;

    [Tooltip("Hiragana or katakana reading. Required when Japanese contains kanji.")]
    [TextArea(1, 3)]
    public string reading;

    [Tooltip("Hepburn romaji. Write wa for は and o for を.")]
    [TextArea(1, 3)]
    public string romaji;

    [TextArea(1, 3)]
    public string english;
}
