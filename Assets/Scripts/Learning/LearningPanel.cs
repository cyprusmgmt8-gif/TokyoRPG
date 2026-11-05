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

    public static LearningPanel Current { get; private set; }

    public bool IsOpen { get; private set; }

    GameObject panelRoot;
    GameObject promptRoot;
    Text promptText;
    Text titleText;
    Text japaneseText;
    Text wordRomajiText;
    Text readingText;
    Text translationText;
    Text exampleText;
    Text exampleRomajiText;
    Text exampleReadingText;
    Text exampleTranslationText;
    Text grammarText;
    Text audioText;
    Text dictionaryStatusText;
    Text wordButtonLabel;
    Text exampleButtonLabel;
    Button wordButton;
    Button exampleButton;
    GameObject exampleSection;
    RectTransform cardRect;
    RectTransform studyContent;
    LayoutElement scrollElement;
    AudioClip currentWordClip;
    AudioClip currentExampleClip;
    string currentVocabularyId = "";
    GameObject grammarButton;
    FirstPersonController pausedMovement;
    Font uiFont;

    void Awake()
    {
        Current = this;
        uiFont = FindJapaneseFont();
        EnsureEventSystem();
        JapaneseAudioPlayer.Ensure();
        BuildCanvas();
        panelRoot.SetActive(false);
        promptRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (Current == this)
        {
            Current = null;
        }
    }

    void LateUpdate()
    {
        if (IsOpen && promptRoot != null && promptRoot.activeSelf)
        {
            promptRoot.SetActive(false);
        }
    }

    void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !GrammarPanel.IsOpenNow && !RevisionPanel.IsOpenNow)
        {
            Close();
        }

        RefreshSpeakerButtons();
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

        JapaneseAudioPlayer.Ensure().Stop();
        titleText.text = target.PromptName;
        if (!target.TryGetEntry(out VocabularyEntry word))
        {
            japaneseText.text = "No vocabulary entry";
            readingText.text = target.VocabularyId;
            translationText.text = "Add this id to the vocabulary catalog.";
            currentWordClip = null;
            currentExampleClip = null;
            currentVocabularyId = "";
            exampleSection.SetActive(false);
            grammarButton.SetActive(false);
            SetLine(wordRomajiText, string.Empty);
            SetLine(exampleRomajiText, string.Empty);
            SetLine(exampleReadingText, string.Empty);
            SetLine(exampleTranslationText, string.Empty);
            SetLine(grammarText, string.Empty);
            SetLine(audioText, string.Empty);
            SetLine(dictionaryStatusText, string.Empty);
        }
        else
        {
            japaneseText.text = WordOrFallback(word.japanese);
            FitText(japaneseText, 16, 68f);
            readingText.text = WordOrFallback(word.reading);
            FitText(readingText, 28, 36f);
            SetLine(wordRomajiText, JapaneseReading.ShownRomaji(word.japanese, word.reading, word.romaji));
            translationText.text = WordOrFallback(word.translation);
            FitText(translationText, 36, 40f);
            currentWordClip = word.wordClip;
            currentExampleClip = word.exampleClip;
            currentVocabularyId = word.id;
            bool hasGrammar = GrammarLibrary.Instance != null
                && GrammarLibrary.Instance.TryGetSentence(word.id, out GrammarSentenceNote note)
                && note != null;
            grammarButton.SetActive(hasGrammar);
            bool hasExample = !string.IsNullOrWhiteSpace(word.exampleSentence);
            exampleSection.SetActive(hasExample);
            if (hasExample)
            {
                exampleText.text = word.exampleSentence;
                FitText(exampleText, 22, 44f);
            }

            SetLine(exampleRomajiText, JapaneseReading.ShownRomaji(word.exampleSentence, word.exampleReading, word.exampleRomaji));
            SetLine(exampleReadingText, word.exampleReading);
            SetLine(exampleTranslationText, word.exampleTranslation);
            SetLine(grammarText, word.grammarExplanation);
            SetLine(audioText, MissingRecordingMessage(word));
            bool added = PersonalDictionary.Instance != null && PersonalDictionary.Instance.Discover(word.id);
            string status = added ? "Saved to your dictionary." : "Already in your dictionary.";
            if (added && PlayerProgression.Instance != null)
            {
                status += " +" + PlayerProgression.Instance.XpPerDiscovery + " XP.";
            }

            SetLine(dictionaryStatusText, status);
        }

        RefreshSpeakerButtons();

        panelRoot.SetActive(true);
        promptRoot.SetActive(false);
        IsOpen = true;

        pausedMovement = movement;
        if (pausedMovement != null)
        {
            pausedMovement.enabled = false;
        }

        RefreshCardSize();
        GameplayHud.Apply();
    }

    public void Close()
    {
        if (GrammarPanel.IsOpenNow && GrammarPanel.Instance != null)
        {
            GrammarPanel.Instance.Close();
        }

        if (JapaneseAudioPlayer.Instance != null)
        {
            JapaneseAudioPlayer.Instance.Stop();
        }

        panelRoot.SetActive(false);
        IsOpen = false;

        if (pausedMovement != null)
        {
            pausedMovement.enabled = true;
            pausedMovement = null;
        }

        GameplayHud.Apply();
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
            FitText(text, 34, text.fontSize + 14f);
        }
    }

    static void FitText(Text text, int charsPerLine, float minHeight)
    {
        LayoutElement element = text.GetComponent<LayoutElement>();
        if (element == null)
        {
            return;
        }

        int lines = WrapLines(text.text, charsPerLine);
        float height = Mathf.Max(minHeight, lines * (text.fontSize + 12f));
        element.preferredHeight = height;
        element.minHeight = height;
    }

    static int WrapLines(string value, int charsPerLine)
    {
        if (string.IsNullOrEmpty(value) || charsPerLine < 1)
        {
            return 1;
        }

        int lines = 1;
        int column = 0;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == '\n')
            {
                lines++;
                column = 0;
                continue;
            }

            column++;
            if (column >= charsPerLine)
            {
                lines++;
                column = 0;
            }
        }

        return Mathf.Max(1, lines);
    }

    GameObject CreateSection(Transform parent, string name, float spacing, int padding, bool framed)
    {
        GameObject section = CreateRect(name, parent);
        if (framed)
        {
            Image background = section.AddComponent<Image>();
            background.color = new Color(0.09f, 0.085f, 0.08f, 1f);
        }

        VerticalLayoutGroup group = section.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(padding, padding, padding, padding);
        group.spacing = spacing;
        group.childAlignment = TextAnchor.UpperCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;
        return section;
    }

    void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("Study Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 35;

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
        cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(980f, 480f);
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.14f, 0.13f, 0.12f, 0.98f);
        ContentSizeFitter cardFit = card.AddComponent<ContentSizeFitter>();
        cardFit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        cardFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(36, 36, 28, 28);
        layout.spacing = 16;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        titleText = AddLine(card.transform, "Title", 24, FontStyle.Bold, new Color(0.85f, 0.78f, 0.66f), 32);
        RectTransform scroll = MenuUi.CreateScroll(card.transform, out studyContent);
        scrollElement = scroll.GetComponent<LayoutElement>();
        scrollElement.flexibleHeight = 0f;
        Image scrollImage = scroll.GetComponent<Image>();
        if (scrollImage != null)
        {
            scrollImage.color = new Color(0f, 0f, 0f, 0f);
        }

        VerticalLayoutGroup contentLayout = studyContent.GetComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 20f;
        contentLayout.padding = new RectOffset(8, 8, 4, 8);
        contentLayout.childAlignment = TextAnchor.UpperCenter;

        GameObject wordGroup = CreateSection(studyContent, "Word", 8, 4, false);
        japaneseText = AddLine(wordGroup.transform, "Japanese", 56, FontStyle.Bold, Color.white, 72);
        readingText = AddLine(wordGroup.transform, "Reading", 28, FontStyle.Normal, new Color(1f, 0.86f, 0.55f), 36);
        wordRomajiText = AddLine(wordGroup.transform, "Word Romaji", 24, FontStyle.Italic, new Color(1f, 0.86f, 0.55f), 34);
        translationText = AddLine(wordGroup.transform, "Translation", 28, FontStyle.Normal, new Color(0.93f, 0.93f, 0.9f), 40);
        wordButton = CreateCenteredButton(wordGroup.transform, "Word Play", new Color(0.22f, 0.32f, 0.4f, 1f), 200f, out wordButtonLabel);
        wordButton.onClick.AddListener(() => ToggleClip(currentWordClip));

        exampleSection = CreateSection(studyContent, "Example Section", 8, 20, true);
        Text exampleHeading = AddLine(exampleSection.transform, "Example Heading", 20, FontStyle.Bold, new Color(0.78f, 0.72f, 0.62f), 28);
        exampleHeading.text = "Example Sentence";
        exampleText = AddLine(exampleSection.transform, "Example", 32, FontStyle.Normal, Color.white, 48);
        exampleReadingText = AddLine(exampleSection.transform, "Example Reading", 24, FontStyle.Normal, new Color(1f, 0.86f, 0.55f), 34);
        exampleRomajiText = AddLine(exampleSection.transform, "Example Romaji", 22, FontStyle.Italic, new Color(1f, 0.86f, 0.55f), 32);
        exampleTranslationText = AddLine(exampleSection.transform, "Example Translation", 24, FontStyle.Italic, new Color(0.86f, 0.86f, 0.82f), 36);
        exampleButton = CreateCenteredButton(exampleSection.transform, "Example Play", new Color(0.22f, 0.32f, 0.4f, 1f), 220f, out exampleButtonLabel);
        exampleButton.onClick.AddListener(() => ToggleClip(currentExampleClip));
        exampleSection.SetActive(false);

        GameObject footer = CreateSection(studyContent, "Footer", 8, 0, false);
        grammarText = AddLine(footer.transform, "Grammar", 20, FontStyle.Normal, new Color(0.78f, 0.84f, 0.78f), 32);
        grammarButton = CreateRect("Grammar Button", footer.transform);
        LayoutElement grammarRow = grammarButton.AddComponent<LayoutElement>();
        grammarRow.preferredHeight = 44f;
        grammarRow.minHeight = 44f;
        HorizontalLayoutGroup grammarAlign = grammarButton.AddComponent<HorizontalLayoutGroup>();
        grammarAlign.childAlignment = TextAnchor.MiddleCenter;
        grammarAlign.childControlWidth = true;
        grammarAlign.childControlHeight = true;
        grammarAlign.childForceExpandWidth = false;
        grammarAlign.childForceExpandHeight = true;
        GameObject grammarObject = CreateRect("Grammar", grammarButton.transform);
        LayoutElement grammarLayout = grammarObject.AddComponent<LayoutElement>();
        grammarLayout.preferredWidth = 220f;
        grammarLayout.preferredHeight = 40f;
        Image grammarImage = grammarObject.AddComponent<Image>();
        grammarImage.color = new Color(0.28f, 0.26f, 0.24f, 1f);
        Button grammar = grammarObject.AddComponent<Button>();
        grammar.targetGraphic = grammarImage;
        grammar.onClick.AddListener(OpenGrammar);
        Text grammarLabel = CreateLabel(grammarObject.transform, "Grammar Label", 20, FontStyle.Normal, new Color(0.9f, 0.86f, 0.78f));
        Stretch(grammarLabel.rectTransform, Vector2.zero, Vector2.one);
        grammarLabel.alignment = TextAnchor.MiddleCenter;
        grammarLabel.text = "Grammar";
        grammarButton.SetActive(false);

        audioText = AddLine(footer.transform, "Audio", 18, FontStyle.Normal, new Color(0.62f, 0.6f, 0.56f), 28);
        dictionaryStatusText = AddLine(footer.transform, "Dictionary Status", 18, FontStyle.Normal, new Color(0.62f, 0.6f, 0.56f), 28);

        GameObject closeObject = CreateRect("Close", card.transform);
        LayoutElement closeLayout = closeObject.AddComponent<LayoutElement>();
        closeLayout.preferredHeight = 56;
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

    void OpenGrammar()
    {
        if (string.IsNullOrEmpty(currentVocabularyId))
        {
            return;
        }

        if (GrammarPanel.Instance == null)
        {
            gameObject.AddComponent<GrammarPanel>();
        }

        GrammarPanel.Instance.ShowSentence(currentVocabularyId);
    }

    void ToggleClip(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        JapaneseAudioPlayer player = JapaneseAudioPlayer.Ensure();
        if (player.IsPlaying(clip))
        {
            player.Stop();
        }
        else
        {
            player.Play(clip);
        }

        RefreshSpeakerButtons();
    }

    void RefreshSpeakerButtons()
    {
        SetSpeakerLabel(wordButton, wordButtonLabel, currentWordClip);
        SetSpeakerLabel(exampleButton, exampleButtonLabel, currentExampleClip);
    }

    static void SetSpeakerLabel(Button button, Text label, AudioClip clip)
    {
        if (button == null || label == null)
        {
            return;
        }

        if (clip == null)
        {
            button.interactable = false;
            label.text = "No recording";
            return;
        }

        button.interactable = true;
        bool playing = JapaneseAudioPlayer.Instance != null && JapaneseAudioPlayer.Instance.IsPlaying(clip);
        label.text = playing ? "Stop" : "Play";
    }

    static string MissingRecordingMessage(VocabularyEntry word)
    {
        if (word.wordClip != null && word.exampleClip != null)
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(word.audioReference))
        {
            return "No recording yet. Add audio clips on this word in the vocabulary catalog.";
        }

        return "No recording yet. File name: " + word.audioReference;
    }

    void RefreshCardSize()
    {
        if (studyContent == null || scrollElement == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(studyContent);
        float contentHeight = LayoutUtility.GetPreferredHeight(studyContent);
        scrollElement.preferredHeight = Mathf.Clamp(contentHeight + 8f, 180f, 760f);
        scrollElement.flexibleHeight = 0f;
        if (cardRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardRect);
        }
    }

    Button CreateCenteredButton(Transform parent, string name, Color color, float width, out Text label)
    {
        GameObject row = CreateRect(name + " Row", parent);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 52f;
        rowLayout.minHeight = 52f;
        HorizontalLayoutGroup group = row.AddComponent<HorizontalLayoutGroup>();
        group.childAlignment = TextAnchor.MiddleCenter;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = true;

        GameObject buttonObject = CreateRect(name, row.transform);
        LayoutElement buttonLayout = buttonObject.AddComponent<LayoutElement>();
        buttonLayout.preferredWidth = width;
        buttonLayout.preferredHeight = 48f;
        Image image = buttonObject.AddComponent<Image>();
        image.color = color;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        label = CreateLabel(buttonObject.transform, name + " Label", 22, FontStyle.Bold, Color.white);
        Stretch(label.rectTransform, Vector2.zero, Vector2.one);
        label.alignment = TextAnchor.MiddleCenter;
        label.text = "Play";
        return button;
    }

    Text AddLine(Transform parent, string name, int size, FontStyle style, Color color, float height)
    {
        Text text = CreateLabel(parent, name, size, style, color);
        LayoutElement element = text.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
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
