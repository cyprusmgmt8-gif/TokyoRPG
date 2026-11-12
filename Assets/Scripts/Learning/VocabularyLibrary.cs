using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Points the game at the vocabulary catalogs and the chosen interface language.
/// Put this on the player. The primary catalog is searched first. Additional catalogs are searched after it.
/// </summary>
public class VocabularyLibrary : MonoBehaviour
{
    public static VocabularyLibrary Instance { get; private set; }

    [Tooltip("The first catalog to search. Bedroom words stay in Assets/Data/Vocabulary/BedroomVocabulary.")]
    [SerializeField] VocabularyCatalog catalog;

    [Tooltip("The asset that chooses the translation language. It is in Assets/Data/Localization.")]
    [SerializeField] InterfaceLanguage interfaceLanguage;

    [Tooltip("Catalogs searched after the primary catalog. The first matching id wins. Empty slots are skipped.")]
    [SerializeField] List<VocabularyCatalog> additionalCatalogs = new List<VocabularyCatalog>();

    [Tooltip("Spoken phrases searched after the word catalogs. Any NPC can share a phrase id.")]
    [SerializeField] PhraseCatalog phraseCatalog;

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
        string languageCode = LanguageCode;
        if (catalog != null && catalog.TryResolve(vocabularyId, languageCode, out entry))
        {
            return true;
        }

        if (additionalCatalogs != null)
        {
            for (int i = 0; i < additionalCatalogs.Count; i++)
            {
                VocabularyCatalog extra = additionalCatalogs[i];
                if (extra != null && extra.TryResolve(vocabularyId, languageCode, out entry))
                {
                    return true;
                }
            }
        }

        if (phraseCatalog != null && phraseCatalog.TryResolve(vocabularyId, out entry))
        {
            return true;
        }

        entry = null;
        return false;
    }
}
