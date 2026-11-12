using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Checkout prompt and payment screen for the Konbini register.
/// Shopping Basket adds this when the Konbini player starts.
/// </summary>
public class CheckoutPanel : MonoBehaviour
{
    public static bool IsOpenNow { get; private set; }

    struct Phrase
    {
        public string japanese;
        public string reading;
        public string romaji;
        public string english;
    }

    static readonly Phrase[] phrases =
    {
        new Phrase
        {
            japanese = "お会計をお願いします。",
            reading = "おかいけいをおねがいします。",
            romaji = "Okaikei o onegai shimasu.",
            english = "The bill, please."
        },
        new Phrase
        {
            japanese = "袋は大丈夫です。",
            reading = "ふくろはだいじょうぶです。",
            romaji = "Fukuro wa daijoubu desu.",
            english = "I don't need a bag."
        },
        new Phrase
        {
            japanese = "カードでお願いします。",
            reading = "カードでおねがいします。",
            romaji = "Kaado de onegai shimasu.",
            english = "By card, please."
        }
    };

    ShoppingBasket basket;
    PlayerInteractor interactor;
    FirstPersonController pausedMovement;
    Font font;
    GameObject promptRoot;
    GameObject screenRoot;
    RectTransform listContent;
    Text emptyText;
    Text totalText;
    Text phraseJapanese;
    Text phraseReading;
    Text phraseRomaji;
    Text phraseEnglish;
    GameObject phraseRoot;
    Button actionButton;
    Text actionLabel;
    Text successText;
    bool isOpen;
    bool purchased;
    bool summaryOnly;
    int phraseIndex;

    void Awake()
    {
        basket = GetComponent<ShoppingBasket>();
        interactor = GetComponent<PlayerInteractor>();
        font = MenuUi.FindFont();
        MenuUi.EnsureEventSystem();
        BuildPrompt();
        BuildScreen();
        screenRoot.SetActive(false);
        promptRoot.SetActive(false);
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

        bool showPrompt = !isOpen && !LearningScreenOpen() && interactor != null && interactor.CheckoutTarget != null;
        if (promptRoot.activeSelf != showPrompt)
        {
            promptRoot.SetActive(showPrompt);
        }
    }

    public void Open()
    {
        summaryOnly = false;
        OpenScreen();
    }

    /// <summary>
    /// Opens the basket summary after the cashier has already asked about the bag and payment.
    /// Those spoken lines are not shown again. Confirm still clears the basket once.
    /// </summary>
    public void OpenSummary()
    {
        summaryOnly = true;
        OpenScreen();
    }

    void OpenScreen()
    {
        if (isOpen || LearningScreenOpen())
        {
            return;
        }

        BasketPanel basketPanel = FindAnyObjectByType<BasketPanel>();
        if (basketPanel != null && BasketPanel.IsOpenNow)
        {
            basketPanel.Close();
        }

        purchased = false;
        phraseIndex = 0;
        screenRoot.SetActive(true);
        isOpen = true;
        IsOpenNow = true;
        Rebuild();

        pausedMovement = GetComponent<FirstPersonController>();
        if (pausedMovement != null)
        {
            pausedMovement.enabled = false;
        }
    }

    public void Close()
    {
        bool thankThePlayer = isOpen && summaryOnly && purchased;
        summaryOnly = false;
        screenRoot.SetActive(false);
        isOpen = false;
        IsOpenNow = false;

        if (pausedMovement != null && !LearningScreenOpen() && !BasketPanel.IsOpenNow && !QuestPanel.IsOpenNow && !DialoguePanel.IsOpenNow)
        {
            pausedMovement.enabled = true;
        }

        pausedMovement = null;
        if (thankThePlayer)
        {
            ShowPurchaseThanks();
        }
    }

    void Advance()
    {
        if (purchased)
        {
            if (summaryOnly)
            {
                ContinueAfterPurchase();
            }

            return;
        }

        if (basket == null || basket.Items.Count == 0)
        {
            return;
        }

        if (summaryOnly)
        {
            ConfirmPurchase();
            return;
        }

        if (phraseIndex < phrases.Length - 1)
        {
            phraseIndex++;
            ShowPhrase();
            return;
        }

        ConfirmPurchase();
    }

    void ConfirmPurchase()
    {
        if (purchased || basket == null || basket.Items.Count == 0)
        {
            return;
        }

        purchased = true;
        basket.Clear();
        phraseRoot.SetActive(false);
        emptyText.gameObject.SetActive(false);
        successText.gameObject.SetActive(true);
        successText.text = "Purchase complete.";
        totalText.text = "Total  ¥0";
        ClearRows();
        if (summaryOnly && actionButton != null)
        {
            actionButton.gameObject.SetActive(true);
            if (actionLabel != null)
            {
                actionLabel.text = "Continue";
            }
        }
        else if (actionButton != null)
        {
            actionButton.gameObject.SetActive(false);
        }
    }

    void ContinueAfterPurchase()
    {
        Close();
    }

    void ShowPurchaseThanks()
    {
        DialoguePanel talk = GetComponent<DialoguePanel>();
        DialoguePoint speaker = FindAnyObjectByType<DialoguePoint>();
        if (talk == null || speaker == null || speaker.Catalog == null)
        {
            return;
        }

        if (!speaker.Catalog.TryGet(DialogueLineIds.PurchaseThanks, out _))
        {
            return;
        }

        talk.Open(speaker, DialogueLineIds.PurchaseThanks);
    }

    void Rebuild()
    {
        ClearRows();
        successText.gameObject.SetActive(false);
        IReadOnlyList<BasketLine> items = basket != null ? basket.Items : new List<BasketLine>();
        int shown = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null || items[i].quantity <= 0)
            {
                continue;
            }

            shown++;
            CreateRow(items[i]);
        }

        bool empty = shown == 0;
        emptyText.gameObject.SetActive(empty);
        emptyText.text = "Your basket is empty.";
        phraseRoot.SetActive(!empty && !purchased && !summaryOnly);
        actionButton.gameObject.SetActive(!empty && !purchased);
        int total = basket != null ? basket.TotalYen : 0;
        totalText.text = "Total  ¥" + total;
        if (!empty && summaryOnly && actionLabel != null)
        {
            actionLabel.text = "Confirm purchase";
        }
        else if (!empty)
        {
            phraseIndex = 0;
            ShowPhrase();
        }
    }

    void ShowPhrase()
    {
        Phrase phrase = phrases[Mathf.Clamp(phraseIndex, 0, phrases.Length - 1)];
        phraseJapanese.text = phrase.japanese;
        phraseReading.text = phrase.reading;
        string romaji = JapaneseReading.ShownRomaji(phrase.japanese, phrase.reading, phrase.romaji);
        phraseRomaji.gameObject.SetActive(!string.IsNullOrEmpty(romaji));
        phraseRomaji.text = romaji;
        phraseEnglish.text = phrase.english;
        if (actionLabel != null)
        {
            actionLabel.text = phraseIndex >= phrases.Length - 1 ? "Confirm purchase" : "Next";
        }
    }

    void CreateRow(BasketLine line)
    {
        string japanese = line.vocabularyId;
        string romaji = "";
        string english = "";
        if (VocabularyLibrary.Instance != null && VocabularyLibrary.Instance.TryGet(line.vocabularyId, out VocabularyEntry entry))
        {
            japanese = string.IsNullOrWhiteSpace(entry.japanese) ? line.vocabularyId : entry.japanese;
            romaji = JapaneseReading.ShownRomaji(entry.japanese, entry.reading, entry.romaji);
            english = entry.translation;
        }

        int price = 0;
        bool priced = basket != null && basket.Catalog != null && basket.Catalog.TryGetPrice(line.vocabularyId, out price);
        int lineTotal = priced ? price * line.quantity : 0;
        string money = priced
            ? "¥" + price + " each    × " + line.quantity + "    ¥" + lineTotal
            : "× " + line.quantity;

        GameObject row = MenuUi.CreateRect("Item", listContent);
        VerticalLayoutGroup layout = row.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 2;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        MenuUi.AddText(row.transform, japanese, font, 28, FontStyle.Bold, Color.white, 36, TextAnchor.MiddleLeft);
        if (!string.IsNullOrEmpty(romaji))
        {
            MenuUi.AddText(row.transform, romaji, font, 20, FontStyle.Italic, MenuUi.Paper, 28, TextAnchor.MiddleLeft);
        }

        if (!string.IsNullOrWhiteSpace(english))
        {
            MenuUi.AddText(row.transform, english, font, 20, FontStyle.Normal, MenuUi.Muted, 28, TextAnchor.MiddleLeft);
        }

        MenuUi.AddText(row.transform, money, font, 20, FontStyle.Bold, MenuUi.Paper, 28, TextAnchor.MiddleLeft);
    }

    void ClearRows()
    {
        if (listContent == null)
        {
            return;
        }

        for (int i = listContent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(listContent.GetChild(i).gameObject);
        }
    }

    static bool LearningScreenOpen()
    {
        bool study = LearningPanel.Current != null && LearningPanel.Current.IsOpen;
        return study || DictionaryPanel.IsOpenNow || GrammarPanel.IsOpenNow || RevisionPanel.IsOpenNow || QuestPanel.IsOpenNow || BasketPanel.IsOpenNow || DialoguePanel.IsOpenNow;
    }

    void BuildPrompt()
    {
        Canvas canvas = MenuUi.CreateCanvas(transform, "Checkout Prompt Canvas", 17);
        promptRoot = MenuUi.CreateRect("Checkout Prompt", canvas.transform);
        MenuUi.Stretch(promptRoot.GetComponent<RectTransform>(), new Vector2(0.30f, 0.40f), new Vector2(0.70f, 0.52f));
        Image image = promptRoot.AddComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.1f, 0.88f);
        Button button = promptRoot.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(Open);
        Text label = MenuUi.AddText(promptRoot.transform, "E  —  Checkout", font, 32, FontStyle.Bold, MenuUi.Paper, 56, TextAnchor.MiddleCenter);
        MenuUi.Stretch(label.rectTransform, Vector2.zero, Vector2.one);
        label.GetComponent<LayoutElement>().ignoreLayout = true;
    }

    void BuildScreen()
    {
        Canvas canvas = MenuUi.CreateCanvas(transform, "Checkout Screen Canvas", 36);
        screenRoot = MenuUi.CreateRect("Checkout Screen", canvas.transform);
        MenuUi.Stretch(screenRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        Image dim = screenRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject card = MenuUi.CreateRect("Card", screenRoot.transform);
        MenuUi.Stretch(card.GetComponent<RectTransform>(), new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.94f));
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.14f, 0.13f, 0.12f, 0.98f);
        VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(24, 24, 18, 18);
        cardLayout.spacing = 10;
        cardLayout.childAlignment = TextAnchor.UpperCenter;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        MenuUi.AddText(card.transform, "Checkout", font, 34, FontStyle.Bold, Color.white, 44, TextAnchor.MiddleCenter);

        GameObject scrollObject = MenuUi.CreateRect("Scroll", card.transform);
        LayoutElement scrollLayout = scrollObject.AddComponent<LayoutElement>();
        scrollLayout.preferredHeight = 280;
        scrollLayout.flexibleHeight = 1;
        scrollObject.AddComponent<RectMask2D>();
        ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.viewport = scrollObject.GetComponent<RectTransform>();
        GameObject content = MenuUi.CreateRect("Content", scrollObject.transform);
        listContent = content.GetComponent<RectTransform>();
        listContent.anchorMin = new Vector2(0f, 1f);
        listContent.anchorMax = new Vector2(1f, 1f);
        listContent.pivot = new Vector2(0.5f, 1f);
        listContent.offsetMin = Vector2.zero;
        listContent.offsetMax = Vector2.zero;
        VerticalLayoutGroup listLayout = content.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 8;
        listLayout.padding = new RectOffset(8, 8, 8, 8);
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = listContent;

        emptyText = MenuUi.AddText(card.transform, "Your basket is empty.", font, 24, FontStyle.Normal, MenuUi.Muted, 40, TextAnchor.MiddleCenter);
        totalText = MenuUi.AddText(card.transform, "Total  ¥0", font, 26, FontStyle.Bold, MenuUi.Paper, 36, TextAnchor.MiddleCenter);

        phraseRoot = MenuUi.CreateRect("Phrase", card.transform);
        Image phraseImage = phraseRoot.AddComponent<Image>();
        phraseImage.color = new Color(0.09f, 0.085f, 0.08f, 1f);
        VerticalLayoutGroup phraseLayout = phraseRoot.AddComponent<VerticalLayoutGroup>();
        phraseLayout.padding = new RectOffset(16, 16, 12, 12);
        phraseLayout.spacing = 4;
        phraseLayout.childAlignment = TextAnchor.UpperCenter;
        phraseLayout.childControlWidth = true;
        phraseLayout.childControlHeight = true;
        phraseLayout.childForceExpandWidth = true;
        phraseLayout.childForceExpandHeight = false;
        phraseJapanese = MenuUi.AddText(phraseRoot.transform, "", font, 32, FontStyle.Bold, Color.white, 48, TextAnchor.MiddleCenter);
        phraseReading = MenuUi.AddText(phraseRoot.transform, "", font, 22, FontStyle.Normal, MenuUi.Paper, 32, TextAnchor.MiddleCenter);
        phraseRomaji = MenuUi.AddText(phraseRoot.transform, "", font, 22, FontStyle.Italic, MenuUi.Paper, 32, TextAnchor.MiddleCenter);
        phraseEnglish = MenuUi.AddText(phraseRoot.transform, "", font, 22, FontStyle.Normal, MenuUi.Muted, 32, TextAnchor.MiddleCenter);

        actionButton = MenuUi.CreateButton(card.transform, "Next", font, MenuUi.Amber, Advance, 58);
        actionLabel = actionButton.GetComponentInChildren<Text>();
        successText = MenuUi.AddText(card.transform, "Purchase complete.", font, 26, FontStyle.Bold, MenuUi.Paper, 40, TextAnchor.MiddleCenter);
        successText.gameObject.SetActive(false);
        MenuUi.CreateButton(card.transform, "Close", font, MenuUi.Stone, Close, 56);
    }
}
