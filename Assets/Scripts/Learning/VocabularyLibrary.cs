using UnityEngine;

/// <summary>
/// Points the game at the vocabulary catalog and the chosen interface language.
/// Put this on the player. Assign both assets in the Inspector.
/// </summary>
public class VocabularyLibrary : MonoBehaviour
{
    public static VocabularyLibrary Instance { get; private set; }

    [Tooltip("The asset that stores the words. Bedroom words are in Assets/Data/Vocabulary.")]
    [SerializeField] VocabularyCatalog catalog;

    [Tooltip("The asset that chooses the translation language. It is in Assets/Data/Localization.")]
    [SerializeField] InterfaceLanguage interfaceLanguage;

    public string LanguageCode
    {
        get
        {
            string fallback = interfaceLanguage != null ? interfaceLanguage.LanguageCode : "en";
            return PlayerProfile.CurrentLanguageCode(fallback);
        }
    }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool TryGet(string vocabularyId, out VocabularyEntry entry)
    {
        entry = null;
        if (catalog == null)
        {
            return false;
        }

        return catalog.TryResolve(vocabularyId, LanguageCode, out entry);
    }
}
