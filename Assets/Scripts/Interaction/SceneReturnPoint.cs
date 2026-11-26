using UnityEngine;

/// <summary>
/// A marked standing spot in a scene.
/// It does not move the player by itself. A scene load asks for it by id.
/// </summary>
public class SceneReturnPoint : MonoBehaviour
{
    [Tooltip("Unique id inside this scene, for example tokyo.konbini.outside.")]
    [SerializeField] string pointId = "";

    public string PointId => pointId ?? "";
}
