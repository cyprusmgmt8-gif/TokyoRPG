using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// The dictionary button and the dictionary screen.
/// This is added automatically by Personal Dictionary when the scene plays.
/// </summary>
public class DictionaryPanel : MonoBehaviour
{
    public static bool IsOpenNow { get; private set; }

    DictionaryFilter filter = DictionaryFilter.All;
    GameObject screenRoot;
    RectTransform listContent;
    Text emptyText;
    Font uiFont;
    FirstPersonController pausedMovement;
    bool isOpen;

    void Awake()
    {
        uiFont = FindJapaneseFont();
        EnsureEventSystem();
        BuildUi();
        screenRoot.SetActive(false);
    }

    void Start()
    {
        if (PersonalDictionary.Instance != null)
        {
            PersonalDictionary.Instance.Changed += RebuildList;
        }
    }

    void OnDestroy()
    {
        if (PersonalDictionary.Instance != null)
        {
            PersonalDictionary.Instance.Changed -= RebuildList;
        }
    }

    void Update()
    {
        if (!isOpen)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    public void Toggle()
    {
        if (isOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void Open()
    {
        LearningPanel learning = FindAnyObjectByType<LearningPanel>();
        if (learning != null && learning.IsOpen)
        {
            learning.Close();
        }

        screenRoot.SetActive(true);
        isOpen = true;
        IsOpenNow = true;
        RebuildList();

        pausedMovement = GetComponent<FirstPersonController>();
        if (pausedMovement != null)
        {
            pausedMovement.enabled = false;
        }
    }

    public void Close()
    {
        screenRoot.SetActive(false);
        isOpen = false;
        IsOpenNow = false;

        LearningPanel learning = FindAnyObjectByType<LearningPanel>();
        bool learningOpen = learning != null && learning.IsOpen;
        if (pausedMovement != null && !learningOpen)
        {
            pausedMovement.enabled = true;
        }

        pausedMovement = null;
    }

    void SetFilter(DictionaryFilter nextFilter)
    {
        filter = nextFilter;
        RebuildList();
    }

    void RebuildList()
    {
        if (listContent == null || PersonalDictionary.Instance == null)
        {
            return;
        }

        for (int i = listContent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(listContent.GetChild(i).gameObject);
        }

        List<DictionaryWordProgress> words = PersonalDictionary.Instance.GetWords(filter);
        emptyText.gameObject.SetActive(words.Count == 0);
        if (words.Count == 0)
        {
            emptyText.text = EmptyMessage(filter);
            return;
        }

        for (int i = 0; i < words.Count; i++)
        {
            CreateRow(words[i]);
        }
    }

    void CreateRow(DictionaryWordProgress progress)
    {
        VocabularyEntry entry = null;
        bool found = VocabularyLibrary.Instance != null && VocabularyLibrary.Instance.TryGet(progress.vocabularyId, out entry);

        GameObject row = CreateRect("Word", listContent);
        Image background = row.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.05f);
        VerticalLayoutGroup layout = row.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 12, 12);
        layout.spacing = 4;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        LayoutElement element = row.AddComponent<LayoutElement>();
        element.preferredHeight = found ? 240 : 80;

        if (!found)
        {
            AddText(row.transform, progress.vocabularyId, 24, FontStyle.Normal, Color.white, 36, TextAnchor.MiddleLeft);
            return;
        }

        AddText(row.transform, entry.japanese, 36, FontStyle.Bold, Color.white, 46, TextAnchor.MiddleLeft);
        AddText(row.transform, entry.reading, 24, FontStyle.Normal, new Color(1f, 0.86f, 0.55f), 32, TextAnchor.MiddleLeft);
        AddText(row.transform, entry.translation, 24, FontStyle.Normal, new Color(0.9f, 0.9f, 0.88f), 32, TextAnchor.MiddleLeft);
        AddText(row.transform, entry.exampleSentence, 22, FontStyle.Italic, new Color(0.82f, 0.82f, 0.78f), 48, TextAnchor.MiddleLeft);

        GameObject buttonObject = CreateRect("Revision", row.transform);
        LayoutElement buttonLayout = buttonObject.AddComponent<LayoutElement>();
        buttonLayout.preferredHeight = 44;
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = progress.needsRevision
            ? new Color(0.72f, 0.42f, 0.22f, 1f)
            : new Color(0.25f, 0.32f, 0.28f, 1f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        string vocabularyId = progress.vocabularyId;
        bool needsRevision = progress.needsRevision;
        button.onClick.AddListener(() =>
        {
            if (PersonalDictionary.Instance != null)
            {
                PersonalDictionary.Instance.SetNeedsRevision(vocabularyId, !needsRevision);
            }
        });
        Text buttonLabel = AddText(buttonObject.transform, needsRevision ? "Needs revision" : "Reviewed", 20, FontStyle.Bold, Color.white, 44, TextAnchor.MiddleCenter);
        Stretch(buttonLabel.rectTransform, Vector2.zero, Vector2.one);
    }

    void BuildUi()
    {
        GameObject canvasObject = new GameObject("Dictionary Canvas");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject openButton = CreateRect("Open Dictionary", canvasObject.transform);
        Stretch(openButton.GetComponent<RectTransform>(), new Vector2(0.74f, 0.86f), new Vector2(0.98f, 0.98f));
        Image openImage = openButton.AddComponent<Image>();
        openImage.color = new Color(0.14f, 0.13f, 0.12f, 0.92f);
        Button open = openButton.AddComponent<Button>();
        open.targetGraphic = openImage;
        open.onClick.AddListener(Toggle);
        Text openLabel = AddText(openButton.transform, "Dictionary", 28, FontStyle.Bold, new Color(1f, 0.93f, 0.8f), 40, TextAnchor.MiddleCenter);
        Stretch(openLabel.rectTransform, Vector2.zero, Vector2.one);

        screenRoot = CreateRect("Dictionary Screen", canvasObject.transform);
        Stretch(screenRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        Image dim = screenRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject card = CreateRect("Card", screenRoot.transform);
        Stretch(card.GetComponent<RectTransform>(), new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.88f));
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.14f, 0.13f, 0.12f, 0.98f);
        VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(24, 24, 20, 20);
        cardLayout.spacing = 12;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        AddText(card.transform, "Dictionary", 36, FontStyle.Bold, Color.white, 48, TextAnchor.MiddleCenter);

        GameObject filters = CreateRect("Filters", card.transform);
        LayoutElement filterLayout = filters.AddComponent<LayoutElement>();
        filterLayout.preferredHeight = 64;
        HorizontalLayoutGroup filterRow = filters.AddComponent<HorizontalLayoutGroup>();
        filterRow.spacing = 12;
        filterRow.childControlWidth = true;
        filterRow.childControlHeight = true;
        filterRow.childForceExpandWidth = true;
        filterRow.childForceExpandHeight = true;
        CreateFilterButton(filters.transform, "All", DictionaryFilter.All);
        CreateFilterButton(filters.transform, "Recent", DictionaryFilter.Recent);
        CreateFilterButton(filters.transform, "Revision", DictionaryFilter.Revision);

        GameObject scrollObject = CreateRect("Scroll", card.transform);
        LayoutElement scrollLayout = scrollObject.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1;
        scrollLayout.preferredHeight = 640;
        scrollObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.2f);
        scrollObject.AddComponent<RectMask2D>();
        ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.viewport = scrollObject.GetComponent<RectTransform>();

        GameObject content = CreateRect("Content", scrollObject.transform);
        listContent = content.GetComponent<RectTransform>();
        listContent.anchorMin = new Vector2(0f, 1f);
        listContent.anchorMax = new Vector2(1f, 1f);
        listContent.pivot = new Vector2(0.5f, 1f);
        listContent.offsetMin = Vector2.zero;
        listContent.offsetMax = Vector2.zero;
        VerticalLayoutGroup listLayout = content.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 10;
        listLayout.padding = new RectOffset(8, 8, 8, 8);
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = listContent;

        emptyText = AddText(card.transform, "No words yet. Study an object to add one.", 24, FontStyle.Normal, new Color(0.8f, 0.8f, 0.76f), 60, TextAnchor.MiddleCenter);
        emptyText.gameObject.SetActive(false);

        GameObject closeObject = CreateRect("Close", card.transform);
        LayoutElement closeLayout = closeObject.AddComponent<LayoutElement>();
        closeLayout.preferredHeight = 64;
        Image closeImage = closeObject.AddComponent<Image>();
        closeImage.color = new Color(0.72f, 0.42f, 0.22f, 1f);
        Button closeButton = closeObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(Close);
        Text closeText = AddText(closeObject.transform, "Close", 28, FontStyle.Bold, Color.white, 64, TextAnchor.MiddleCenter);
        Stretch(closeText.rectTransform, Vector2.zero, Vector2.one);
    }

    void CreateFilterButton(Transform parent, string label, DictionaryFilter nextFilter)
    {
        GameObject buttonObject = CreateRect(label, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.28f, 0.26f, 0.24f, 1f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => SetFilter(nextFilter));
        Text text = AddText(buttonObject.transform, label, 22, FontStyle.Bold, Color.white, 40, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, Vector2.zero, Vector2.one);
    }

    static string EmptyMessage(DictionaryFilter currentFilter)
    {
        if (currentFilter == DictionaryFilter.Recent)
        {
            return "No words were discovered in the last 7 days.";
        }

        if (currentFilter == DictionaryFilter.Revision)
        {
            return "No words need revision.";
        }

        return "No words yet. Study an object to add one.";
    }

    Text AddText(Transform parent, string value, int size, FontStyle style, Color color, float height, TextAnchor alignment)
    {
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = uiFont;
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

    static GameObject CreateRect(string name, Transform parent)
    {
        GameObject item = new GameObject(name);
        item.transform.SetParent(parent, false);
        item.AddComponent<RectTransform>();
        return item;
    }

    static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventObject = new GameObject("EventSystem");
        eventObject.AddComponent<EventSystem>();
        eventObject.AddComponent<InputSystemUIInputModule>();
    }

    static Font FindJapaneseFont()
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
}
