using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Whether the player may enter premium places.
/// This prototype stores a test switch. It does not take payment.
/// A later store can replace the local file when real access is added.
/// </summary>
[Serializable]
public class AccessSaveData
{
    public int version = 1;
    public bool simulatePremium;
}

public interface IAccessStore
{
    AccessSaveData Load();
    void Save(AccessSaveData data);
}

public class LocalAccessStore : IAccessStore
{
    public const string FileName = "tokyo-rpg-access.json";

    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public AccessSaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            return new AccessSaveData();
        }

        try
        {
            AccessSaveData data = JsonUtility.FromJson<AccessSaveData>(File.ReadAllText(SavePath));
            return data ?? new AccessSaveData();
        }
        catch (Exception)
        {
            return new AccessSaveData();
        }
    }

    public void Save(AccessSaveData data)
    {
        if (data == null)
        {
            data = new AccessSaveData();
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }
}

public static class AccessEntitlement
{
    public static event Action Changed;

    static bool loaded;
    static IAccessStore store = new LocalAccessStore();
    static AccessSaveData data = new AccessSaveData();

    public static bool HasPremium
    {
        get
        {
            EnsureLoaded();
            return data.simulatePremium;
        }
    }

    public static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        data = store.Load();
    }

    public static void SetSimulatedPremium(bool value)
    {
        EnsureLoaded();
        data.simulatePremium = value;
        data.version = 1;
        store.Save(data);
        Changed?.Invoke();
    }
}
