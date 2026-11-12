using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The Grammar button, the list of lessons, and the explanation panel.
/// Grammar Progress adds this when the scene plays.
/// </summary>
public class GrammarPanel : MonoBehaviour
{
    public static GrammarPanel Instance { get; private set; }

    public static bool IsOpenNow { get; private set; }

    enum Page
    {
        List,
        Sentence,
        Lesson
    }

    Font font;
    GameObject openButton;
    GameObject screenRoot;
    RectTransform content;
    Text heading;
    bool openedFromStudy;
    Page page = Page.List;
    bool showingLearned = true;
    string currentVocabularyId = "";
    string currentLessonId = "";
    string awardNote = "";
    string sentenceAwardNote = "";
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
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Back();
        }
    }

    public void ShowList()
    {
        openedFromStudy = false;
        page = Page.List;
        OpenScreen();
        Rebuild();
    }

    public void ShowSentence(string vocabularyId)
    {
        currentVocabularyId = vocabularyId;
        openedFromStudy = true;
        page = Page.Sentence;
        OpenScreen();
        DiscoverSentenceLessons();
        Rebuild();
    }

    public void ShowLesson(string lessonId)
    {
        currentLessonId = lessonId;
        page = Page.Lesson;
        bool fresh = GrammarProgress.Instance != null && GrammarProgress.Instance.Discover(lessonId);
        int xp = PlayerProgression.Instance != null ? PlayerProgression.Instance.XpPerGrammar : 0;
        awardNote = fresh ? "New lesson. +" + xp + " XP." : "Already learned. No extra XP.";
        OpenScreen();
        Rebuild();
    }

    public void Close()
    {
        screenRoot.SetActive(false);
        IsOpenNow = false;

        LearningPanel learning = FindAnyObjectByType<LearningPanel>();
        bool learningOpen = learning != null && learning.IsOpen;
        bool dictionaryOpen = DictionaryPanel.IsOpenNow;
        if (!learningOpen && !dictionaryOpen && JapaneseAudioPlayer.Instance != null)
        {
            JapaneseAudioPlayer.Instance.Stop();
        }

        if (pausedMovement != null && !learningOpen && !dictionaryOpen)
        {
            pausedMovement.enabled = true;
        }

        pausedMovement = null;
        GameplayHud.Apply();
    }

    void Back()
    {
        if (page == Page.Lesson && openedFromStudy)
        {
            page = Page.Sentence;
            Rebuild();
            return;
        }

        if (page == Page.Lesson)
        {
            page = Page.List;
            Rebuild();
            return;
        }

        Close();
    }

    void OpenScreen()
    {
        screenRoot.SetActive(true);
        IsOpenNow = true;

        LearningPanel learning = FindAnyObjectByType<LearningPanel>();
        bool learningOpen = learning != null && learning.IsOpen;
        if (!learningOpen && !DictionaryPanel.IsOpenNow)
        {
            pausedMovement = GetComponent<FirstPersonController>();
            if (pausedMovement != null)
            {
                pausedMovement.enabled = false;
            }
        }

        GameplayHud.Apply();
    }

    void DiscoverSentenceLessons()
    {
        awardNote = "";
        sentenceAwardNote = "";
        if (GrammarLibrary.Instance == null || GrammarProgress.Instance == null)
        {
            return;
        }

        if (!GrammarLibrary.Instance.TryGetSentence(currentVocabularyId, out GrammarSentenceNote note) || note.lessonIds == null)
        {
            return;
        }

        List<string> fresh = new List<string>();
        for (int i = 0; i < note.lessonIds.Count; i++)
        {
            string lessonId = note.lessonIds[i];
            if (!GrammarProgress.Instance.Discover(lessonId))
            {
                continue;
            }

            if (GrammarLibrary.Instance.TryGetLesson(lessonId, out GrammarLesson lesson))
            {
                fresh.Add(lesson.grammarElement);
            }
        }

        if (fresh.Count == 0)
        {
            sentenceAwardNote = "Already learned. No extra XP.";
            return;
        }

        int xp = PlayerProgression.Instance != null ? PlayerProgression.Instance.XpPerGrammar * fresh.Count : 0;
        sentenceAwardNote = "New: " + string.Join(", ", fresh) + ". +" + xp + " XP.";
    }

    void Rebuild()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(content.GetChild(i).gameObject);
        }

        if (page == Page.List)
        {
            heading.text = "Grammar";
            BuildList();
            return;
        }

        if (page == Page.Sentence)
        {
            heading.text = "This sentence";
            BuildSentence();
            return;
        }

        heading.text = "Lesson";
        BuildLesson();
    }

    void BuildList()
    {
        int learned = GrammarProgress.Instance != null ? GrammarProgress.Instance.DiscoveredCount : 0;
        int total = GrammarLibrary.Instance != null && GrammarLibrary.Instance.Catalog != null
            ? GrammarLibrary.Instance.Catalog.Lessons.Count
            : 0;
        AddBody("Notes from the bedroom. " + learned + " of " + total + " opened.");

        GameObject filters = MenuUi.CreateRect("Filters", content);
        LayoutElement filterLayout = filters.AddComponent<LayoutElement>();
        filterLayout.preferredHeight = 64;
        HorizontalLayoutGroup row = filters.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 12;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = true;
        row.childForceExpandHeight = true;
        MenuUi.CreateButton(filters.transform, "Learned", font, showingLearned ? MenuUi.Amber : MenuUi.Stone, () =>
        {
            showingLearned = true;
            Rebuild();
        }, 64);
        MenuUi.CreateButton(filters.transform, "New", font, showingLearned ? MenuUi.Stone : MenuUi.Amber, () =>
        {
            showingLearned = false;
            Rebuild();
        }, 64);

        int shown = 0;
        if (GrammarLibrary.Instance != null && GrammarLibrary.Instance.Catalog != null)
        {
            IReadOnlyList<GrammarLesson> lessons = GrammarLibrary.Instance.Catalog.Lessons;
            for (int i = 0; i < lessons.Count; i++)
            {
                GrammarLesson lesson = lessons[i];
                if (lesson == null)
                {
                    continue;
                }

                bool discovered = GrammarProgress.Instance != null && GrammarProgress.Instance.IsDiscovered(lesson.id);
                if (discovered != showingLearned)
                {
                    continue;
                }

                shown++;
                string lessonId = lesson.id;
                string label = lesson.grammarElement + "    " + lesson.title;
                MenuUi.CreateButton(content, label, font, MenuUi.Stone, () => ShowLesson(lessonId), 72);
            }
        }

        if (shown == 0)
        {
            AddBody(showingLearned
                ? "No lessons yet. Study an object, then press Grammar. New lessons are under New."
                : "You have opened every lesson in this set.");
        }
    }

    void BuildSentence()
    {
        if (GrammarLibrary.Instance == null || !GrammarLibrary.Instance.TryGetSentence(currentVocabularyId, out GrammarSentenceNote note))
        {
            AddBody("This object has no grammar note yet.");
            return;
        }

        AddHeading(note.japaneseSentence);
        AddBody(JapaneseReading.ShownRomaji(note.japaneseSentence, "", note.romaji));
        AddBody(note.englishTranslation);
        AddHeading("Breakdown");
        AddBreakdown(note.breakdown);
        AddHeading("In this sentence");
        AddBody(note.explanation);
        AddBody(sentenceAwardNote);

        if (note.lessonIds != null)
        {
            for (int i = 0; i < note.lessonIds.Count; i++)
            {
                string lessonId = note.lessonIds[i];
                if (!GrammarLibrary.Instance.TryGetLesson(lessonId, out GrammarLesson lesson))
                {
                    continue;
                }

                MenuUi.CreateButton(content, "Read " + lesson.grammarElement, font, MenuUi.Amber, () => ShowLesson(lessonId), 64);
            }
        }
    }

    void BuildLesson()
    {
        if (GrammarLibrary.Instance == null || !GrammarLibrary.Instance.TryGetLesson(currentLessonId, out GrammarLesson lesson))
        {
            AddBody("This lesson is missing from the grammar catalog.");
            return;
        }

        bool learned = GrammarProgress.Instance != null && GrammarProgress.Instance.IsDiscovered(lesson.id);
        AddHeading(lesson.grammarElement + "   " + lesson.title);
        if (!string.IsNullOrWhiteSpace(lesson.reading))
        {
            string said = lesson.reading;
            string saidRomaji = JapaneseReading.ShownRomaji(lesson.grammarElement, lesson.reading, "");
            if (!string.IsNullOrEmpty(saidRomaji))
            {
                said += "  ·  " + saidRomaji;
            }

            AddBody("Said: " + said);
        }

        AddBody(learned ? "Learned" : "Not opened before");
        AddBody(awardNote);
        AddBody(DifficultyLabel(lesson.difficulty) + "  ·  " + LocationLabel(lesson.locationId));
        AddHeading("The short idea");
        AddBody(lesson.beginnerDescription);
        AddHeading("A little more");
        AddBody(lesson.englishExplanation);
        AddHeading("Example");
        AddBody(lesson.exampleSentence);
        AddBody(JapaneseReading.ShownRomaji(lesson.exampleSentence, "", lesson.romaji));
        AddBody(lesson.exampleTranslation);
        AddHeading("Breakdown");
        AddBreakdown(lesson.breakdown);
        AddHeading("Words that use it");
        AddBody(RelatedWords(lesson));
    }

    void AddBreakdown(List<GrammarBreakdownPart> parts)
    {
        if (parts == null || parts.Count == 0)
        {
            AddBody("No breakdown yet.");
            return;
        }

        for (int i = 0; i < parts.Count; i++)
        {
            GrammarBreakdownPart part = parts[i];
            if (part == null)
            {
                continue;
            }

            string reading = string.IsNullOrWhiteSpace(part.reading) ? "" : " (" + part.reading + ")";
            string romaji = JapaneseReading.ShownRomaji(part.japanese, part.reading, "");
            string romajiText = string.IsNullOrEmpty(romaji) ? "" : "  " + romaji;
            AddBody(part.japanese + reading + romajiText + "  =  " + part.meaning);
        }
    }

    static string RelatedWords(GrammarLesson lesson)
    {
        if (lesson.relatedVocabularyIds == null || lesson.relatedVocabularyIds.Count == 0)
        {
            return "None yet.";
        }

        List<string> labels = new List<string>();
        for (int i = 0; i < lesson.relatedVocabularyIds.Count; i++)
        {
            string id = lesson.relatedVocabularyIds[i];
            if (VocabularyLibrary.Instance != null && VocabularyLibrary.Instance.TryGet(id, out VocabularyEntry entry))
            {
                labels.Add(entry.japanese + " (" + entry.translation + ")");
            }
            else
            {
                labels.Add(id);
            }
        }

        return string.Join("\n", labels);
    }

    static string DifficultyLabel(int difficulty)
    {
        return difficulty <= 1 ? "Beginner" : "Level " + difficulty;
    }

    static string LocationLabel(string locationId)
    {
        return string.IsNullOrWhiteSpace(locationId) ? "Any room" : "Place: " + locationId;
    }

    void AddHeading(string value)
    {
        MenuUi.AddText(content, value, font, 30, FontStyle.Bold, MenuUi.Paper, 72, TextAnchor.MiddleLeft);
    }

    void AddBody(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        MenuUi.AddText(content, value, font, 24, FontStyle.Normal, Color.white, PreferredHeight(value), TextAnchor.UpperLeft);
    }

    static float PreferredHeight(string value)
    {
        int lines = 1;
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == '\n')
            {
                lines++;
            }
        }

        int wraps = Mathf.CeilToInt(value.Length / 42f);
        return Mathf.Max(44f, Mathf.Max(lines, wraps) * 34f);
    }

    void BuildUi()
    {
        Canvas canvas = MenuUi.CreateCanvas(transform, "Grammar Canvas", 40);
        openButton = MenuUi.CreateRect("Open Grammar", canvas.transform);
        MenuUi.Stretch(openButton.GetComponent<RectTransform>(), new Vector2(0.425f, 0.945f), new Vector2(0.513f, 0.985f));
        Image openImage = openButton.AddComponent<Image>();
        openImage.color = new Color(0.11f, 0.105f, 0.1f, 0.72f);
        Button open = openButton.AddComponent<Button>();
        open.targetGraphic = openImage;
        open.onClick.AddListener(ShowList);
        Text openLabel = MenuUi.AddText(openButton.transform, "Grammar", font, 20, FontStyle.Bold, MenuUi.Paper, 36, TextAnchor.MiddleCenter);
        MenuUi.Stretch(openLabel.rectTransform, Vector2.zero, Vector2.one);
        openLabel.GetComponent<LayoutElement>().ignoreLayout = true;

        screenRoot = MenuUi.CreateRect("Grammar Screen", canvas.transform);
        MenuUi.Stretch(screenRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        Image dim = screenRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject card = MenuUi.CreateCard(screenRoot.transform, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.9f));
        heading = MenuUi.AddText(card.transform, "Grammar", font, 36, FontStyle.Bold, Color.white, 52, TextAnchor.MiddleCenter);
        MenuUi.CreateScroll(card.transform, out content);
        MenuUi.CreateButton(card.transform, "Back", font, MenuUi.Amber, Back, 64);
    }
}
