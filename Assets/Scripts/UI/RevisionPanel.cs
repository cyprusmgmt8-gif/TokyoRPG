using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Flashcard revision. Revision Progress adds this when the scene plays.
/// </summary>
public class RevisionPanel : MonoBehaviour
{
    enum Page
    {
        Summary,
        Card,
        Done
    }

    enum PracticeMode
    {
        JapaneseToEnglish,
        EnglishToJapanese,
        Listening
    }

    class SessionCard
    {
        public string id;
        public bool wasDue;
        public bool returned;
    }

    public static RevisionPanel Instance { get; private set; }

    public static bool IsOpenNow { get; private set; }

    Font font;
    GameObject openButton;
    GameObject screenRoot;
    RectTransform content;
    Text heading;
    Page page = Page.Summary;
    PracticeMode mode = PracticeMode.JapaneseToEnglish;
    bool revealed;
    int lastHeardIndex = -2;
    int index;
    int dueAtStart;
    string resultNote = "";
    readonly List<SessionCard> cards = new List<SessionCard>();
    readonly HashSet<string> ratedIds = new HashSet<string>();
    readonly HashSet<string> ratedDueIds = new HashSet<string>();
    FirstPersonController pausedMovement;

    void Awake()
    {
        Instance = this;
        font = MenuUi.FindFont();
        MenuUi.EnsureEventSystem();
        BuildUi();
        screenRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            IsOpenNow = false;
        }
    }

    void LateUpdate()
    {
        LearningPanel learning = FindAnyObjectByType<LearningPanel>();
        bool learningOpen = learning != null && learning.IsOpen;
        if (openButton != null)
        {
            openButton.SetActive(!IsOpenNow && !learningOpen && !GameplayHud.Hidden);
        }

        if (!IsOpenNow)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Back();
            return;
        }

        if (page != Page.Card)
        {
            return;
        }

        if (!revealed && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
        {
            revealed = true;
            Rebuild();
            return;
        }

        if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            Step(-1);
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            Step(1);
        }
        else if (revealed && keyboard.digit1Key.wasPressedThisFrame)
        {
            Rate(RevisionRating.Again);
        }
        else if (revealed && keyboard.digit2Key.wasPressedThisFrame)
        {
            Rate(RevisionRating.Hard);
        }
        else if (revealed && keyboard.digit3Key.wasPressedThisFrame)
        {
            Rate(RevisionRating.Good);
        }
        else if (revealed && keyboard.digit4Key.wasPressedThisFrame)
        {
            Rate(RevisionRating.Easy);
        }
    }

    public void ShowSummary()
    {
        page = Page.Summary;
        resultNote = "";
        OpenScreen();
        Rebuild();
    }

    public void Close()
    {
        screenRoot.SetActive(false);
        IsOpenNow = false;
        if (JapaneseAudioPlayer.Instance != null)
        {
            JapaneseAudioPlayer.Instance.Stop();
        }

        LearningPanel learning = FindAnyObjectByType<LearningPanel>();
        bool learningOpen = learning != null && learning.IsOpen;
        if (pausedMovement != null && !learningOpen && !DictionaryPanel.IsOpenNow && !GrammarPanel.IsOpenNow)
        {
            pausedMovement.enabled = true;
        }

        pausedMovement = null;
        GameplayHud.Apply();
    }

    void Back()
    {
        if (page == Page.Card || page == Page.Done)
        {
            ShowSummary();
            return;
        }

        Close();
    }

    void OpenScreen()
    {
        screenRoot.SetActive(true);
        IsOpenNow = true;
        pausedMovement = GetComponent<FirstPersonController>();
        if (pausedMovement != null)
        {
            pausedMovement.enabled = false;
        }

        GameplayHud.Apply();
    }

    public void ReviewWord(string vocabularyId)
    {
        if (string.IsNullOrWhiteSpace(vocabularyId) || RevisionProgress.Instance == null)
        {
            return;
        }

        mode = PracticeMode.JapaneseToEnglish;
        cards.Clear();
        ratedIds.Clear();
        ratedDueIds.Clear();
        bool due = RevisionProgress.Instance.IsDue(vocabularyId);
        dueAtStart = due ? 1 : 0;
        cards.Add(new SessionCard
        {
            id = vocabularyId,
            wasDue = due
        });
        index = 0;
        revealed = false;
        lastHeardIndex = -2;
        resultNote = "";
        page = Page.Card;
        OpenScreen();
        if (JapaneseAudioPlayer.Instance != null)
        {
            JapaneseAudioPlayer.Instance.Stop();
        }

        Rebuild();
    }

    void StartSession(PracticeMode nextMode, int source)
    {
        if (RevisionProgress.Instance == null)
        {
            return;
        }

        mode = nextMode;
        cards.Clear();
        ratedIds.Clear();
        ratedDueIds.Clear();
        lastHeardIndex = -2;
        List<string> dueIds = RevisionProgress.Instance.GetSessionIds(true);
        dueAtStart = dueIds.Count;
        List<string> ids = source == 1
            ? RevisionProgress.Instance.GetDifficultIds()
            : source == 0 ? dueIds : RevisionProgress.Instance.GetSessionIds(false);
        HashSet<string> dueSet = new HashSet<string>(dueIds);
        for (int i = 0; i < ids.Count; i++)
        {
            cards.Add(new SessionCard
            {
                id = ids[i],
                wasDue = dueSet.Contains(ids[i])
            });
        }

        if (cards.Count == 0)
        {
            resultNote = "No words in this set yet. Study an object in the room first.";
            page = Page.Summary;
            Rebuild();
            return;
        }

        index = 0;
        revealed = false;
        resultNote = "";
        page = Page.Card;
        if (JapaneseAudioPlayer.Instance != null)
        {
            JapaneseAudioPlayer.Instance.Stop();
        }

        Rebuild();
    }

    void Step(int direction)
    {
        if (cards.Count == 0)
        {
            return;
        }

        int next = index + direction;
        if (next < 0 || next >= cards.Count)
        {
            return;
        }

        index = next;
        revealed = false;
        if (JapaneseAudioPlayer.Instance != null)
        {
            JapaneseAudioPlayer.Instance.Stop();
        }

        Rebuild();
    }

    void Rate(RevisionRating rating)
    {
        if (page != Page.Card || index < 0 || index >= cards.Count || RevisionProgress.Instance == null)
        {
            return;
        }

        SessionCard card = cards[index];
        int xp = 0;
        int bonus = 0;
        RevisionCardProgress saved = RevisionProgress.Instance.GetCard(card.id);
        bool firstMark = ratedIds.Add(card.id);
        bool retryAfterMiss = !firstMark && card.returned && saved != null && saved.lastRating == (int)RevisionRating.Again;
        if (firstMark || retryAfterMiss)
        {
            xp = RevisionProgress.Instance.Rate(card.id, rating, card.wasDue);
            if (card.wasDue)
            {
                ratedDueIds.Add(card.id);
            }

            bonus = RevisionProgress.Instance.TrySessionBonus(ratedDueIds.Count, dueAtStart);
            resultNote = ResultLine(card.id, rating, xp, bonus);
            if (rating == RevisionRating.Again && !card.returned)
            {
                card.returned = true;
                int insertAt = Mathf.Min(index + 2, cards.Count);
                cards.Insert(insertAt, card);
            }
        }
        else
        {
            resultNote = "Already marked in this session. No extra XP.";
        }

        if (index + 1 >= cards.Count)
        {
            page = Page.Done;
        }
        else
        {
            index++;
            revealed = false;
        }

        if (JapaneseAudioPlayer.Instance != null)
        {
            JapaneseAudioPlayer.Instance.Stop();
        }

        Rebuild();
    }

    static string ResultLine(string vocabularyId, RevisionRating rating, int xp, int bonus)
    {
        string wait = RevisionProgress.Instance != null
            ? RevisionProgress.Instance.DescribeWait(vocabularyId)
            : "";
        string xpText = xp > 0 ? " +" + xp + " XP." : " No extra XP.";
        if (!string.IsNullOrEmpty(wait))
        {
            xpText = " " + wait + "." + xpText;
        }

        if (bonus > 0)
        {
            xpText += " Set complete. +" + bonus + " XP.";
        }

        return rating + "." + xpText;
    }

    void Rebuild()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(content.GetChild(i).gameObject);
        }

        if (page == Page.Summary)
        {
            heading.text = "Vocabulary Revision";
            BuildSummary();
            return;
        }

        if (page == Page.Done)
        {
            heading.text = "Set complete";
            BuildDone();
            return;
        }

        heading.text = ModeTitle(mode);
        BuildCard();
    }

    void BuildSummary()
    {
        RevisionProgress progress = RevisionProgress.Instance;
        int due = progress != null ? progress.DueCount : 0;
        int reviewed = progress != null ? progress.ReviewedTodayCount : 0;
        int correct = progress != null ? progress.CorrectToday : 0;
        int incorrect = progress != null ? progress.IncorrectToday : 0;
        int mastered = progress != null ? progress.MasteredCount : 0;
        int difficult = progress != null ? progress.NeedsAttentionCount : 0;
        int discovered = progress != null ? progress.DiscoveredCount : 0;

        AddBody("A short visit is enough. These are words you have already met.");
        AddBody("Due today:  " + due);
        AddBody("Reviewed today:  " + reviewed);
        AddBody("Remembered today:  " + correct);
        AddBody("Forgot today:  " + incorrect);
        AddBody("Mastered:  " + mastered + " of " + discovered);
        if (!string.IsNullOrEmpty(resultNote))
        {
            AddBody(resultNote);
        }

        if (discovered == 0)
        {
            AddBody("Study an object in the room. It will be waiting here.");
            return;
        }

        AddHeading(due > 0 ? "Due today" : "Nothing is due");
        if (due > 0)
        {
            AddModeButtons(0, MenuUi.Amber);
        }
        else
        {
            AddBody("You are clear for now. Extra practice does not add XP.");
        }

        AddHeading(difficult > 0 ? "Tricky words" : "No tricky words");
        if (difficult > 0)
        {
            AddModeButtons(1, MenuUi.Stone);
        }
        else
        {
            AddBody("Words you forget, or that are still Learning, will gather here.");
        }

        AddHeading("Practice everything");
        AddModeButtons(2, MenuUi.Stone);
        AddBody("Again returns in about a minute, then tomorrow if you remember it. Hard is tomorrow, Good is 3 days, Easy is 7 days. Gaps grow when you keep remembering, up to 30 days. Revision XP stops at " + RevisionProgress.DailyXpCap + " a day, and Again never pays.");
    }

    void AddModeButtons(int source, Color color)
    {
        MenuUi.CreateButton(content, "Japanese → English", font, color, () => StartSession(PracticeMode.JapaneseToEnglish, source), 64);
        MenuUi.CreateButton(content, "English → Japanese", font, color, () => StartSession(PracticeMode.EnglishToJapanese, source), 64);
        MenuUi.CreateButton(content, "Listening", font, color, () => StartSession(PracticeMode.Listening, source), 64);
    }

    static string ModeTitle(PracticeMode practiceMode)
    {
        if (practiceMode == PracticeMode.EnglishToJapanese)
        {
            return "English to Japanese";
        }

        if (practiceMode == PracticeMode.Listening)
        {
            return "Listening";
        }

        return "Japanese to English";
    }

    void BuildCard()
    {
        SessionCard card = cards[index];
        VocabularyEntry entry = null;
        bool found = VocabularyLibrary.Instance != null && VocabularyLibrary.Instance.TryGet(card.id, out entry);
        AddNote((index + 1) + " of " + cards.Count);
        if (!string.IsNullOrEmpty(resultNote))
        {
            AddNote(resultNote);
        }

        if (!found)
        {
            AddBody(card.id);
            return;
        }

        if (mode == PracticeMode.Listening && !revealed)
        {
            AddBody("Listen, then see if you know the word.");
            AddCenteredClip(content, entry.wordClip, "Play word");
        }
        else if (mode == PracticeMode.EnglishToJapanese && !revealed)
        {
            AddPrompt(entry.translation);
        }
        else
        {
            AddWordGroup(entry, true);
        }

        if (revealed)
        {
            AddExampleGroup(entry);
            AddProgressLine(entry);
            AddHeading("How did that feel?");
            GameObject ratings = MenuUi.CreateRect("Ratings", content);
            LayoutElement ratingsLayout = ratings.AddComponent<LayoutElement>();
            ratingsLayout.preferredHeight = 84;
            HorizontalLayoutGroup row = ratings.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 8;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;
            MenuUi.CreateButton(ratings.transform, "Again", font, new Color(0.45f, 0.24f, 0.22f, 1f), () => Rate(RevisionRating.Again), 84);
            MenuUi.CreateButton(ratings.transform, "Hard", font, MenuUi.Stone, () => Rate(RevisionRating.Hard), 84);
            MenuUi.CreateButton(ratings.transform, "Good", font, MenuUi.Amber, () => Rate(RevisionRating.Good), 84);
            MenuUi.CreateButton(ratings.transform, "Easy", font, new Color(0.24f, 0.38f, 0.3f, 1f), () => Rate(RevisionRating.Easy), 84);
        }
        else
        {
            MenuUi.CreateButton(content, "Reveal Meaning", font, MenuUi.Amber, () =>
            {
                revealed = true;
                Rebuild();
            }, 72);
        }

        MaybePlayListening(entry);
        GameObject move = MenuUi.CreateRect("Move", content);
        LayoutElement moveLayout = move.AddComponent<LayoutElement>();
        moveLayout.preferredHeight = 64;
        HorizontalLayoutGroup moveRow = move.AddComponent<HorizontalLayoutGroup>();
        moveRow.spacing = 12;
        moveRow.childControlWidth = true;
        moveRow.childControlHeight = true;
        moveRow.childForceExpandWidth = true;
        moveRow.childForceExpandHeight = true;
        MenuUi.CreateButton(move.transform, "Previous", font, MenuUi.Stone, () => Step(-1), 64);
        MenuUi.CreateButton(move.transform, "Next", font, MenuUi.Stone, () => Step(1), 64);
    }

    void AddWordGroup(VocabularyEntry entry, bool playWord)
    {
        GameObject group = BeginGroup("Word", false);
        AddStyled(group.transform, entry.japanese, 56, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 16, 68f);
        AddStyled(group.transform, entry.reading, 28, FontStyle.Normal, new Color(1f, 0.86f, 0.55f), TextAnchor.MiddleCenter, 28, 36f);
        AddStyled(group.transform, JapaneseReading.ShownRomaji(entry.japanese, entry.reading, entry.romaji), 24, FontStyle.Italic, new Color(1f, 0.86f, 0.55f), TextAnchor.MiddleCenter, 32, 34f);
        if (revealed)
        {
            AddStyled(group.transform, entry.translation, 28, FontStyle.Normal, new Color(0.93f, 0.93f, 0.9f), TextAnchor.MiddleCenter, 34, 40f);
        }

        if (playWord)
        {
            AddCenteredClip(group.transform, entry.wordClip, "Play word");
        }
    }

    void AddExampleGroup(VocabularyEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.exampleSentence))
        {
            return;
        }

        GameObject group = BeginGroup("Example", true);
        AddStyled(group.transform, "Example Sentence", 20, FontStyle.Bold, new Color(0.78f, 0.72f, 0.62f), TextAnchor.MiddleCenter, 28, 28f);
        AddStyled(group.transform, entry.exampleSentence, 32, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter, 24, 44f);
        AddStyled(group.transform, entry.exampleReading, 24, FontStyle.Normal, new Color(1f, 0.86f, 0.55f), TextAnchor.MiddleCenter, 30, 34f);
        AddStyled(group.transform, JapaneseReading.ShownRomaji(entry.exampleSentence, entry.exampleReading, entry.exampleRomaji), 22, FontStyle.Italic, new Color(1f, 0.86f, 0.55f), TextAnchor.MiddleCenter, 34, 32f);
        AddStyled(group.transform, entry.exampleTranslation, 24, FontStyle.Italic, new Color(0.86f, 0.86f, 0.82f), TextAnchor.MiddleCenter, 34, 36f);
        AddCenteredClip(group.transform, entry.exampleClip, "Play sentence");
    }

    void MaybePlayListening(VocabularyEntry entry)
    {
        if (mode != PracticeMode.Listening || revealed || entry == null || entry.wordClip == null || lastHeardIndex == index)
        {
            return;
        }

        lastHeardIndex = index;
        JapaneseAudioPlayer.Ensure().Play(entry.wordClip);
    }

    void AddProgressLine(VocabularyEntry entry)
    {
        RevisionCardProgress progress = RevisionProgress.Instance != null ? RevisionProgress.Instance.GetCard(entry.id) : null;
        if (progress == null)
        {
            return;
        }

        AddNote(RevisionProgress.StatusLabel(progress.status) + "  ·  reviewed " + progress.attempts + " times");
    }

    GameObject BeginGroup(string name, bool framed)
    {
        GameObject group = MenuUi.CreateRect(name, content);
        if (framed)
        {
            Image background = group.AddComponent<Image>();
            background.color = new Color(0.16f, 0.15f, 0.14f, 1f);
        }

        VerticalLayoutGroup layout = group.AddComponent<VerticalLayoutGroup>();
        int pad = framed ? 18 : 4;
        layout.padding = new RectOffset(pad, pad, framed ? 16 : 4, framed ? 16 : 8);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return group;
    }

    void AddStyled(Transform parent, string value, int size, FontStyle style, Color color, TextAnchor align, int charsPerLine, float minHeight)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        int lines = WrapLines(value, charsPerLine);
        float height = Mathf.Max(minHeight, lines * (size + 12f));
        Text text = MenuUi.AddText(parent, value, font, size, style, color, height, align);
        text.verticalOverflow = VerticalWrapMode.Overflow;
    }

    void AddNote(string value)
    {
        AddStyled(content, value, 20, FontStyle.Normal, MenuUi.Muted, TextAnchor.MiddleCenter, 42, 32f);
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

    void AddCenteredClip(Transform parent, AudioClip clip, string label)
    {
        GameObject row = MenuUi.CreateRect("Play Row", parent);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 56f;
        rowLayout.minHeight = 56f;
        HorizontalLayoutGroup rowGroup = row.AddComponent<HorizontalLayoutGroup>();
        rowGroup.childAlignment = TextAnchor.MiddleCenter;
        rowGroup.childControlWidth = true;
        rowGroup.childControlHeight = true;
        rowGroup.childForceExpandWidth = false;
        rowGroup.childForceExpandHeight = true;
        Button button = AddClipButton(row.transform, clip, label);
        LayoutElement buttonLayout = button.GetComponent<LayoutElement>();
        buttonLayout.preferredWidth = 220f;
        buttonLayout.flexibleWidth = 0f;
    }

    Button AddClipButton(Transform parent, AudioClip clip, string label)
    {
        if (clip == null)
        {
            Button missing = MenuUi.CreateButton(parent, "No recording", font, MenuUi.Stone, () => { }, 56);
            missing.interactable = false;
            return missing;
        }

        AudioClip captured = clip;
        string capturedLabel = label;
        return MenuUi.CreateButton(parent, ButtonLabel(captured, capturedLabel), font, new Color(0.22f, 0.32f, 0.4f, 1f), () =>
        {
            JapaneseAudioPlayer player = JapaneseAudioPlayer.Ensure();
            if (player.IsPlaying(captured))
            {
                player.Stop();
            }
            else
            {
                player.Play(captured);
            }

            Rebuild();
        }, 56);
    }

    static string ButtonLabel(AudioClip clip, string label)
    {
        return JapaneseAudioPlayer.Instance != null && JapaneseAudioPlayer.Instance.IsPlaying(clip)
            ? "Stop"
            : label;
    }

    void BuildDone()
    {
        AddBody("That set is finished.");
        if (!string.IsNullOrEmpty(resultNote))
        {
            AddBody(resultNote);
        }

        AddBody("Due now:  " + (RevisionProgress.Instance != null ? RevisionProgress.Instance.DueCount : 0));
        AddBody("Reviewed today:  " + (RevisionProgress.Instance != null ? RevisionProgress.Instance.ReviewedTodayCount : 0));
        MenuUi.CreateButton(content, "Back to summary", font, MenuUi.Amber, ShowSummary, 72);
    }

    void AddHeading(string value)
    {
        MenuUi.AddText(content, value, font, 28, FontStyle.Bold, MenuUi.Paper, 48, TextAnchor.MiddleLeft);
    }

    void AddPrompt(string value)
    {
        MenuUi.AddText(content, value, font, 54, FontStyle.Bold, Color.white, 88, TextAnchor.MiddleCenter);
    }

    void AddBody(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        int lines = 1;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == '\n')
            {
                lines++;
            }
        }

        float height = Mathf.Max(40f, lines * 34f);
        MenuUi.AddText(content, value, font, 24, FontStyle.Normal, Color.white, height, TextAnchor.MiddleLeft);
    }

    void BuildUi()
    {
        Canvas canvas = MenuUi.CreateCanvas(transform, "Revision Canvas", 45);
        openButton = MenuUi.CreateRect("Open Revision", canvas.transform);
        MenuUi.Stretch(openButton.GetComponent<RectTransform>(), new Vector2(0.520f, 0.945f), new Vector2(0.608f, 0.985f));
        Image openImage = openButton.AddComponent<Image>();
        openImage.color = new Color(0.11f, 0.105f, 0.1f, 0.72f);
        Button open = openButton.AddComponent<Button>();
        open.targetGraphic = openImage;
        open.onClick.AddListener(ShowSummary);
        Text openLabel = MenuUi.AddText(openButton.transform, "Revision", font, 20, FontStyle.Bold, MenuUi.Paper, 36, TextAnchor.MiddleCenter);
        MenuUi.Stretch(openLabel.rectTransform, Vector2.zero, Vector2.one);
        openLabel.GetComponent<LayoutElement>().ignoreLayout = true;

        screenRoot = MenuUi.CreateRect("Revision Screen", canvas.transform);
        MenuUi.Stretch(screenRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        Image dim = screenRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject card = MenuUi.CreateCard(screenRoot.transform, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.9f));
        heading = MenuUi.AddText(card.transform, "Revise", font, 36, FontStyle.Bold, Color.white, 52, TextAnchor.MiddleCenter);
        MenuUi.CreateScroll(card.transform, out content);
        MenuUi.CreateButton(card.transform, "Back", font, MenuUi.Amber, Back, 64);
    }
}
