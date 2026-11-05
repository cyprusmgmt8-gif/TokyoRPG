using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Progress for one saved word. The Japanese text stays in the vocabulary catalog.
/// This file only remembers that the player has found the word.
/// </summary>
[Serializable]
public class DictionaryWordProgress
{
    public string vocabularyId;
    public string discoveredAtUtc;
    public bool needsRevision;
}

/// <summary>
/// The whole dictionary save. A future cloud save can send this same object.
/// </summary>
[Serializable]
public class DictionarySaveData
{
    public int version = 1;
    public List<DictionaryWordProgress> words = new List<DictionaryWordProgress>();
}

/// <summary>
/// Loads and saves dictionary progress.
/// The game uses a file on the device now. A cloud store can replace it later
/// without changing the dictionary screen.
/// </summary>
public interface IDictionaryStore
{
    DictionarySaveData Load();
    void Save(DictionarySaveData data);
}

/// <summary>
/// Saves the dictionary as JSON in Unity's persistent data folder on this device.
/// </summary>
public class LocalDictionaryStore : IDictionaryStore
{
    public const string FileName = "tokyo-rpg-dictionary.json";

    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public DictionarySaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            return new DictionarySaveData();
        }

        try
        {
            DictionarySaveData data = JsonUtility.FromJson<DictionarySaveData>(File.ReadAllText(SavePath));
            if (data == null)
            {
                return new DictionarySaveData();
            }

            if (data.words == null)
            {
                data.words = new List<DictionaryWordProgress>();
            }

            return data;
        }
        catch (Exception)
        {
            return new DictionarySaveData();
        }
    }

    public void Save(DictionarySaveData data)
    {
        if (data == null)
        {
            data = new DictionarySaveData();
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }
}
