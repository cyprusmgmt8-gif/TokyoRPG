using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Shared pieces for the menu and the city map.
/// Screens build themselves when play starts, so the scenes do not need a hand-built canvas.
/// </summary>
public static class MenuUi
{
    public static readonly Color Ink = new Color(0.1f, 0.11f, 0.14f, 0.98f);
    public static readonly Color Paper = new Color(1f, 0.93f, 0.8f, 1f);
    public static readonly Color Amber = new Color(0.72f, 0.42f, 0.22f, 1f);
    public static readonly Color Stone = new Color(0.28f, 0.26f, 0.24f, 1f);
    public static readonly Color Muted = new Color(0.8f, 0.8f, 0.76f, 1f);

    public static Font FindFont()
    {
        string[] candidates =
        {
            "Hiragino Sans",
            "Hiragino Kaku Gothic ProN",
            "Yu Gothic",
            "Noto Sans CJK JP",
            "Noto Sans JP",
            "Arial Unicode MS"
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            try
            {
                Font font = Font.CreateDynamicFontFromOSFont(candidates[i], 48);
                if (font != null && font.HasCharacter('字'))
                {
                    return font;
                }
            }
            catch (Exception)
            {
                // Try the next font installed on this computer or phone.
            }
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    public static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventObject = new GameObject("EventSystem");
        eventObject.AddComponent<EventSystem>();
        eventObject.AddComponent<InputSystemUIInputModule>();
    }

    public static void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void PaintNightSky()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.08f, 0.14f);
    }

    public static Canvas CreateCanvas(Transform parent, string name, int sortOrder)
    {
        GameObject canvasObject = new GameObject(name);
        canvasObject.transform.SetParent(parent, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static GameObject CreateRect(string name, Transform parent)
    {
        GameObject item = new GameObject(name);
        item.transform.SetParent(parent, false);
        item.AddComponent<RectTransform>();
        return item;
    }

    public static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static Text AddText(Transform parent, string value, Font font, int size, FontStyle style, Color color, float height, TextAnchor alignment)
    {
        GameObject textObject = CreateRect("Text", parent);
        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        text.text = value;
        LayoutElement element = textObject.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        return text;
    }

    public static Button CreateButton(Transform parent, string label, Font font, Color color, UnityAction onClick, float height)
    {
        GameObject buttonObject = CreateRect(label, parent);
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.minHeight = height;
        Image image = buttonObject.AddComponent<Image>();
        image.color = color;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        Text text = AddText(buttonObject.transform, label, font, 28, FontStyle.Bold, Color.white, height, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, Vector2.zero, Vector2.one);
        text.GetComponent<LayoutElement>().ignoreLayout = true;
        return button;
    }

    public static InputField CreateNicknameField(Transform parent, Font font)
    {
        GameObject root = CreateRect("Nickname", parent);
        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredHeight = 76;
        layout.minHeight = 76;
        Image image = root.AddComponent<Image>();
        image.color = new Color(0.05f, 0.05f, 0.06f, 1f);

        GameObject textObject = CreateRect("Input", root.transform);
        Stretch(textObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        textObject.GetComponent<RectTransform>().offsetMin = new Vector2(20f, 8f);
        textObject.GetComponent<RectTransform>().offsetMax = new Vector2(-20f, -8f);
        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = 32;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.supportRichText = false;
        text.raycastTarget = false;

        GameObject placeholderObject = CreateRect("Placeholder", root.transform);
        Stretch(placeholderObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        placeholderObject.GetComponent<RectTransform>().offsetMin = new Vector2(20f, 8f);
        placeholderObject.GetComponent<RectTransform>().offsetMax = new Vector2(-20f, -8f);
        Text placeholder = placeholderObject.AddComponent<Text>();
        placeholder.font = font;
        placeholder.fontSize = 32;
        placeholder.fontStyle = FontStyle.Italic;
        placeholder.color = new Color(1f, 1f, 1f, 0.4f);
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.raycastTarget = false;
        placeholder.text = "Nickname";

        InputField field = root.AddComponent<InputField>();
        field.targetGraphic = image;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.characterLimit = 16;
        field.lineType = InputField.LineType.SingleLine;
        return field;
    }

    public static RectTransform CreateScroll(Transform parent, out RectTransform content)
    {
        GameObject scrollObject = CreateRect("Scroll", parent);
        LayoutElement scrollLayout = scrollObject.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1;
        scrollLayout.preferredHeight = 520;
        scrollLayout.minHeight = 200;
        scrollObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.18f);
        scrollObject.AddComponent<RectMask2D>();
        ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        scroll.viewport = scrollObject.GetComponent<RectTransform>();

        GameObject contentObject = CreateRect("Content", scrollObject.transform);
        content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        VerticalLayoutGroup listLayout = contentObject.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 12;
        listLayout.padding = new RectOffset(8, 8, 8, 8);
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;
        return scrollObject.GetComponent<RectTransform>();
    }

    public static GameObject CreateCard(Transform parent, Vector2 min, Vector2 max)
    {
        GameObject card = CreateRect("Card", parent);
        Stretch(card.GetComponent<RectTransform>(), min, max);
        Image image = card.AddComponent<Image>();
        image.color = Ink;
        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 24, 24);
        layout.spacing = 14;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return card;
    }
}
