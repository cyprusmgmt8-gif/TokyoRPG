using UnityEngine;

/// <summary>
/// Put this on any object the player can study.
/// The object itself, or one of its children, needs a Collider so the look ray can hit it.
/// No new code is required for the next room. Add this component and type a vocabulary id
/// that already exists in the vocabulary catalog.
/// </summary>
public class InteractableObject : MonoBehaviour
{
    [Header("Prompt")]
    [Tooltip("Short name shown in the prompt, such as Bed or Door.")]
    [SerializeField] string promptName = "Object";

    [Tooltip("Warm tint used while the player is looking at this object.")]
    [SerializeField] Color highlightTint = new Color(1f, 0.82f, 0.45f, 1f);

    [Header("Vocabulary")]
    [Tooltip("Id from the vocabulary catalog, for example bedroom.bed. The Japanese text is not stored on this object.")]
    [SerializeField] string vocabularyId = "bedroom.bed";

    public string PromptName => string.IsNullOrWhiteSpace(promptName) ? name : promptName;
    public string VocabularyId => vocabularyId;

    public bool TryGetEntry(out VocabularyEntry entry)
    {
        entry = null;
        if (VocabularyLibrary.Instance == null)
        {
            return false;
        }

        return VocabularyLibrary.Instance.TryGet(vocabularyId, out entry);
    }

    Renderer[] renderers;
    MaterialPropertyBlock propertyBlock;
    bool focused;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        propertyBlock = new MaterialPropertyBlock();

        // Static batching would glue these shapes together and block a per-object highlight.
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].gameObject.isStatic = false;
            }
        }
    }

    public void SetFocused(bool isFocused)
    {
        if (focused == isFocused)
        {
            return;
        }

        focused = isFocused;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (isFocused)
            {
                propertyBlock.SetColor("_BaseColor", highlightTint);
                propertyBlock.SetColor("_Color", highlightTint);
                renderer.SetPropertyBlock(propertyBlock);
            }
            else
            {
                renderer.SetPropertyBlock(null);
            }
        }
    }
}
