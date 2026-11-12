using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hides the gameplay chips while a full panel is open.
/// Registered objects are the chips only: XP, Map, Quests, and Basket.
/// Each panel keeps its own screen and its own Close or Back button.
/// </summary>
public class GameplayHud : MonoBehaviour
{
    static readonly List<GameObject> roots = new List<GameObject>();

    public static bool ShouldHide(bool studyCardOpen, bool revisionOpen, bool grammarOpen)
    {
        return studyCardOpen || revisionOpen || grammarOpen || OtherPanelOpen();
    }

    public static bool Hidden
    {
        get
        {
            bool studyCardOpen = LearningPanel.Current != null && LearningPanel.Current.IsOpen;
            return ShouldHide(studyCardOpen, RevisionPanel.IsOpenNow, GrammarPanel.IsOpenNow);
        }
    }

    static bool OtherPanelOpen()
    {
        return QuestPanel.IsOpenNow
            || BasketPanel.IsOpenNow
            || DictionaryPanel.IsOpenNow
            || DialoguePanel.IsOpenNow
            || CheckoutPanel.IsOpenNow
            || CityMapScreen.IsOpenNow
            || MainMenuScreen.IsOpenNow;
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
