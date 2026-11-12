using UnityEngine;

/// <summary>
/// Marks a person the player can talk to.
/// This does not open a study card and it does not start checkout by itself.
/// </summary>
public class DialoguePoint : MonoBehaviour
{
    [Tooltip("The conversation for this person. Another NPC uses a different asset.")]
    [SerializeField] DialogueCatalog catalog;

    public DialogueCatalog Catalog => catalog;
}
