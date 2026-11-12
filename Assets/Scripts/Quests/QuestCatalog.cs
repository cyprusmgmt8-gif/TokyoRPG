using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The kinds of objective a quest can ask for.
/// Only Interact is used so far. The other names are reserved for later quests.
/// </summary>
public static class QuestObjectiveTypes
{
    public const string Interact = "interact";
    public const string Talk = "talk";
    public const string Visit = "visit";
    public const string Conversation = "conversation";
}

/// <summary>
/// One step inside a quest. The Japanese learning text stays in the vocabulary catalog.
/// This only says which step it is and what finishes it.
/// </summary>
[Serializable]
public class QuestObjectiveDefinition
{
    [Tooltip("Id of this step inside the quest, for example water.")]
    public string id;

    [Tooltip("interact, or a later type such as talk, visit, or conversation.")]
    public string type = QuestObjectiveTypes.Interact;

    [Tooltip("For an interact step, the vocabulary id the player must study.")]
    public string targetId;

    [Tooltip("Short name shown on the tracker, such as Water.")]
    public string label;
}

/// <summary>
/// One quest definition. Player progress is saved separately.
/// </summary>
[Serializable]
public class QuestDefinition
{
    [Tooltip("Unique id, for example konbini.first-shopping.")]
    public string id;

    public string japanese;
    public string reading;
    public string romaji;
    public string english;

    [TextArea(2, 4)]
    public string description;

    [Tooltip("XP paid once when the quest is completed.")]
    public int rewardXp;

    [Tooltip("Offer this quest when the scene starts, if it is not already active or completed.")]
    public bool offerOnSceneStart;

    public List<QuestObjectiveDefinition> objectives = new List<QuestObjectiveDefinition>();
}

/// <summary>
/// The list of quests for a place. Edit the asset in the Inspector.
/// </summary>
[CreateAssetMenu(fileName = "QuestCatalog", menuName = "Tokyo RPG/Quest Catalog")]
public class QuestCatalog : ScriptableObject
{
    [SerializeField] List<QuestDefinition> quests = new List<QuestDefinition>();

    public IReadOnlyList<QuestDefinition> Quests => quests;

    public bool TryGet(string questId, out QuestDefinition quest)
    {
        quest = null;
        if (string.IsNullOrWhiteSpace(questId) || quests == null)
        {
            return false;
        }

        for (int i = 0; i < quests.Count; i++)
        {
            QuestDefinition item = quests[i];
            if (item != null && string.Equals(item.id, questId, StringComparison.OrdinalIgnoreCase))
            {
                quest = item;
                return true;
            }
        }

        return false;
    }
}
