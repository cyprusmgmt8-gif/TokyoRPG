using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// What a reply does. The words the player says stay in the choice phrase.
/// </summary>
public static class DialogueChoiceActions
{
    public const string Next = "next";
    public const string End = "end";
    public const string Checkout = "checkout";
    public const string Summary = "summary";
}

/// <summary>
/// Line ids shared by dialogue catalogs. The register opens the bill line.
/// </summary>
public static class DialogueLineIds
{
    public const string RegisterBill = "bill";
    public const string Bag = "bag";
    public const string Payment = "payment";
    public const string EmptyBasket = "empty-basket";
    public const string PurchaseThanks = "purchase-thanks";
}

/// <summary>
/// One thing the player can say. Next shows another line. End closes the talk.
/// Checkout closes the talk and opens the existing checkout screen.
/// </summary>
[Serializable]
public class DialogueChoice
{
    public JapanesePhrase phrase = new JapanesePhrase();

    [Tooltip("next, end, or checkout.")]
    public string action = DialogueChoiceActions.Next;

    [Tooltip("Line id to show when the action is next.")]
    public string nextLineId;
}

/// <summary>
/// One thing the NPC says, plus the replies the player can choose.
/// </summary>
[Serializable]
public class DialogueLine
{
    [Tooltip("Id used by choices, for example welcome.")]
    public string id;

    public JapanesePhrase phrase = new JapanesePhrase();

    public List<DialogueChoice> choices = new List<DialogueChoice>();
}

/// <summary>
/// All lines for one NPC. The character model is separate from this asset.
/// </summary>
[CreateAssetMenu(fileName = "DialogueCatalog", menuName = "Tokyo RPG/Dialogue Catalog")]
public class DialogueCatalog : ScriptableObject
{
    [Tooltip("Id for this person, for example konbini.cashier.")]
    [SerializeField] string npcId = "";

    [SerializeField] string speakerJapanese = "";
    [SerializeField] string speakerReading = "";
    [SerializeField] string speakerRomaji = "";
    [SerializeField] string speakerEnglish = "";

    [Tooltip("Line id shown when the player starts talking.")]
    [SerializeField] string startLineId = "";

    [SerializeField] List<DialogueLine> lines = new List<DialogueLine>();

    public string NpcId => npcId ?? "";

    public string SpeakerJapanese => speakerJapanese ?? "";

    public string SpeakerReading => speakerReading ?? "";

    public string SpeakerRomaji => speakerRomaji ?? "";

    public string SpeakerEnglish => speakerEnglish ?? "";

    public string StartLineId => startLineId ?? "";

    public bool TryGet(string lineId, out DialogueLine line)
    {
        line = null;
        if (string.IsNullOrWhiteSpace(lineId) || lines == null)
        {
            return false;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            DialogueLine item = lines[i];
            if (item != null && string.Equals(item.id, lineId, StringComparison.OrdinalIgnoreCase))
            {
                line = item;
                return true;
            }
        }

        return false;
    }
}
