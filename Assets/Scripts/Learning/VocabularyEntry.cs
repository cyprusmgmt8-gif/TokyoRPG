using UnityEngine;

/// <summary>
/// The vocabulary for one word, already chosen for the current interface language.
/// This is what the learning panel shows. The saved content lives in a vocabulary catalog asset.
/// </summary>
public class VocabularyEntry
{
    public string id;
    public string japanese;
    public string reading;
    public string translation;
    public string exampleSentence;
    public string exampleReading;
    public string exampleTranslation;
    public string audioReference;
    public AudioClip wordClip;
    public AudioClip exampleClip;
    public string roomId;
    public string objectId;
    public string grammarExplanation;
    public string languageCode;
}
