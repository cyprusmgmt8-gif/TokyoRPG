using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// A small Quests button and the quest screen.
/// Quest Log adds this when the scene plays. The list is not shown until the button is used.
/// </summary>
public class QuestPanel : MonoBehaviour
{
    public static bool IsOpenNow { get; private set; }

    Font font;
    GameObject screenRoot;
    Text body;
    FirstPersonController pausedMovement;
    bool isOpen;

    void Awake()
    {
        font = MenuUi.FindFont();
        MenuUi.EnsureEventSystem();
        BuildButton();
        BuildScreen();
        screenRoot.SetActive(false);
    }

    void OnEnable()
    {
        if (QuestLog.Instance != null)
        {
            QuestLog.Instance.Changed -= Refresh;
            QuestLog.Instance.Changed += Refresh;
        }

        Refresh();
    }

    void Start()
    {
        if (QuestLog.Instance != null)
        {
            QuestLog.Instance.Changed -= Refresh;
            QuestLog.Instance.Changed += Refresh;
        }

        Refresh();
    }

    void OnDisable()
    {
        if (QuestLog.Instance != null)
        {
            QuestLog.Instance.Changed -= Refresh;
        }

        if (isOpen)
        {
            isOpen = false;
            IsOpenNow = false;
        }
    }

    void Update()
    {
        if (!isOpen)
        {
            return;
        }

        if (LearningScreenOpen())
        {
            Close();
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
        if (LearningScreenOpen() || isOpen)
        {
            return;
        }

        BasketPanel basket = FindAnyObjectByType<BasketPanel>();
        if (basket != null && BasketPanel.IsOpenNow)
        {
            basket.Close();
        }

        screenRoot.SetActive(true);
        isOpen = true;
        IsOpenNow = true;
        Refresh();

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

        if (pausedMovement != null && !LearningScreenOpen() && !BasketPanel.IsOpenNow)
        {
            pausedMovement.enabled = true;
        }

        pausedMovement = null;
    }

    void Refresh()
    {
        if (body == null)
        {
            return;
        }

        QuestLog log = QuestLog.Instance;
        if (log == null || log.Catalog == null)
        {
            body.text = "No quests yet.";
            return;
        }

        List<QuestProgressRecord> quests = log.VisibleQuests();
        StringBuilder active = new StringBuilder();
        StringBuilder completed = new StringBuilder();
        for (int i = 0; i < quests.Count; i++)
        {
            QuestProgressRecord record = quests[i];
            if (record == null || !log.Catalog.TryGet(record.questId, out QuestDefinition quest))
            {
                continue;
            }

            StringBuilder section = record.state == (int)QuestState.Completed ? completed : active;
            if (section.Length > 0)
            {
                section.Append("\n\n");
            }

            AppendQuest(section, quest, record);
        }

        if (active.Length == 0 && completed.Length == 0)
        {
            body.text = "No quests yet.";
            return;
        }

        StringBuilder text = new StringBuilder();
        text.Append("Active");
        text.Append('\n');
        text.Append(active.Length == 0 ? "No active quests." : active.ToString());
        if (completed.Length > 0)
        {
            text.Append("\n\nCompleted\n");
            text.Append(completed);
        }

        body.text = text.ToString();
        FitBody();
    }

    void FitBody()
    {
        if (body == null)
        {
            return;
        }

        LayoutElement element = body.GetComponent<LayoutElement>();
        if (element == null)
        {
            return;
        }

        int lines = 1;
        string value = body.text;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == '\n')
            {
                lines++;
            }
        }

        element.preferredHeight = Mathf.Max(80f, lines * 34f);
        element.minHeight = element.preferredHeight;
    }

    static void AppendQuest(StringBuilder text, QuestDefinition quest, QuestProgressRecord record)
    {
        text.Append(string.IsNullOrWhiteSpace(quest.japanese) ? quest.english : quest.japanese);
        string romaji = JapaneseReading.ShownRomaji(quest.japanese, quest.reading, quest.romaji);
        if (!string.IsNullOrEmpty(romaji))
        {
            text.Append('\n');
            text.Append(romaji);
        }

        if (!string.IsNullOrWhiteSpace(quest.english))
        {
            text.Append('\n');
            text.Append(quest.english);
        }

        if (!string.IsNullOrWhiteSpace(quest.description))
        {
            text.Append('\n');
            text.Append(quest.description);
        }

        text.Append('\n');
        text.Append(RewardLine(quest.rewardXp));

        if (quest.objectives != null)
        {
            for (int i = 0; i < quest.objectives.Count; i++)
            {
                QuestObjectiveDefinition objective = quest.objectives[i];
                if (objective == null)
                {
                    continue;
                }

                bool done = false;
                if (record.objectives != null)
                {
                    for (int j = 0; j < record.objectives.Count; j++)
                    {
                        QuestObjectiveProgress progress = record.objectives[j];
                        if (progress != null && string.Equals(progress.objectiveId, objective.id, System.StringComparison.OrdinalIgnoreCase))
                        {
                            done = progress.complete;
                            break;
                        }
                    }
                }

                string name = string.IsNullOrWhiteSpace(objective.label) ? objective.id : objective.label;
                text.Append('\n');
                text.Append(name);
                text.Append(done ? ": 1/1" : ": 0/1");
            }
        }

        if (record.state == (int)QuestState.Completed)
        {
            text.Append("\nComplete");
        }
    }

    static string RewardLine(int rewardXp)
    {
        if (rewardXp <= 0)
        {
            return "XP Reward: none";
        }

        return "XP Reward: +" + rewardXp + " XP";
    }

    static bool LearningScreenOpen()
    {
        bool study = LearningPanel.Current != null && LearningPanel.Current.IsOpen;
        return study || DictionaryPanel.IsOpenNow || GrammarPanel.IsOpenNow || RevisionPanel.IsOpenNow || CheckoutPanel.IsOpenNow || DialoguePanel.IsOpenNow;
    }

    void BuildButton()
    {
        Canvas canvas = MenuUi.CreateCanvas(transform, "Quest Button Canvas", 34);
        GameplayHud.Register(canvas.gameObject);
        GameObject buttonObject = MenuUi.CreateRect("Quests", canvas.transform);
        MenuUi.Stretch(buttonObject.GetComponent<RectTransform>(), new Vector2(0.615f, 0.945f), new Vector2(0.703f, 0.985f));
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.11f, 0.105f, 0.1f, 0.72f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(Toggle);
        Text label = MenuUi.AddText(buttonObject.transform, "Quests", font, 20, FontStyle.Bold, MenuUi.Paper, 36, TextAnchor.MiddleCenter);
        MenuUi.Stretch(label.rectTransform, Vector2.zero, Vector2.one);
        label.GetComponent<LayoutElement>().ignoreLayout = true;
    }

    void BuildScreen()
    {
        Canvas canvas = MenuUi.CreateCanvas(transform, "Quest Screen Canvas", 33);
        screenRoot = MenuUi.CreateRect("Quest Screen", canvas.transform);
        MenuUi.Stretch(screenRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        Image dim = screenRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.45f);

        GameObject card = MenuUi.CreateRect("Card", screenRoot.transform);
        MenuUi.Stretch(card.GetComponent<RectTransform>(), new Vector2(0.18f, 0.14f), new Vector2(0.82f, 0.86f));
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.12f, 0.115f, 0.11f, 0.96f);
        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 22, 22);
        layout.spacing = 12;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        MenuUi.AddText(card.transform, "Quests", font, 32, FontStyle.Bold, Color.white, 44, TextAnchor.MiddleCenter);

        GameObject scrollObject = MenuUi.CreateRect("Scroll", card.transform);
        LayoutElement scrollLayout = scrollObject.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1;
        scrollLayout.preferredHeight = 520;
        scrollObject.AddComponent<RectMask2D>();
        ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.viewport = scrollObject.GetComponent<RectTransform>();

        GameObject content = MenuUi.CreateRect("Content", scrollObject.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(8, 8, 4, 8);
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRect;

        body = MenuUi.AddText(content.transform, "No quests yet.", font, 24, FontStyle.Normal, MenuUi.Paper, 80, TextAnchor.UpperLeft);
        MenuUi.CreateButton(card.transform, "Close", font, MenuUi.Stone, Close, 56);
    }
}
