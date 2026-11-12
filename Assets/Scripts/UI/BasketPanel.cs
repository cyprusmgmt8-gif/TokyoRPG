using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Basket button, add button, and basket screen.
/// Shopping Basket adds this when the Konbini player starts.
/// </summary>
public class BasketPanel : MonoBehaviour
{
    public static bool IsOpenNow { get; private set; }

    const float AddedMessageSeconds = 1.4f;

    ShoppingBasket basket;
    PlayerInteractor interactor;
    FirstPersonController pausedMovement;
    Font font;
    GameObject addRoot;
    Text addedText;
    GameObject screenRoot;
    GameObject confirmRoot;
    RectTransform listContent;
    Text emptyText;
    Text totalText;
    Text countText;
    bool isOpen;
    float addedUntil;

    void Awake()
    {
        basket = GetComponent<ShoppingBasket>();
        interactor = GetComponent<PlayerInteractor>();
        font = MenuUi.FindFont();
        MenuUi.EnsureEventSystem();
        BuildAddButton();
        BuildLauncher();
        BuildScreen();
        screenRoot.SetActive(false);
        confirmRoot.SetActive(false);
        addRoot.SetActive(false);
        addedText.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (basket != null)
        {
            basket.Changed -= Refresh;
            basket.Changed += Refresh;
        }

        Refresh();
    }

    void OnDisable()
    {
        if (basket != null)
        {
            basket.Changed -= Refresh;
        }

        if (isOpen)
        {
            isOpen = false;
            IsOpenNow = false;
        }
    }

    void Update()
    {
        if (isOpen && OtherScreenOpen())
        {
            Close();
        }

        if (isOpen)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (confirmRoot.activeSelf)
                {
                    confirmRoot.SetActive(false);
                }
                else
                {
                    Close();
                }
            }
        }

        RefreshAddButton();
        if (addedText.gameObject.activeSelf && Time.unscaledTime >= addedUntil)
        {
            addedText.gameObject.SetActive(false);
        }
    }

    void Refresh()
    {
        RefreshCount();
        RebuildList();
    }

    void RefreshCount()
    {
        if (countText == null)
        {
            return;
        }

        int count = 0;
        if (basket != null)
        {
            IReadOnlyList<BasketLine> items = basket.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].quantity > 0)
                {
                    count += items[i].quantity;
                }
            }
        }

        countText.text = "Basket " + count;
    }

    void RefreshAddButton()
    {
        bool show = !isOpen && !OtherScreenOpen() && CanAddLookTarget(out _);
        if (addRoot.activeSelf != show)
        {
            addRoot.SetActive(show);
        }
    }

    bool CanAddLookTarget(out string vocabularyId)
    {
        vocabularyId = "";
        if (interactor == null || interactor.LookTarget == null || basket == null || basket.Catalog == null)
        {
            return false;
        }

        vocabularyId = interactor.LookTarget.VocabularyId;
        return basket.Catalog.Contains(vocabularyId);
    }

    void AddLookedAtProduct()
    {
        if (!CanAddLookTarget(out string vocabularyId) || basket == null)
        {
            return;
        }

        if (!basket.Add(vocabularyId))
        {
            return;
        }

        addedText.gameObject.SetActive(true);
        addedText.text = "Added to basket";
        addedUntil = Time.unscaledTime + AddedMessageSeconds;
    }

    public void Open()
    {
        if (OtherScreenOpen())
        {
            return;
        }

        screenRoot.SetActive(true);
        confirmRoot.SetActive(false);
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
        confirmRoot.SetActive(false);
        isOpen = false;
        IsOpenNow = false;

        if (pausedMovement != null && !OtherScreenOpen())
        {
            pausedMovement.enabled = true;
        }

        pausedMovement = null;
    }

    void RebuildList()
    {
        if (listContent == null || totalText == null)
        {
            return;
        }

        for (int i = listContent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(listContent.GetChild(i).gameObject);
        }

        IReadOnlyList<BasketLine> items = basket != null ? basket.Items : new List<BasketLine>();
        int shown = 0;
        for (int i = 0; i < items.Count; i++)
        {
            BasketLine line = items[i];
            if (line == null || line.quantity <= 0)
            {
                continue;
            }

            shown++;
            CreateRow(line);
        }

        emptyText.gameObject.SetActive(shown == 0);
        int total = basket != null ? basket.TotalYen : 0;
        totalText.text = "Total  ¥" + total;
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

        GameObject row = MenuUi.CreateRect("Item", listContent);
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

        MenuUi.AddText(row.transform, japanese, font, 32, FontStyle.Bold, Color.white, 42, TextAnchor.MiddleLeft);
        if (!string.IsNullOrEmpty(romaji))
        {
            MenuUi.AddText(row.transform, romaji, font, 22, FontStyle.Italic, MenuUi.Paper, 30, TextAnchor.MiddleLeft);
        }

        if (!string.IsNullOrWhiteSpace(english))
        {
            MenuUi.AddText(row.transform, english, font, 22, FontStyle.Normal, MenuUi.Muted, 30, TextAnchor.MiddleLeft);
        }

        string money = priced
            ? "¥" + price + " each    × " + line.quantity + "    ¥" + lineTotal
            : "× " + line.quantity;
        MenuUi.AddText(row.transform, money, font, 22, FontStyle.Bold, MenuUi.Paper, 32, TextAnchor.MiddleLeft);

        string vocabularyId = line.vocabularyId;
        MenuUi.CreateButton(row.transform, "Remove", font, MenuUi.Stone, () =>
        {
            if (basket != null)
            {
                basket.Remove(vocabularyId);
            }
        }, 48);
    }

    void AskClear()
    {
        if (basket == null || basket.Items.Count == 0)
        {
            return;
        }

        confirmRoot.SetActive(true);
    }

    void ConfirmClear()
    {
        confirmRoot.SetActive(false);
        if (basket != null)
        {
            basket.Clear();
        }
    }

    static bool OtherScreenOpen()
    {
        bool study = LearningPanel.Current != null && LearningPanel.Current.IsOpen;
        return study || DictionaryPanel.IsOpenNow || GrammarPanel.IsOpenNow || RevisionPanel.IsOpenNow || QuestPanel.IsOpenNow || CheckoutPanel.IsOpenNow || DialoguePanel.IsOpenNow;
    }

    void BuildAddButton()
    {
        Canvas canvas = MenuUi.CreateCanvas(transform, "Add To Basket Canvas", 16);
        addRoot = MenuUi.CreateRect("Add To Basket", canvas.transform);
        MenuUi.Stretch(addRoot.GetComponent<RectTransform>(), new Vector2(0.30f, 0.27f), new Vector2(0.70f, 0.38f));
        Image image = addRoot.AddComponent<Image>();
        image.color = MenuUi.Amber;
        Button button = addRoot.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(AddLookedAtProduct);
        Text label = MenuUi.AddText(addRoot.transform, "Add to Basket", font, 28, FontStyle.Bold, Color.white, 48, TextAnchor.MiddleCenter);
        MenuUi.Stretch(label.rectTransform, Vector2.zero, Vector2.one);
        label.GetComponent<LayoutElement>().ignoreLayout = true;

        GameObject message = MenuUi.CreateRect("Added", canvas.transform);
        MenuUi.Stretch(message.GetComponent<RectTransform>(), new Vector2(0.30f, 0.20f), new Vector2(0.70f, 0.26f));
        addedText = message.AddComponent<Text>();
        addedText.font = font;
        addedText.fontSize = 24;
        addedText.fontStyle = FontStyle.Bold;
        addedText.color = MenuUi.Paper;
        addedText.alignment = TextAnchor.MiddleCenter;
        addedText.raycastTarget = false;
        addedText.text = "Added to basket";
    }

    void BuildLauncher()
    {
        Canvas canvas = MenuUi.CreateCanvas(transform, "Basket Button Canvas", 22);
        GameplayHud.Register(canvas.gameObject);
        GameObject buttonObject = MenuUi.CreateRect("Basket", canvas.transform);
        MenuUi.Stretch(buttonObject.GetComponent<RectTransform>(), new Vector2(0.805f, 0.945f), new Vector2(0.893f, 0.985f));
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.11f, 0.105f, 0.1f, 0.72f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(Open);
        countText = MenuUi.AddText(buttonObject.transform, "Basket 0", font, 20, FontStyle.Bold, MenuUi.Paper, 36, TextAnchor.MiddleCenter);
        MenuUi.Stretch(countText.rectTransform, Vector2.zero, Vector2.one);
        countText.GetComponent<LayoutElement>().ignoreLayout = true;
    }

    void BuildScreen()
    {
        Canvas canvas = MenuUi.CreateCanvas(transform, "Basket Screen Canvas", 32);
        screenRoot = MenuUi.CreateRect("Basket Screen", canvas.transform);
        MenuUi.Stretch(screenRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        Image dim = screenRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject card = MenuUi.CreateRect("Card", screenRoot.transform);
        MenuUi.Stretch(card.GetComponent<RectTransform>(), new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.88f));
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.14f, 0.13f, 0.12f, 0.98f);
        VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(24, 24, 20, 20);
        cardLayout.spacing = 12;
        cardLayout.childAlignment = TextAnchor.UpperCenter;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        MenuUi.AddText(card.transform, "Basket", font, 36, FontStyle.Bold, Color.white, 48, TextAnchor.MiddleCenter);

        GameObject scrollObject = MenuUi.CreateRect("Scroll", card.transform);
        LayoutElement scrollLayout = scrollObject.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1;
        scrollLayout.preferredHeight = 560;
        scrollObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.2f);
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
        listLayout.spacing = 10;
        listLayout.padding = new RectOffset(8, 8, 8, 8);
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = listContent;

        emptyText = MenuUi.AddText(card.transform, "The basket is empty.", font, 24, FontStyle.Normal, MenuUi.Muted, 40, TextAnchor.MiddleCenter);
        totalText = MenuUi.AddText(card.transform, "Total  ¥0", font, 28, FontStyle.Bold, MenuUi.Paper, 40, TextAnchor.MiddleCenter);

        GameObject actions = MenuUi.CreateRect("Actions", card.transform);
        LayoutElement actionsLayout = actions.AddComponent<LayoutElement>();
        actionsLayout.preferredHeight = 64;
        HorizontalLayoutGroup actionRow = actions.AddComponent<HorizontalLayoutGroup>();
        actionRow.spacing = 12;
        actionRow.childControlWidth = true;
        actionRow.childControlHeight = true;
        actionRow.childForceExpandWidth = true;
        actionRow.childForceExpandHeight = true;
        MenuUi.CreateButton(actions.transform, "Clear basket", font, MenuUi.Stone, AskClear, 64);
        MenuUi.CreateButton(actions.transform, "Close", font, MenuUi.Amber, Close, 64);

        confirmRoot = MenuUi.CreateRect("Confirm", screenRoot.transform);
        MenuUi.Stretch(confirmRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        Image confirmBlock = confirmRoot.AddComponent<Image>();
        confirmBlock.color = new Color(0f, 0f, 0f, 0.35f);

        GameObject confirmCard = MenuUi.CreateRect("Confirm Card", confirmRoot.transform);
        MenuUi.Stretch(confirmCard.GetComponent<RectTransform>(), new Vector2(0.22f, 0.34f), new Vector2(0.78f, 0.66f));
        Image confirmImage = confirmCard.AddComponent<Image>();
        confirmImage.color = new Color(0.1f, 0.1f, 0.12f, 0.98f);
        VerticalLayoutGroup confirmLayout = confirmCard.AddComponent<VerticalLayoutGroup>();
        confirmLayout.padding = new RectOffset(24, 24, 20, 20);
        confirmLayout.spacing = 16;
        confirmLayout.childAlignment = TextAnchor.MiddleCenter;
        confirmLayout.childControlWidth = true;
        confirmLayout.childControlHeight = true;
        confirmLayout.childForceExpandWidth = true;
        confirmLayout.childForceExpandHeight = false;
        MenuUi.AddText(confirmCard.transform, "Clear everything in the basket?", font, 28, FontStyle.Bold, Color.white, 80, TextAnchor.MiddleCenter);
        MenuUi.CreateButton(confirmCard.transform, "Clear", font, MenuUi.Amber, ConfirmClear, 64);
        MenuUi.CreateButton(confirmCard.transform, "Cancel", font, MenuUi.Stone, () => confirmRoot.SetActive(false), 64);
    }
}
