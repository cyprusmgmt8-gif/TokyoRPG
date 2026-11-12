using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Talk prompt and conversation screen.
/// The player object creates this when the scene starts. The words come from a Dialogue Catalog.
/// </summary>
public class DialoguePanel : MonoBehaviour
{
    public static bool IsOpenNow { get; private set; }

    PlayerInteractor interactor;
    FirstPersonController pausedMovement;
    DialogueCatalog catalog;
    Font font;
    GameObject promptRoot;
    GameObject screenRoot;
    Text speakerJapanese;
    Text speakerReading;
    Text speakerRomaji;
    Text speakerEnglish;
    Text lineJapanese;
    Text lineReading;
    Text lineRomaji;
    Text lineEnglish;
    GameObject learnButtonRoot;
    Text learnStatus;
    string currentPhraseId = "";
    RectTransform choiceContent;
    bool isOpen;

    void Awake()
    {
        interactor = GetComponent<PlayerInteractor>();
        font = MenuUi.FindFont();
        MenuUi.EnsureEventSystem();
        try
        {
            BuildPrompt();
            BuildScreen();
        }
        finally
        {
            if (screenRoot != null)
            {
                screenRoot.SetActive(false);
            }

            if (promptRoot != null)
            {
                promptRoot.SetActive(false);
            }
        }
    }

    void OnDisable()
    {
        if (isOpen)
        {
            isOpen = false;
            IsOpenNow = false;
        }
    }

    void Update()
    {
        if (isOpen && LearningScreenOpen())
        {
            Close();
        }

        if (isOpen)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        bool showPrompt = !isOpen && !LearningScreenOpen() && interactor != null && interactor.TalkTarget != null;
        if (promptRoot.activeSelf != showPrompt)
        {
            promptRoot.SetActive(showPrompt);
        }
    }

    public void Open(DialoguePoint point)
    {
        Open(point, null);
    }

    /// <summary>
    /// Opens the conversation. An empty line id uses the catalog's normal start line.
    /// The register passes the bill line so the greeting stays on the cashier.
    /// </summary>
    public void Open(DialoguePoint point, string lineId)
    {
        if (isOpen || point == null || LearningScreenOpen())
        {
            return;
        }

        BasketPanel basketPanel = FindAnyObjectByType<BasketPanel>();
        if (basketPanel != null && BasketPanel.IsOpenNow)
        {
            basketPanel.Close();
        }

        catalog = point.Catalog;
        screenRoot.SetActive(true);
        isOpen = true;
        IsOpenNow = true;
        string startId = string.IsNullOrWhiteSpace(lineId) ? catalog != null ? catalog.StartLineId : "" : lineId;
        if (catalog != null && catalog.TryGet(startId, out DialogueLine line))
        {
            Show(line);
        }
        else
        {
            ShowMissing();
        }

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
        catalog = null;

        if (pausedMovement != null && !LearningScreenOpen() && !BasketPanel.IsOpenNow && !QuestPanel.IsOpenNow && !CheckoutPanel.IsOpenNow)
        {
            pausedMovement.enabled = true;
        }

        pausedMovement = null;
    }

    void OpenFromPrompt()
    {
        if (interactor == null || interactor.TalkTarget == null)
        {
            return;
        }

        Open(interactor.TalkTarget);
    }

    void Show(DialogueLine line)
    {
        ShowSpeaker();
        JapanesePhrase phrase = line != null && line.phrase != null ? line.phrase : new JapanesePhrase();
        currentPhraseId = phrase.id ?? "";
        ShowPhrase(phrase);
        RefreshLearn(currentPhraseId);
        ClearChoices();
        if (line == null || line.choices == null)
        {
            return;
        }

        for (int i = 0; i < line.choices.Count; i++)
        {
            AddChoice(line.choices[i]);
        }
    }

    void ShowMissing()
    {
        ShowSpeaker();
        currentPhraseId = "";
        lineJapanese.text = "……";
        lineReading.gameObject.SetActive(false);
        lineRomaji.gameObject.SetActive(false);
        lineEnglish.text = "Nothing to say yet.";
        RefreshLearn("");
        ClearChoices();
    }

    void ShowSpeaker()
    {
        string japanese = catalog != null ? catalog.SpeakerJapanese : "";
        string reading = catalog != null ? catalog.SpeakerReading : "";
        string authored = catalog != null ? catalog.SpeakerRomaji : "";
        speakerJapanese.text = string.IsNullOrWhiteSpace(japanese) ? "NPC" : japanese;
        bool showReading = !string.IsNullOrWhiteSpace(reading) && reading.Trim() != japanese.Trim();
        speakerReading.gameObject.SetActive(showReading);
        speakerReading.text = reading;
        string romaji = JapaneseReading.ShownRomaji(japanese, reading, authored);
        speakerRomaji.gameObject.SetActive(!string.IsNullOrEmpty(romaji));
        speakerRomaji.text = romaji;
        string english = catalog != null ? catalog.SpeakerEnglish : "";
        speakerEnglish.gameObject.SetActive(!string.IsNullOrWhiteSpace(english));
        speakerEnglish.text = english;
    }

    void ShowPhrase(JapanesePhrase phrase)
    {
        string japanese = phrase.japanese ?? "";
        string reading = phrase.reading ?? "";
        lineJapanese.text = japanese;
        bool showReading = !string.IsNullOrWhiteSpace(reading) && reading.Trim() != japanese.Trim();
        lineReading.gameObject.SetActive(showReading);
        lineReading.text = reading;
        string romaji = JapaneseReading.ShownRomaji(japanese, reading, phrase.romaji);
        lineRomaji.gameObject.SetActive(!string.IsNullOrEmpty(romaji));
        lineRomaji.text = romaji;
        lineEnglish.text = phrase.english ?? "";
    }

    void AddChoice(DialogueChoice choice)
    {
        if (choice == null || choice.phrase == null)
        {
            return;
        }

        DialogueChoice selected = choice;
        JapanesePhrase phrase = choice.phrase;
        GameObject row = MenuUi.CreateRect("Choice Row", choiceContent);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.minHeight = 118;
        rowLayout.preferredHeight = 128;
        HorizontalLayoutGroup rowGroup = row.AddComponent<HorizontalLayoutGroup>();
        rowGroup.spacing = 8;
        rowGroup.childAlignment = TextAnchor.MiddleCenter;
        rowGroup.childControlWidth = true;
        rowGroup.childControlHeight = true;
        rowGroup.childForceExpandWidth = false;
        rowGroup.childForceExpandHeight = true;

        GameObject buttonObject = MenuUi.CreateRect("Choice", row.transform);
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.minHeight = 108;
        layout.preferredHeight = 118;
        layout.preferredWidth = 520;
        layout.flexibleWidth = 1;
        Image image = buttonObject.AddComponent<Image>();
        image.color = MenuUi.Stone;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => Choose(selected));

        VerticalLayoutGroup group = buttonObject.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(16, 16, 8, 8);
        group.spacing = 2;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

        string japanese = phrase.japanese ?? "";
        MenuUi.AddText(buttonObject.transform, japanese, font, 26, FontStyle.Bold, Color.white, 34, TextAnchor.MiddleLeft);
        string reading = phrase.reading ?? "";
        if (!string.IsNullOrWhiteSpace(reading) && reading.Trim() != japanese.Trim())
        {
            MenuUi.AddText(buttonObject.transform, reading, font, 18, FontStyle.Normal, MenuUi.Paper, 24, TextAnchor.MiddleLeft);
        }

        string romaji = JapaneseReading.ShownRomaji(japanese, reading, phrase.romaji);
        if (!string.IsNullOrEmpty(romaji))
        {
            MenuUi.AddText(buttonObject.transform, romaji, font, 18, FontStyle.Italic, MenuUi.Paper, 24, TextAnchor.MiddleLeft);
        }

        if (!string.IsNullOrWhiteSpace(phrase.english))
        {
            MenuUi.AddText(buttonObject.transform, phrase.english, font, 18, FontStyle.Normal, MenuUi.Muted, 24, TextAnchor.MiddleLeft);
        }

        string phraseId = phrase.id;
        if (!string.IsNullOrWhiteSpace(phraseId))
        {
            string learnId = phraseId.Trim();
            Button learn = MenuUi.CreateButton(row.transform, "Learn", font, MenuUi.Amber, () => LearnPhrase(learnId), 118);
            LayoutElement learnLayout = learn.GetComponent<LayoutElement>();
            learnLayout.preferredWidth = 150;
            learnLayout.flexibleWidth = 0;
        }
    }

    void LearnCurrent()
    {
        LearnPhrase(currentPhraseId);
    }

    void LearnPhrase(string phraseId)
    {
        if (!isOpen)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(phraseId) || VocabularyLibrary.Instance == null || !VocabularyLibrary.Instance.TryGet(phraseId, out VocabularyEntry known))
        {
            SetLearnStatus("That phrase is not ready to learn yet.");
            return;
        }

        if (PersonalDictionary.Instance == null)
        {
            SetLearnStatus("Could not save that phrase.");
            return;
        }

        bool firstTime = PersonalDictionary.Instance.Discover(known.id);
        if (!firstTime)
        {
            SetLearnStatus("Already in your dictionary.");
            return;
        }

        int xp = PlayerProgression.Instance != null ? PlayerProgression.Instance.XpPerDiscovery : 0;
        SetLearnStatus(xp > 0 ? "Phrase learned.  +" + xp + " XP" : "Phrase learned.");
    }

    void RefreshLearn(string phraseId)
    {
        bool canLearn = !string.IsNullOrWhiteSpace(phraseId);
        if (learnButtonRoot != null)
        {
            learnButtonRoot.SetActive(canLearn);
        }

        SetLearnStatus("");
    }

    void SetLearnStatus(string message)
    {
        if (learnStatus == null)
        {
            return;
        }

        learnStatus.text = message ?? "";
        learnStatus.gameObject.SetActive(!string.IsNullOrEmpty(learnStatus.text));
    }

    void Choose(DialogueChoice choice)
    {
        if (!isOpen || choice == null)
        {
            return;
        }

        string action = choice.action ?? "";
        if (string.Equals(action, DialogueChoiceActions.Checkout, System.StringComparison.OrdinalIgnoreCase))
        {
            if (!BasketHasItems() && catalog != null && catalog.TryGet(DialogueLineIds.EmptyBasket, out DialogueLine emptyLine))
            {
                Show(emptyLine);
                return;
            }

            if (BasketHasItems() && catalog != null && catalog.TryGet(DialogueLineIds.Bag, out DialogueLine bagLine))
            {
                Show(bagLine);
                return;
            }

            Close();
            CheckoutPanel checkout = GetComponent<CheckoutPanel>();
            if (checkout != null)
            {
                checkout.Open();
            }

            return;
        }

        if (string.Equals(action, DialogueChoiceActions.Summary, System.StringComparison.OrdinalIgnoreCase))
        {
            Close();
            CheckoutPanel summary = GetComponent<CheckoutPanel>();
            if (summary != null)
            {
                summary.OpenSummary();
            }

            return;
        }

        if (string.Equals(action, DialogueChoiceActions.End, System.StringComparison.OrdinalIgnoreCase))
        {
            Close();
            return;
        }

        if (catalog != null && catalog.TryGet(choice.nextLineId, out DialogueLine next))
        {
            Show(next);
            return;
        }

        Close();
    }

    static bool BasketHasItems()
    {
        ShoppingBasket basket = ShoppingBasket.Instance;
        if (basket == null)
        {
            return false;
        }

        System.Collections.Generic.IReadOnlyList<BasketLine> items = basket.Items;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].quantity > 0)
            {
                return true;
            }
        }

        return false;
    }

    void ClearChoices()
    {
        if (choiceContent == null)
        {
            return;
        }

        for (int i = choiceContent.childCount - 1; i >= 0; i--)
        {
            Destroy(choiceContent.GetChild(i).gameObject);
        }
    }

    static bool LearningScreenOpen()
    {
        bool study = LearningPanel.Current != null && LearningPanel.Current.IsOpen;
        return study || DictionaryPanel.IsOpenNow || GrammarPanel.IsOpenNow || RevisionPanel.IsOpenNow || QuestPanel.IsOpenNow || BasketPanel.IsOpenNow || CheckoutPanel.IsOpenNow;
    }

    void BuildPrompt()
    {
        Canvas canvas = MenuUi.CreateCanvas(transform, "Talk Prompt Canvas", 17);
        promptRoot = MenuUi.CreateRect("Talk Prompt", canvas.transform);
        MenuUi.Stretch(promptRoot.GetComponent<RectTransform>(), new Vector2(0.30f, 0.40f), new Vector2(0.70f, 0.52f));
        Image image = promptRoot.AddComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.1f, 0.88f);
        Button button = promptRoot.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(OpenFromPrompt);
        Text label = MenuUi.AddText(promptRoot.transform, "E  —  Talk", font, 32, FontStyle.Bold, MenuUi.Paper, 56, TextAnchor.MiddleCenter);
        MenuUi.Stretch(label.rectTransform, Vector2.zero, Vector2.one);
        label.GetComponent<LayoutElement>().ignoreLayout = true;
    }

    void BuildScreen()
    {
        Canvas canvas = MenuUi.CreateCanvas(transform, "Dialogue Canvas", 37);
        GameObject dim = MenuUi.CreateRect("Dim", canvas.transform);
        MenuUi.Stretch(dim.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        dim.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        screenRoot = dim;
        GameObject card = MenuUi.CreateCard(dim.transform, new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.92f));
        VerticalLayoutGroup layout = card.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 22, 22);
        layout.spacing = 6;

        speakerJapanese = MenuUi.AddText(card.transform, "", font, 22, FontStyle.Bold, MenuUi.Amber, 30, TextAnchor.MiddleLeft);
        speakerReading = MenuUi.AddText(card.transform, "", font, 18, FontStyle.Normal, MenuUi.Paper, 24, TextAnchor.MiddleLeft);
        speakerRomaji = MenuUi.AddText(card.transform, "", font, 18, FontStyle.Italic, MenuUi.Paper, 24, TextAnchor.MiddleLeft);
        speakerEnglish = MenuUi.AddText(card.transform, "", font, 18, FontStyle.Normal, MenuUi.Muted, 24, TextAnchor.MiddleLeft);
        lineJapanese = MenuUi.AddText(card.transform, "", font, 36, FontStyle.Bold, Color.white, 52, TextAnchor.MiddleLeft);
        lineReading = MenuUi.AddText(card.transform, "", font, 22, FontStyle.Normal, MenuUi.Paper, 30, TextAnchor.MiddleLeft);
        lineRomaji = MenuUi.AddText(card.transform, "", font, 22, FontStyle.Italic, MenuUi.Paper, 30, TextAnchor.MiddleLeft);
        lineEnglish = MenuUi.AddText(card.transform, "", font, 22, FontStyle.Normal, MenuUi.Muted, 32, TextAnchor.MiddleLeft);
        Button learnPhrase = MenuUi.CreateButton(card.transform, "Learn Phrase", font, MenuUi.Amber, LearnCurrent, 64);
        learnButtonRoot = learnPhrase.gameObject;
        learnStatus = MenuUi.AddText(card.transform, "", font, 20, FontStyle.Bold, new Color(0.78f, 0.9f, 0.75f), 28, TextAnchor.MiddleLeft);
        learnStatus.gameObject.SetActive(false);
        MenuUi.AddText(card.transform, "You", font, 18, FontStyle.Bold, MenuUi.Amber, 26, TextAnchor.MiddleLeft);
        MenuUi.CreateScroll(card.transform, out choiceContent);
        Button close = MenuUi.CreateButton(card.transform, "Close", font, MenuUi.Stone, Close, 64);
        close.GetComponent<LayoutElement>().preferredHeight = 64;
    }
}
