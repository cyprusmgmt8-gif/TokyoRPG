using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grammar lessons and the sentence notes used by study cards.
/// Add a lesson in the Inspector. The game screens only read this asset.
/// </summary>
[CreateAssetMenu(fileName = "GrammarCatalog", menuName = "Tokyo RPG/Grammar Catalog")]
public class GrammarCatalog : ScriptableObject
{
    [SerializeField] List<GrammarLesson> lessons = new List<GrammarLesson>();
    [SerializeField] List<GrammarSentenceNote> sentences = new List<GrammarSentenceNote>();

    public IReadOnlyList<GrammarLesson> Lessons => lessons;

    public bool TryGetLesson(string lessonId, out GrammarLesson lesson)
    {
        lesson = null;
        if (string.IsNullOrWhiteSpace(lessonId) || lessons == null)
        {
            return false;
        }

        for (int i = 0; i < lessons.Count; i++)
        {
            GrammarLesson item = lessons[i];
            if (item != null && string.Equals(item.id, lessonId, StringComparison.OrdinalIgnoreCase))
            {
                lesson = item;
                return true;
            }
        }

        return false;
    }

    public bool TryGetSentence(string vocabularyId, out GrammarSentenceNote note)
    {
        note = null;
        if (string.IsNullOrWhiteSpace(vocabularyId) || sentences == null)
        {
            return false;
        }

        for (int i = 0; i < sentences.Count; i++)
        {
            GrammarSentenceNote item = sentences[i];
            if (item != null && string.Equals(item.vocabularyId, vocabularyId, StringComparison.OrdinalIgnoreCase))
            {
                note = item;
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// One grammar point. The Japanese text stays here. The screens only display it.
/// </summary>
[Serializable]
public class GrammarLesson
{
    [Tooltip("Unique id, for example wa or masu.")]
    public string id;

    [Tooltip("Short title, for example The topic marker.")]
    public string title;

    [Tooltip("The grammar element, for example は.")]
    public string grammarElement;

    [Tooltip("Reading, when the written form is not how it is said. は as a particle is わ.")]
    public string reading;

    [TextArea(2, 5)]
    [Tooltip("What this grammar does. Keep it accurate, not a one-word English label.")]
    public string englishExplanation;

    [TextArea(2, 4)]
    [Tooltip("A shorter note for a new learner.")]
    public string beginnerDescription;

    [Tooltip("A natural Japanese example.")]
    public string exampleSentence;

    [Tooltip("Romaji for the example sentence.")]
    public string romaji;

    [Tooltip("English translation of the example.")]
    public string exampleTranslation;

    public List<GrammarBreakdownPart> breakdown = new List<GrammarBreakdownPart>();

    [Tooltip("1 is beginner.")]
    public int difficulty = 1;

    [Tooltip("Location id, for example bedroom.")]
    public string locationId = "bedroom";

    [Tooltip("Vocabulary ids that use this grammar, for example bedroom.bed.")]
    public List<string> relatedVocabularyIds = new List<string>();
}

/// <summary>
/// One piece of a sentence, shown in order.
/// </summary>
[Serializable]
public class GrammarBreakdownPart
{
    public string japanese;
    public string reading;
    public string meaning;
}

/// <summary>
/// Explains the grammar inside one vocabulary example sentence.
/// </summary>
[Serializable]
public class GrammarSentenceNote
{
    [Tooltip("Vocabulary id, for example bedroom.bed.")]
    public string vocabularyId;

    public string japaneseSentence;
    public string romaji;
    public string englishTranslation;

    [TextArea(2, 5)]
    public string explanation;

    public List<string> lessonIds = new List<string>();
    public List<GrammarBreakdownPart> breakdown = new List<GrammarBreakdownPart>();
}
