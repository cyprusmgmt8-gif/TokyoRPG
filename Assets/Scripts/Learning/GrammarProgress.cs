using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Remembers which grammar lessons the player has opened.
/// The lesson text stays in the grammar catalog. This file does not change dictionary or vocabulary saves.
/// </summary>
[Serializable]
public class GrammarLessonProgress
{
    public string lessonId;
    public string discoveredAtUtc;
}

[Serializable]
public class GrammarSaveData
{
    public int version = 1;
    public List<GrammarLessonProgress> lessons = new List<GrammarLessonProgress>();
}

public interface IGrammarStore
{
    GrammarSaveData Load();
    void Save(GrammarSaveData data);
}

public class LocalGrammarStore : IGrammarStore
{
    public const string FileName = "tokyo-rpg-grammar.json";

    public static string SavePath => SaveSlots.FilePath(FileName);

    public GrammarSaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            return new GrammarSaveData();
        }

        try
        {
            GrammarSaveData data = JsonUtility.FromJson<GrammarSaveData>(File.ReadAllText(SavePath));
            if (data == null)
            {
                return new GrammarSaveData();
            }

            if (data.lessons == null)
            {
                data.lessons = new List<GrammarLessonProgress>();
            }

            return data;
        }
        catch (Exception)
        {
            return new GrammarSaveData();
        }
    }

    public void Save(GrammarSaveData data)
    {
        if (data == null)
        {
            data = new GrammarSaveData();
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }
}

/// <summary>
/// Put this on the same object as Grammar Library. It loads the local save when play starts.
/// </summary>
public class GrammarProgress : MonoBehaviour
{
    public static GrammarProgress Instance { get; private set; }

    public event Action Changed;

    /// <summary>
    /// Raised only the first time a lesson is opened.
    /// </summary>
    public event Action<string> LessonDiscovered;

    IGrammarStore store;
    GrammarSaveData data = new GrammarSaveData();

    void Awake()
    {
        Instance = this;
        store = new LocalGrammarStore();
        data = store.Load();

        if (GetComponent<GrammarPanel>() == null)
        {
            gameObject.AddComponent<GrammarPanel>();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool IsDiscovered(string lessonId)
    {
        return Find(lessonId) != null;
    }

    public int DiscoveredCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < data.lessons.Count; i++)
            {
                if (data.lessons[i] != null && !string.IsNullOrWhiteSpace(data.lessons[i].lessonId))
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>
    /// Saves a lesson the first time it is opened. Returns false if it was already saved.
    /// </summary>
    public bool Discover(string lessonId)
    {
        if (string.IsNullOrWhiteSpace(lessonId) || IsDiscovered(lessonId))
        {
            return false;
        }

        data.lessons.Add(new GrammarLessonProgress
        {
            lessonId = lessonId,
            discoveredAtUtc = DateTime.UtcNow.ToString("o")
        });
        store.Save(data);
        Changed?.Invoke();
        LessonDiscovered?.Invoke(lessonId);
        return true;
    }

    GrammarLessonProgress Find(string lessonId)
    {
        for (int i = 0; i < data.lessons.Count; i++)
        {
            GrammarLessonProgress lesson = data.lessons[i];
            if (lesson != null && string.Equals(lesson.lessonId, lessonId, StringComparison.OrdinalIgnoreCase))
            {
                return lesson;
            }
        }

        return null;
    }
}
