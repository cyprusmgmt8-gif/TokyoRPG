using UnityEngine;

/// <summary>
/// Points the game at the grammar catalog.
/// Put this on the player and assign the catalog in the Inspector.
/// </summary>
public class GrammarLibrary : MonoBehaviour
{
    public static GrammarLibrary Instance { get; private set; }

    [Tooltip("The asset that stores the lessons. Bedroom lessons are in Assets/Data/Grammar.")]
    [SerializeField] GrammarCatalog catalog;

    public GrammarCatalog Catalog => catalog;

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

    public bool TryGetLesson(string lessonId, out GrammarLesson lesson)
    {
        lesson = null;
        return catalog != null && catalog.TryGetLesson(lessonId, out lesson);
    }

    public bool TryGetSentence(string vocabularyId, out GrammarSentenceNote note)
    {
        note = null;
        return catalog != null && catalog.TryGetSentence(vocabularyId, out note);
    }
}
