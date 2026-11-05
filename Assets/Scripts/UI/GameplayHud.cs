using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shows or hides the bedroom bars that sit on their own canvases:
/// the XP canvas, the Map canvas, and the Dictionary button.
/// Those canvases are drawn on top of the study card, so the card cannot cover them.
/// The learning card, revision screen, and grammar screen stay untouched.
/// </summary>
public class GameplayHud : MonoBehaviour
{
    static readonly List<GameObject> roots = new List<GameObject>();

    public static bool ShouldHide(bool studyCardOpen, bool revisionOpen, bool grammarOpen)
    {
        return studyCardOpen || revisionOpen || grammarOpen;
    }

    public static bool Hidden
    {
        get
        {
            bool studyCardOpen = LearningPanel.Current != null && LearningPanel.Current.IsOpen;
            return ShouldHide(studyCardOpen, RevisionPanel.IsOpenNow, GrammarPanel.IsOpenNow);
        }
    }

    public static void Register(GameObject root)
    {
        if (root == null || roots.Contains(root))
        {
            return;
        }

        roots.Add(root);
        if (FindAnyObjectByType<GameplayHud>() == null)
        {
            GameObject host = new GameObject("Gameplay HUD");
            host.AddComponent<GameplayHud>();
        }
    }

    public static void Apply()
    {
        bool show = !Hidden;
        for (int i = roots.Count - 1; i >= 0; i--)
        {
            GameObject root = roots[i];
            if (root == null)
            {
                roots.RemoveAt(i);
                continue;
            }

            if (root.activeSelf != show)
            {
                root.SetActive(show);
            }
        }

        DictionaryPanel.ApplyLauncher();
    }

    void LateUpdate()
    {
        Apply();
    }
}
