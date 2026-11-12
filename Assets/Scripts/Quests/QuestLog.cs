using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum QuestState
{
    NotStarted = 0,
    Active = 1,
    Completed = 2
}

/// <summary>
/// Saved progress for one objective. The wording stays in the quest catalog.
/// </summary>
[Serializable]
public class QuestObjectiveProgress
{
    public string objectiveId;
    public bool complete;
}

/// <summary>
/// Saved progress for one quest.
/// </summary>
[Serializable]
public class QuestProgressRecord
{
    public string questId;
    public int state;
    public bool rewardGranted;
    public List<QuestObjectiveProgress> objectives = new List<QuestObjectiveProgress>();
}

[Serializable]
public class QuestSaveData
{
    public int version = 1;
    public List<QuestProgressRecord> quests = new List<QuestProgressRecord>();
}

public interface IQuestStore
{
    QuestSaveData Load();
    void Save(QuestSaveData data);
}

/// <summary>
/// Saves quest progress in the active profile folder.
/// </summary>
public class LocalQuestStore : IQuestStore
{
    public const string FileName = "tokyo-rpg-quests.json";

    public static string SavePath => SaveSlots.FilePath(FileName);

    public QuestSaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            return new QuestSaveData();
        }

        try
        {
            QuestSaveData data = JsonUtility.FromJson<QuestSaveData>(File.ReadAllText(SavePath));
            if (data == null)
            {
                return new QuestSaveData();
            }

            if (data.quests == null)
            {
                data.quests = new List<QuestProgressRecord>();
            }

            return data;
        }
        catch (Exception)
        {
            return new QuestSaveData();
        }
    }

    public void Save(QuestSaveData data)
    {
        if (data == null)
        {
            data = new QuestSaveData();
        }

        if (data.quests == null)
        {
            data.quests = new List<QuestProgressRecord>();
        }

        string folder = Path.GetDirectoryName(SavePath);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }
}

/// <summary>
/// Remembers which quests are active or completed.
/// Put this on the Player and assign a quest catalog.
/// </summary>
public class QuestLog : MonoBehaviour
{
    public static QuestLog Instance { get; private set; }

    public event Action Changed;

    [Tooltip("The quests available in this scene. Konbini quests are in Assets/Data/Quests.")]
    [SerializeField] QuestCatalog catalog;

    IQuestStore store;
    QuestSaveData data = new QuestSaveData();

    public QuestCatalog Catalog => catalog;

    void Awake()
    {
        Instance = this;
        store = new LocalQuestStore();
        data = store.Load();

        if (GetComponent<QuestPanel>() == null)
        {
            gameObject.AddComponent<QuestPanel>();
        }
    }

    void OnEnable()
    {
        PlayerInteractor.Studied -= OnStudied;
        PlayerInteractor.Studied += OnStudied;
    }

    void Start()
    {
        OfferSceneQuests();
        Changed?.Invoke();
    }

    void OnDisable()
    {
        PlayerInteractor.Studied -= OnStudied;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public QuestProgressRecord FindRecord(string questId)
    {
        if (data.quests == null || string.IsNullOrWhiteSpace(questId))
        {
            return null;
        }

        for (int i = 0; i < data.quests.Count; i++)
        {
            QuestProgressRecord record = data.quests[i];
            if (record != null && string.Equals(record.questId, questId, StringComparison.OrdinalIgnoreCase))
            {
                return record;
            }
        }

        return null;
    }

    public List<QuestProgressRecord> VisibleQuests()
    {
        List<QuestProgressRecord> result = new List<QuestProgressRecord>();
        if (data.quests == null)
        {
            return result;
        }

        for (int i = 0; i < data.quests.Count; i++)
        {
            QuestProgressRecord record = data.quests[i];
            if (record == null)
            {
                continue;
            }

            if (record.state == (int)QuestState.Active || record.state == (int)QuestState.Completed)
            {
                result.Add(record);
            }
        }

        return result;
    }

    /// <summary>
    /// Starts a quest that is not already active or completed.
    /// An NPC can call this later. Entering the scene uses it for quests marked offer on start.
    /// </summary>
    public bool Offer(string questId)
    {
        if (catalog == null || !catalog.TryGet(questId, out QuestDefinition quest))
        {
            return false;
        }

        QuestProgressRecord record = FindRecord(questId);
        if (record != null && record.state != (int)QuestState.NotStarted)
        {
            return false;
        }

        if (record == null)
        {
            record = new QuestProgressRecord();
            record.questId = quest.id;
            if (data.quests == null)
            {
                data.quests = new List<QuestProgressRecord>();
            }

            data.quests.Add(record);
        }

        record.state = (int)QuestState.Active;
        record.rewardGranted = false;
        record.objectives = CreateFreshObjectives(quest);
        store.Save(data);
        Changed?.Invoke();
        return true;
    }

    void OfferSceneQuests()
    {
        if (catalog == null || catalog.Quests == null)
        {
            return;
        }

        for (int i = 0; i < catalog.Quests.Count; i++)
        {
            QuestDefinition quest = catalog.Quests[i];
            if (quest == null || !quest.offerOnSceneStart || string.IsNullOrWhiteSpace(quest.id))
            {
                continue;
            }

            Offer(quest.id);
        }
    }

    void OnStudied(string vocabularyId)
    {
        if (string.IsNullOrWhiteSpace(vocabularyId) || catalog == null || data.quests == null)
        {
            return;
        }

        bool changed = false;
        int rewardXp = 0;

        for (int i = 0; i < data.quests.Count; i++)
        {
            QuestProgressRecord record = data.quests[i];
            if (record == null || record.state != (int)QuestState.Active)
            {
                continue;
            }

            if (!catalog.TryGet(record.questId, out QuestDefinition quest))
            {
                continue;
            }

            if (!AdvanceOneObjective(record, quest, vocabularyId))
            {
                continue;
            }

            changed = true;
            if (!AllObjectivesComplete(record, quest) || record.rewardGranted)
            {
                continue;
            }

            record.state = (int)QuestState.Completed;
            record.rewardGranted = true;
            rewardXp += Mathf.Max(0, quest.rewardXp);
        }

        if (!changed)
        {
            return;
        }

        store.Save(data);
        if (rewardXp > 0 && PlayerProgression.Instance != null)
        {
            PlayerProgression.Instance.AwardQuestXp(rewardXp);
        }

        Changed?.Invoke();
    }

    static bool AdvanceOneObjective(QuestProgressRecord record, QuestDefinition quest, string vocabularyId)
    {
        if (quest.objectives == null)
        {
            return false;
        }

        for (int i = 0; i < quest.objectives.Count; i++)
        {
            QuestObjectiveDefinition objective = quest.objectives[i];
            if (objective == null || string.IsNullOrWhiteSpace(objective.id))
            {
                continue;
            }

            if (!string.Equals(objective.type, QuestObjectiveTypes.Interact, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.Equals(objective.targetId, vocabularyId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            QuestObjectiveProgress progress = FindObjective(record, objective.id);
            if (progress == null || progress.complete)
            {
                continue;
            }

            progress.complete = true;
            return true;
        }

        return false;
    }

    static bool AllObjectivesComplete(QuestProgressRecord record, QuestDefinition quest)
    {
        if (quest.objectives == null || quest.objectives.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < quest.objectives.Count; i++)
        {
            QuestObjectiveDefinition objective = quest.objectives[i];
            if (objective == null || string.IsNullOrWhiteSpace(objective.id))
            {
                continue;
            }

            QuestObjectiveProgress progress = FindObjective(record, objective.id);
            if (progress == null || !progress.complete)
            {
                return false;
            }
        }

        return true;
    }

    static QuestObjectiveProgress FindObjective(QuestProgressRecord record, string objectiveId)
    {
        if (record.objectives == null)
        {
            return null;
        }

        for (int i = 0; i < record.objectives.Count; i++)
        {
            QuestObjectiveProgress progress = record.objectives[i];
            if (progress != null && string.Equals(progress.objectiveId, objectiveId, StringComparison.OrdinalIgnoreCase))
            {
                return progress;
            }
        }

        return null;
    }

    static List<QuestObjectiveProgress> CreateFreshObjectives(QuestDefinition quest)
    {
        List<QuestObjectiveProgress> objectives = new List<QuestObjectiveProgress>();
        if (quest.objectives == null)
        {
            return objectives;
        }

        for (int i = 0; i < quest.objectives.Count; i++)
        {
            QuestObjectiveDefinition objective = quest.objectives[i];
            if (objective == null || string.IsNullOrWhiteSpace(objective.id))
            {
                continue;
            }

            objectives.Add(new QuestObjectiveProgress
            {
                objectiveId = objective.id,
                complete = false
            });
        }

        return objectives;
    }
}
