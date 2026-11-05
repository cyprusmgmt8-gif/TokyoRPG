using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// One folder per profile. Continue uses the active folder.
/// New Game adds another folder and leaves the previous one in place.
/// </summary>
[Serializable]
public class SaveSlotIndexData
{
    public string activeId = "";
    public List<string> ids = new List<string>();
}

public class SaveSlotSummary
{
    public string id;
    public string nickname;
    public int totalXp;
    public bool active;
}

public static class SaveSlots
{
    public const string FolderName = "saves";

    static bool ready;
    static SaveSlotIndexData index = new SaveSlotIndexData();

    public static string ActiveId => index.activeId;

    static string Root => Path.Combine(Application.persistentDataPath, FolderName);

    static string IndexPath => Path.Combine(Root, "index.json");

    public static string ActiveDirectory
    {
        get
        {
            EnsureReady();
            if (string.IsNullOrEmpty(index.activeId))
            {
                return Path.Combine(Root, "unsaved");
            }

            return Path.Combine(Root, index.activeId);
        }
    }

    public static string FilePath(string fileName)
    {
        return Path.Combine(ActiveDirectory, fileName);
    }

    public static void EnsureReady()
    {
        if (ready)
        {
            return;
        }

        ready = true;
        Directory.CreateDirectory(Root);
        if (!File.Exists(IndexPath))
        {
            ImportLegacyFiles();
        }

        LoadIndex();
        RecoverFoldersMissingFromIndex();
    }

    public static int Count
    {
        get
        {
            EnsureReady();
            return index.ids.Count;
        }
    }

    public static List<SaveSlotSummary> List()
    {
        EnsureReady();
        List<SaveSlotSummary> result = new List<SaveSlotSummary>();
        for (int i = 0; i < index.ids.Count; i++)
        {
            string id = index.ids[i];
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            string folder = Path.Combine(Root, id);
            PlayerProfileData profile = ReadJson<PlayerProfileData>(Path.Combine(folder, LocalProfileStore.FileName));
            PlayerProgressData progress = ReadJson<PlayerProgressData>(Path.Combine(folder, LocalProgressionStore.FileName));
            result.Add(new SaveSlotSummary
            {
                id = id,
                nickname = profile != null && !string.IsNullOrWhiteSpace(profile.nickname) ? profile.nickname : "Profile",
                totalXp = progress != null ? Mathf.Max(0, progress.totalXp) : 0,
                active = id == index.activeId
            });
        }

        return result;
    }

    public static void Activate(string id)
    {
        EnsureReady();
        if (string.IsNullOrEmpty(id) || !index.ids.Contains(id))
        {
            return;
        }

        index.activeId = id;
        Directory.CreateDirectory(Path.Combine(Root, id));
        SaveIndex();
    }

    /// <summary>
    /// Points Continue at a new empty folder. Earlier folders are not changed.
    /// </summary>
    public static void BeginNewProfile()
    {
        EnsureReady();
        string id = "p" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        Directory.CreateDirectory(Path.Combine(Root, id));
        index.ids.Add(id);
        index.activeId = id;
        SaveIndex();
    }

    static void ImportLegacyFiles()
    {
        string[] names =
        {
            LocalProfileStore.FileName,
            LocalProgressionStore.FileName,
            LocalDictionaryStore.FileName,
            LocalGrammarStore.FileName,
            LocalRevisionStore.FileName
        };

        bool any = false;
        for (int i = 0; i < names.Length; i++)
        {
            if (File.Exists(Path.Combine(Application.persistentDataPath, names[i])))
            {
                any = true;
                break;
            }
        }

        if (!any)
        {
            return;
        }

        string id = "p-original";
        string folder = Path.Combine(Root, id);
        Directory.CreateDirectory(folder);
        for (int i = 0; i < names.Length; i++)
        {
            string from = Path.Combine(Application.persistentDataPath, names[i]);
            string to = Path.Combine(folder, names[i]);
            if (!File.Exists(from) || File.Exists(to))
            {
                continue;
            }

            File.Move(from, to);
        }

        index.activeId = id;
        index.ids.Add(id);
        SaveIndex();
    }

    static void RecoverFoldersMissingFromIndex()
    {
        if (index.ids.Count > 0 || !Directory.Exists(Root))
        {
            return;
        }

        string[] folders = Directory.GetDirectories(Root);
        for (int i = 0; i < folders.Length; i++)
        {
            string id = Path.GetFileName(folders[i]);
            if (string.IsNullOrEmpty(id) || !id.StartsWith("p", StringComparison.Ordinal))
            {
                continue;
            }

            index.ids.Add(id);
        }

        if (index.ids.Count == 0)
        {
            return;
        }

        index.activeId = index.ids[index.ids.Count - 1];
        SaveIndex();
    }

    static void LoadIndex()
    {
        if (!File.Exists(IndexPath))
        {
            index = new SaveSlotIndexData();
            return;
        }

        try
        {
            SaveSlotIndexData loaded = JsonUtility.FromJson<SaveSlotIndexData>(File.ReadAllText(IndexPath));
            index = loaded ?? new SaveSlotIndexData();
            if (index.ids == null)
            {
                index.ids = new List<string>();
            }
        }
        catch (Exception)
        {
            index = new SaveSlotIndexData();
        }
    }

    static void SaveIndex()
    {
        Directory.CreateDirectory(Root);
        File.WriteAllText(IndexPath, JsonUtility.ToJson(index, true));
    }

    static T ReadJson<T>(string path) where T : class
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<T>(File.ReadAllText(path));
        }
        catch (Exception)
        {
            return null;
        }
    }
}
