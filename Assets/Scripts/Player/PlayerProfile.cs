using System;
using System.IO;
using UnityEngine;

/// <summary>
/// The player's nickname, avatar and interface language.
/// Dictionary words and XP stay in their own files. This file does not replace them.
/// </summary>
[Serializable]
public class PlayerProfileData
{
    public int version = 1;
    public bool hasStarted;
    public string nickname = "";
    public string avatarId = "traveler";
    public string languageCode = "";
}

public interface IProfileStore
{
    bool Exists();
    PlayerProfileData Load();
    void Save(PlayerProfileData data);
}

/// <summary>
/// Saves the profile as JSON in Unity's persistent data folder on this device.
/// A cloud store can replace this later.
/// </summary>
public class LocalProfileStore : IProfileStore
{
    public const string FileName = "tokyo-rpg-profile.json";

    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public bool Exists()
    {
        return File.Exists(SavePath);
    }

    public PlayerProfileData Load()
    {
        if (!Exists())
        {
            return new PlayerProfileData();
        }

        try
        {
            PlayerProfileData data = JsonUtility.FromJson<PlayerProfileData>(File.ReadAllText(SavePath));
            if (data == null)
            {
                return new PlayerProfileData();
            }

            if (data.nickname == null)
            {
                data.nickname = "";
            }

            if (string.IsNullOrWhiteSpace(data.avatarId))
            {
                data.avatarId = "traveler";
            }

            if (data.languageCode == null)
            {
                data.languageCode = "";
            }

            return data;
        }
        catch (Exception)
        {
            return new PlayerProfileData();
        }
    }

    public void Save(PlayerProfileData data)
    {
        if (data == null)
        {
            data = new PlayerProfileData();
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }
}

public class AvatarChoice
{
    public string id;
    public string label;
    public Color color;

    public AvatarChoice(string id, string label, Color color)
    {
        this.id = id;
        this.label = label;
        this.color = color;
    }
}

public static class AvatarChoices
{
    public static readonly AvatarChoice[] All =
    {
        new AvatarChoice("traveler", "Traveler", new Color(0.86f, 0.55f, 0.28f)),
        new AvatarChoice("student", "Student", new Color(0.35f, 0.55f, 0.78f)),
        new AvatarChoice("neighbor", "Neighbor", new Color(0.36f, 0.62f, 0.42f))
    };

    public static AvatarChoice Find(string id)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (string.Equals(All[i].id, id, StringComparison.OrdinalIgnoreCase))
            {
                return All[i];
            }
        }

        return All[0];
    }
}

public class LanguageChoice
{
    public string code;
    public string label;

    public LanguageChoice(string code, string label)
    {
        this.code = code;
        this.label = label;
    }
}

/// <summary>
/// Languages the menu can offer. English words are in the catalog today.
/// Other codes fall back to English until translations are added.
/// </summary>
public static class InterfaceLanguageChoices
{
    public static readonly LanguageChoice[] All =
    {
        new LanguageChoice("en", "English"),
        new LanguageChoice("es", "Español"),
        new LanguageChoice("ja", "日本語")
    };

    public static bool IsKnown(string code)
    {
        return Find(code) != null;
    }

    public static LanguageChoice Find(string code)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (string.Equals(All[i].code, code, StringComparison.OrdinalIgnoreCase))
            {
                return All[i];
            }
        }

        return null;
    }
}

/// <summary>
/// Loads the profile once per play session and writes it back when the menu changes it.
/// </summary>
public static class PlayerProfile
{
    static bool loaded;
    static IProfileStore store = new LocalProfileStore();

    public static PlayerProfileData Data { get; private set; } = new PlayerProfileData();

    public static bool HasStarted =>
        Data != null && Data.hasStarted && !string.IsNullOrWhiteSpace(Data.nickname);

    public static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        Data = store.Load();
    }

    public static string CurrentLanguageCode(string fallback)
    {
        EnsureLoaded();
        if (!string.IsNullOrWhiteSpace(Data.languageCode))
        {
            return Data.languageCode.Trim();
        }

        return string.IsNullOrWhiteSpace(fallback) ? "en" : fallback.Trim();
    }

    public static void SaveNew(string nickname, string avatarId, string languageCode)
    {
        EnsureLoaded();
        Data.nickname = nickname.Trim();
        Data.avatarId = AvatarChoices.Find(avatarId).id;
        Data.languageCode = InterfaceLanguageChoices.IsKnown(languageCode) ? languageCode.Trim() : "en";
        Data.hasStarted = true;
        Data.version = 1;
        store.Save(Data);
    }

    public static void SetLanguage(string languageCode)
    {
        EnsureLoaded();
        if (!InterfaceLanguageChoices.IsKnown(languageCode))
        {
            return;
        }

        Data.languageCode = languageCode.Trim();
        store.Save(Data);
    }
}
