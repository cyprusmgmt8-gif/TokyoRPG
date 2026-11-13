using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene names used by the menu and the city map.
/// Each scene must be enabled in File, Build Profiles.
/// </summary>
public static class GameScenes
{
    public const string MainMenu = "MainMenu";
    public const string CityMap = "CityMap";
    public const string Bedroom = "Bedroom";
    public const string Konbini = "Konbini";
    public const string VisitorTest = "VisitorTest";
    public const string Airport = "Airport";
    public const string TrainStation = "TrainStation";

    public static void Load(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("Add " + sceneName + " in File > Build Profiles, then try again.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
