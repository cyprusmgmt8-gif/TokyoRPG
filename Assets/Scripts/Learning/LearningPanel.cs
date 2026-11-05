using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Builds the study card and the "press E" prompt when the scene starts.
/// The same panel is used for every interactive object.
/// </summary>
public class LearningPanel : MonoBehaviour
{
    public event Action PromptClicked;

    public bool IsOpen { get; private set; }

    GameObject panelRoot;
    GameObject promptRoot;
    Text promptText;
    Text titleText;
    Text japaneseText;
    Text readingText;
    Text translationText;
    Text exampleText;
    Text exampleReadingText;
    Text exampleTranslationText;
    Text grammarText;
    Text audioText;
    Text dictionaryStatusText;
    FirstPersonController pausedMovement;
    Font uiFont;

    void Awake()
    {
        uiFont = FindJapaneseFont();
        EnsureEventSystem();
        BuildCanvas();
        panelRoot.SetActive(false);
        promptRoot.SetActive(false);
    }

    void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    public void SetPrompt(string objectName)
    {
        bool show = !string.IsNullOrEmpty(objectName) && !IsOpen;
        promptRoot.SetActive(show);
        if (show)
        {
            promptText.text = "E    Study " + objectName;
        }
    }

    public void Open(InteractableObject target, FirstPersonController movement)
    {
        if (target == null)
        {
            return;
        }

        titleText.text = target.PromptName;
        if (!target.TryGetEntry(out VocabularyEntry word))
        {
            japaneseText.text = "No vocabulary entry";
            readingText.text = target.VocabularyId;
            translationText.text = "Add this id to the vocabulary catalog.";
            SetLine(exampleText, string.Empty);
            SetLine(exampleReadingText, string.Empty);
            SetLine(exampleTranslationText, string.Empty);
            SetLine(grammarText, string.Empty);
            SetLine(audioText, string.Empty);
            SetLine(dictionaryStatusText, string.Empty);
        }
        else
        {
            japaneseText.text = WordOrFallback(word.japanese);
            readingText.text = WordOrFallback(word.reading);
            translationText.text = WordOrFallback(word.translation);
            SetLine(exampleText, word.exampleSentence);
            SetLine(exampleReadingText, word.exampleReading);
            SetLine(exampleTranslationText, word.exampleTranslation);
            SetLine(grammarText, word.grammarExplanation);
            SetLine(audioText, string.IsNullOrWhiteSpace(word.audioReference) ? string.Empty : "Audio: " + word.audioReference);
            bool added = PersonalDictionary.Instance != null && PersonalDictionary.Instance.Discover(word.id);
            string status = added ? "Saved to your dictionary." : "Already in your dictionary.";
            if (added && PlayerProgression.Instance != null)
            {
                status += " +" + PlayerProgression.Instance.XpPerDiscovery + " XP.";
            }

            SetLine(dictionaryStatusText, status);
        }

        panelRoot.SetActive(true);
        promptRoot.SetActive(false);
        IsOpen = true;

        pausedMovement = movement;
        if (pausedMovement != null)
        {
            pausedMovement.enabled = false;
        }
    }

    public void Close()
    {
        panelRoot.SetActive(false);
        IsOpen = false;

        if (pausedMovement != null)
        {
            pausedMovement.enabled = true;
            pausedMovement = null;
        }
    }

    static string WordOrFallback(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "—" : value;
    }

    static void SetLine(Text text, string value)
    {
        bool show = !string.IsNullOrWhiteSpace(value);
        text.gameObject.SetActive(show);
        if (show)
        {
            text.text = value;
        }
    }

    void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("Study Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        promptRoot = CreateRect("Prompt", canvasObject.transform);
        Stretch(promptRoot.GetComponent<RectTransform>(), new Vector2(0.3f, 0.4f), new Vector2(0.7f, 0.52f));
        Image promptImage = promptRoot.AddComponent<Image>();
        promptImage.color = new Color(0.08f, 0.08f, 0.1f, 0.88f);
        Button promptButton = promptRoot.AddComponent<Button>();
        promptButton.targetGraphic = promptImage;
        promptButton.onClick.AddListener(() => PromptClicked?.Invoke());
        promptText = CreateLabel(promptRoot.transform, "Prompt Label", 36, FontStyle.Bold, new Color(1f, 0.93f, 0.8f));
        Stretch(promptText.rectTransform, Vector2.zero, Vector2.one);
        promptText.alignment = TextAnchor.MiddleCenter;

        panelRoot = CreateRect("Panel", canvasObject.transform);
        Stretch(panelRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        Image dim = panelRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject card = CreateRect("Card", panelRoot.transform);
        Stretch(card.GetComponent<RectTransform>(), new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.14f, 0.13f, 0.12f, 0.98f);

        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(36, 36, 32, 32);
        layout.spacing = 14;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        titleText = AddLine(card.transform, "Title", 28, FontStyle.Bold, new Color(0.85f, 0.78f, 0.66f), 40);
        japaneseText = AddLine(card.transform, "Japanese", 64, FontStyle.Bold, Color.white, 90);
        readingText = AddLine(card.transform, "Reading", 32, FontStyle.Normal, new Color(1f, 0.86f, 0.55f), 48);
        translationText = AddLine(card.transform, "Translation", 32, FontStyle.Normal, new Color(0.9f, 0.9f, 0.88f), 48);
        AddLine(card.transform, "Example Heading", 22, FontStyle.Bold, new Color(0.7f, 0.66f, 0.58f), 32).text = "Example";
        exampleText = AddLine(card.transform, "Example", 28, FontStyle.Normal, Color.white, 64);
        exampleReadingText = AddLine(card.transform, "Example Reading", 24, FontStyle.Normal, new Color(1f, 0.86f, 0.55f), 48);
        exampleTranslationText = AddLine(card.transform, "Example Translation", 24, FontStyle.Italic, new Color(0.82f, 0.82f, 0.78f), 56);
        grammarText = AddLine(card.transform, "Grammar", 22, FontStyle.Normal, new Color(0.78f, 0.84f, 0.78f), 72);
        audioText = AddLine(card.transform, "Audio", 20, FontStyle.Normal, new Color(0.7f, 0.7f, 0.66f), 36);
        dictionaryStatusText = AddLine(card.transform, "Dictionary Status", 22, FontStyle.Bold, new Color(0.78f, 0.9f, 0.75f), 36);

        GameObject closeObject = CreateRect("Close", card.transform);
        LayoutElement closeLayout = closeObject.AddComponent<LayoutElement>();
        closeLayout.preferredHeight = 72;
        Image closeImage = closeObject.AddComponent<Image>();
        closeImage.color = new Color(0.72f, 0.42f, 0.22f, 1f);
        Button closeButton = closeObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(Close);
        Text closeText = CreateLabel(closeObject.transform, "Close Label", 30, FontStyle.Bold, Color.white);
        Stretch(closeText.rectTransform, Vector2.zero, Vector2.one);
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.text = "Close";
    }

    Text AddLine(Transform parent, string name, int size, FontStyle style, Color color, float height)
    {
        Text text = CreateLabel(parent, name, size, style, color);
        LayoutElement element = text.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    Text CreateLabel(Transform parent, string name, int size, FontStyle style, Color color)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.raycastTarget = false;
        text.supportRichText = false;
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
                Font font = Font.CreateDynamicFontFromOSFont(candidates[i], 64);
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
