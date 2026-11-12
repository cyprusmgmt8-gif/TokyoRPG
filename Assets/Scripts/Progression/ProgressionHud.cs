using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the current level and XP at the top-left of the game view.
/// This is added automatically by Player Progression when the scene plays.
/// </summary>
public class ProgressionHud : MonoBehaviour
{
    Text label;

    void Awake()
    {
        BuildHud();
    }

    void OnEnable()
    {
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.Instance.Changed += Refresh;
        }
    }

    void Start()
    {
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.Instance.Changed -= Refresh;
            PlayerProgression.Instance.Changed += Refresh;
        }

        Refresh();
    }

    void OnDisable()
    {
        if (PlayerProgression.Instance != null)
        {
            PlayerProgression.Instance.Changed -= Refresh;
        }
    }

    void Refresh()
    {
        if (label == null || PlayerProgression.Instance == null)
        {
            return;
        }

        PlayerProgression progress = PlayerProgression.Instance;
        label.text = "Lv " + progress.Level + "  ·  " + progress.TotalXp + " XP";
    }

    void BuildHud()
    {
        GameObject canvasObject = new GameObject("Progress Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        GameplayHud.Register(canvasObject);

        GameObject badge = new GameObject("XP");
        badge.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = badge.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.015f, 0.945f);
        rect.anchorMax = new Vector2(0.18f, 0.985f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image background = badge.AddComponent<Image>();
        background.color = new Color(0.11f, 0.105f, 0.1f, 0.72f);
        background.raycastTarget = false;

        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(badge.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 2f);
        textRect.offsetMax = new Vector2(-10f, -2f);

        label = textObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 20;
        label.fontStyle = FontStyle.Bold;
        label.color = new Color(1f, 0.93f, 0.8f);
        label.alignment = TextAnchor.MiddleLeft;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.raycastTarget = false;
        label.text = "Level 1    0 XP";
    }
}
