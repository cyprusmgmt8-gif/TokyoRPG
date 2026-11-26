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
    public const string RamenShop = "RamenShop";
    public const string ClothingStore = "ClothingStore";
    public const string Hotel = "Hotel";
    public const string ExploringTokyo = "ExploringTokyo";

    public static void Load(string sceneName)
    {
        Load(sceneName, "");
    }

    /// <summary>
    /// Loads a scene. An empty return id clears any pending doorway return,
    /// which is what the city map and the menus use.
    /// </summary>
    public static void Load(string sceneName, string returnPointId)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneTransitionContext.Clear();
            Debug.LogError("Add " + sceneName + " in File > Build Profiles, then try again.");
            return;
        }

        if (string.IsNullOrWhiteSpace(returnPointId))
        {
            SceneTransitionContext.Clear();
        }
        else
        {
            SceneTransitionContext.Set(sceneName, returnPointId);
        }

        SceneManager.LoadScene(sceneName);
    }
}
