using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor switches for the free and premium test.
/// Menu: Tokyo RPG / Access.
/// </summary>
public static class AccessTestMenu
{
    [MenuItem("Tokyo RPG/Access/Test as free player")]
    static void Free()
    {
        AccessEntitlement.SetSimulatedPremium(false);
        Debug.Log("Premium test is off. Saved to " + LocalAccessStore.SavePath);
    }

    [MenuItem("Tokyo RPG/Access/Test as premium player")]
    static void Premium()
    {
        AccessEntitlement.SetSimulatedPremium(true);
        Debug.Log("Premium test is on. Saved to " + LocalAccessStore.SavePath);
    }
}
