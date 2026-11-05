using System;
using System.IO;
using UnityEngine;

/// <summary>
/// The player's experience. Room mastery will use its own save later.
/// A future cloud save can send this same object.
/// </summary>
[Serializable]
public class PlayerProgressData
{
    public int version = 1;
    public int totalXp;
}

/// <summary>
/// Loads and saves experience.
/// The game uses a file on the device now. A cloud store can replace it later
/// without changing how XP is awarded.
/// </summary>
public interface IProgressionStore
{
    PlayerProgressData Load();
    void Save(PlayerProgressData data);
}

/// <summary>
/// Saves experience as JSON in Unity's persistent data folder on this device.
/// </summary>
public class LocalProgressionStore : IProgressionStore
{
    public const string FileName = "tokyo-rpg-progress.json";

    public static string SavePath => SaveSlots.FilePath(FileName);

    public PlayerProgressData Load()
    {
        if (!File.Exists(SavePath))
        {
            return new PlayerProgressData();
        }

        try
        {
            PlayerProgressData data = JsonUtility.FromJson<PlayerProgressData>(File.ReadAllText(SavePath));
            if (data == null)
            {
                return new PlayerProgressData();
            }

            if (data.totalXp < 0)
            {
                data.totalXp = 0;
            }

            return data;
        }
        catch (Exception)
        {
            return new PlayerProgressData();
        }
    }

    public void Save(PlayerProgressData data)
    {
        if (data == null)
        {
            data = new PlayerProgressData();
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }
}
