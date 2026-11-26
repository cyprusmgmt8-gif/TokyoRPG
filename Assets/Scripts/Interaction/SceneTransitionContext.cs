using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Remembers one return spot while a scene is loading.
/// It is not written to the player profile. The city map clears it by loading with no return id.
/// </summary>
public static class SceneTransitionContext
{
    static bool listening;

    public static bool HasPending { get; private set; }

    public static string SceneName { get; private set; } = "";

    public static string ReturnPointId { get; private set; } = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Listen()
    {
        if (listening)
        {
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        listening = true;
    }

    public static void Set(string sceneName, string returnPointId)
    {
        SceneName = sceneName ?? "";
        ReturnPointId = returnPointId == null ? "" : returnPointId.Trim();
        HasPending = SceneName.Length > 0 && ReturnPointId.Length > 0;
        if (!HasPending)
        {
            SceneName = "";
            ReturnPointId = "";
        }
    }

    public static void Clear()
    {
        HasPending = false;
        SceneName = "";
        ReturnPointId = "";
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!HasPending || mode != LoadSceneMode.Single)
        {
            return;
        }

        string expectedScene = SceneName;
        string pointId = ReturnPointId;
        Clear();

        if (scene.name != expectedScene)
        {
            Debug.LogWarning("A return to \"" + pointId + "\" was dropped because " + scene.name + " loaded instead of " + expectedScene + ".");
            return;
        }

        SceneReturnPoint match = FindPoint(scene, pointId, out int count);
        if (match == null)
        {
            Debug.LogWarning("Return point \"" + pointId + "\" was not found in " + scene.name + ". The normal spawn is used.");
            return;
        }

        if (count > 1)
        {
            Debug.LogWarning("More than one return point uses \"" + pointId + "\" in " + scene.name + ". The first one is used.");
        }

        FirstPersonController player = Object.FindAnyObjectByType<FirstPersonController>();
        if (player == null)
        {
            Debug.LogWarning("No player was found for return point \"" + pointId + "\". The normal spawn is used.");
            return;
        }

        player.PlaceAt(match.transform);
    }

    static SceneReturnPoint FindPoint(Scene scene, string pointId, out int count)
    {
        SceneReturnPoint match = null;
        count = 0;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            SceneReturnPoint[] points = roots[i].GetComponentsInChildren<SceneReturnPoint>(true);
            for (int j = 0; j < points.Length; j++)
            {
                if (points[j].PointId != pointId)
                {
                    continue;
                }

                count++;
                if (match == null)
                {
                    match = points[j];
                }
            }
        }

        return match;
    }
}
