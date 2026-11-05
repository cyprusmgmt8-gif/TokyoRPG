using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A list of Japanese words saved as a project asset.
/// Edit the asset in the Inspector. You do not need to change scripts to add a word.
/// </summary>
[CreateAssetMenu(fileName = "VocabularyCatalog", menuName = "Tokyo RPG/Vocabulary Catalog")]
public class VocabularyCatalog : ScriptableObject
{
    [SerializeField] List<VocabularyRecord> entries = new List<VocabularyRecord>();

    public IReadOnlyList<VocabularyRecord> Entries => entries;

    public bool TryResolve(string vocabularyId, string languageCode, out VocabularyEntry entry)
    {
        entry = null;
        VocabularyRecord record = FindRecord(vocabularyId);
        if (record == null)
        {
            return false;
        }

        string code = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.Trim();
        VocabularyTranslation translation = record.FindTranslation(code);
        if (translation == null && code != "en")
        {
            translation = record.FindTranslation("en");
            code = translation != null ? "en" : code;
        }

        entry = new VocabularyEntry
        {
            id = record.id,
            japanese = record.japanese,
            reading = record.reading,
            exampleSentence = record.exampleSentence,
            exampleReading = record.exampleReading,
            audioReference = record.audioReference,
            wordClip = record.wordClip,
            exampleClip = record.exampleClip,
            roomId = record.roomId,
            objectId = record.objectId,
            translation = translation != null ? translation.translation : string.Empty,
            exampleTranslation = translation != null ? translation.exampleTranslation : string.Empty,
            grammarExplanation = translation != null ? translation.grammarExplanation : string.Empty,
            languageCode = code
        };
        return true;
    }

    VocabularyRecord FindRecord(string vocabularyId)
    {
        if (string.IsNullOrWhiteSpace(vocabularyId))
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            VocabularyRecord record = entries[i];
            if (record != null && string.Equals(record.id, vocabularyId, StringComparison.OrdinalIgnoreCase))
            {
                return record;
            }
        }

        return null;
    }
}

/// <summary>
/// One word, plus a translation block for each interface language.
/// Japanese text stays here. Translations live in the translations list.
/// </summary>
[Serializable]
public class VocabularyRecord
{
    [Tooltip("Unique id, for example bedroom.bed. Objects use this id to find the word.")]
    public string id;

    [Tooltip("The word in Japanese. Use kanji when the word is normally written with kanji.")]
    public string japanese;

    [Tooltip("Hiragana reading of the word.")]
    public string reading;

    [TextArea(2, 3)]
    [Tooltip("Example sentence in Japanese.")]
    public string exampleSentence;

    [TextArea(2, 3)]
    [Tooltip("Hiragana reading of the example sentence. Leave empty when the sentence is already in hiragana or katakana.")]
    public string exampleReading;

    [Tooltip("Reminder of the file name, for example bedroom_bed. The clips below are what the game plays.")]
    public string audioReference;

    [Tooltip("Recording of this Japanese word. Drag a wav, mp3, or ogg file here.")]
    public AudioClip wordClip;

    [Tooltip("Recording of the example sentence. Drag a wav, mp3, or ogg file here.")]
    public AudioClip exampleClip;

    [Tooltip("Room id, for example bedroom.")]
    public string roomId;

    [Tooltip("Object id inside that room, for example bed.")]
    public string objectId;

    [Tooltip("One block per interface language. English uses language code en.")]
    public List<VocabularyTranslation> translations = new List<VocabularyTranslation>();

    public VocabularyTranslation FindTranslation(string languageCode)
    {
        if (translations == null)
        {
            return null;
        }

        for (int i = 0; i < translations.Count; i++)
        {
            VocabularyTranslation translation = translations[i];
            if (translation != null && string.Equals(translation.languageCode, languageCode, StringComparison.OrdinalIgnoreCase))
            {
                return translation;
            }
        }

        return null;
    }
}

/// <summary>
/// The parts of a word that change when the interface language changes.
/// </summary>
[Serializable]
public class VocabularyTranslation
{
    [Tooltip("Language code. English is en. A later language can be es, zh, or ko.")]
    public string languageCode = "en";

    [Tooltip("Meaning of the word in this language.")]
    public string translation;

    [TextArea(2, 3)]
    [Tooltip("Example sentence in this language.")]
    public string exampleTranslation;

    [TextArea(2, 4)]
    [Tooltip("Optional note about the grammar. Leave empty when there is nothing extra to explain.")]
    public string grammarExplanation;
}
