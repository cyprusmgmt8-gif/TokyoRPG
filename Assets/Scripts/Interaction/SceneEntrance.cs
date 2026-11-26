using UnityEngine;

/// <summary>
/// A doorway the player looks at and opens with E.
/// Walking past it does nothing. The city map's access rule decides if the scene loads.
/// </summary>
public class SceneEntrance : MonoBehaviour
{
    [Tooltip("Scene name from GameScenes, for example Konbini. The place must also be on the city map.")]
    [SerializeField] string sceneName = "";

    [Tooltip("Words after E on the prompt, for example Enter Konbini.")]
    [SerializeField] string promptLabel = "Enter";

    [Tooltip("Optional standing spot in the destination. Leave empty to use that scene's normal spawn.")]
    [SerializeField] string returnPointId = "";

    static bool loading;

    public string SceneName => sceneName ?? "";

    public string PromptLabel => string.IsNullOrWhiteSpace(promptLabel) ? "Enter" : promptLabel;

    /// <summary>
    /// Loads the destination once. A second press while the load is starting is ignored.
    /// </summary>
    public void Enter()
    {
        if (loading)
        {
            return;
        }

        if (!CityDirectory.TryGetByScene(sceneName, out CityLocation place) || !CityDirectory.CanEnter(place))
        {
            SceneTransitionContext.Clear();
            Debug.Log("Premium access. The free game includes the bedroom only. This place opens with premium access.");
            return;
        }

        loading = true;
        GameScenes.Load(place.sceneName, returnPointId);
        loading = false;
    }
}
