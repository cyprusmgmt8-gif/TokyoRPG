using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The title screen. Put this on an empty object in the Main Menu scene.
/// It builds the buttons when play starts.
/// </summary>
public class MainMenuScreen : MonoBehaviour
{
    public static bool IsOpenNow { get; private set; }

    static bool openAsOverlay;
    static MainMenuScreen gameplayMenu;

    Font font;
    GameObject homeRoot;
    GameObject createRoot;
    GameObject confirmRoot;
    GameObject profilesRoot;
    Button savedProfilesButton;
    RectTransform profileList;
    SettingsPanel settings;
    Text welcomeText;
    Image avatarImage;
    Text avatarLetter;
    Button continueButton;
    InputField nicknameField;
    Text errorText;
    Image[] avatarImages;
    Image[] languageImages;
    string selectedAvatarId = "traveler";
    string selectedLanguageCode = "en";
    GameObject screenRoot;
    FirstPersonController pausedMovement;
    bool openedOverGameplay;
    bool pausedByMenu;

    /// <summary>
    /// Draws the existing title menu over the current room.
    /// Back hides it. The room stays loaded, so the player does not move.
    /// </summary>
    public static void OpenOverGameplay(Transform host)
    {
        if (host == null || CityMapScreen.IsOpenNow)
        {
            return;
        }

        if (gameplayMenu != null)
        {
            gameplayMenu.Show();
            return;
        }

        openAsOverlay = true;
        GameObject menuObject = new GameObject("Gameplay Menu");
        menuObject.transform.SetParent(host, false);
        gameplayMenu = menuObject.AddComponent<MainMenuScreen>();
    }

    void Awake()
    {
        openedOverGameplay = openAsOverlay;
        openAsOverlay = false;
        if (openedOverGameplay)
        {
            gameplayMenu = this;
        }

        PlayerProfile.EnsureLoaded();
        AccessEntitlement.EnsureLoaded();
        MenuUi.ShowCursor();
        if (!openedOverGameplay)
        {
            MenuUi.PaintNightSky();
        }

        font = MenuUi.FindFont();
        MenuUi.EnsureEventSystem();

        int sortOrder = openedOverGameplay ? 65 : 10;
        Canvas canvas = MenuUi.CreateCanvas(transform, "Menu Canvas", sortOrder);
        screenRoot = canvas.gameObject;
        homeRoot = BuildHome(canvas.transform);
        createRoot = BuildCreate(canvas.transform);
        confirmRoot = BuildConfirm(canvas.transform);
        profilesRoot = BuildProfiles(canvas.transform);
        if (openedOverGameplay)
        {
            BuildBackButton(canvas.transform);
        }

        settings = new SettingsPanel(canvas.transform, font, ShowHome);
        ShowHome();
        if (openedOverGameplay)
        {
            Show();
        }
    }

    void OnDisable()
    {
        if (openedOverGameplay && IsOpenNow)
        {
            IsOpenNow = false;
            ResumeMovement();
        }
    }

    void OnDestroy()
    {
        if (gameplayMenu == this)
        {
            gameplayMenu = null;
        }
    }

    void Show()
    {
        if (screenRoot != null)
        {
            screenRoot.SetActive(true);
        }

        IsOpenNow = true;
        MenuUi.ShowCursor();
        PauseMovement();
    }

    void CloseMenu()
    {
        if (!openedOverGameplay)
        {
            return;
        }

        if (screenRoot != null)
        {
            screenRoot.SetActive(false);
        }

        IsOpenNow = false;
        ResumeMovement();
    }

    void PauseMovement()
    {
        if (pausedByMenu)
        {
            return;
        }

        pausedMovement = GetComponentInParent<FirstPersonController>();
        if (pausedMovement != null && pausedMovement.enabled)
        {
            pausedMovement.enabled = false;
            pausedByMenu = true;
        }
    }

    void ResumeMovement()
    {
        if (!pausedByMenu || pausedMovement == null)
        {
            return;
        }

        pausedByMenu = false;
        if (!CityMapScreen.IsOpenNow && !AnotherScreenOpen())
        {
            pausedMovement.enabled = true;
        }
    }

    static bool AnotherScreenOpen()
    {
        bool study = LearningPanel.Current != null && LearningPanel.Current.IsOpen;
        return study
            || DictionaryPanel.IsOpenNow
            || GrammarPanel.IsOpenNow
            || RevisionPanel.IsOpenNow
            || QuestPanel.IsOpenNow
            || BasketPanel.IsOpenNow
            || CheckoutPanel.IsOpenNow
            || DialoguePanel.IsOpenNow;
    }

    void BuildBackButton(Transform parent)
    {
        GameObject back = MenuUi.CreateRect("Back To Room", parent);
        MenuUi.Stretch(back.GetComponent<RectTransform>(), new Vector2(0.055f, 0.905f), new Vector2(0.22f, 0.965f));
        Image image = back.AddComponent<Image>();
        image.color = MenuUi.Amber;
        Button button = back.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(CloseMenu);
        Text label = MenuUi.AddText(back.transform, "Back", font, 28, FontStyle.Bold, Color.white, 56, TextAnchor.MiddleCenter);
        MenuUi.Stretch(label.rectTransform, Vector2.zero, Vector2.one);
        label.GetComponent<LayoutElement>().ignoreLayout = true;
    }

    void ShowHome()
    {
        homeRoot.SetActive(true);
        createRoot.SetActive(false);
        confirmRoot.SetActive(false);
        profilesRoot.SetActive(false);
        settings.Root.SetActive(false);
        RefreshHome();
    }

    void RefreshHome()
    {
        bool saved = PlayerProfile.HasStarted;
        continueButton.interactable = saved;
        savedProfilesButton.gameObject.SetActive(SaveSlots.Count > 1);
        if (saved)
        {
            AvatarChoice avatar = AvatarChoices.Find(PlayerProfile.Data.avatarId);
            welcomeText.text = "Continue as " + PlayerProfile.Data.nickname;
            avatarImage.color = avatar.color;
            avatarLetter.text = avatar.label.Substring(0, 1);
            avatarImage.gameObject.SetActive(true);
        }
        else
        {
            welcomeText.text = "No saved profile yet.";
            avatarImage.gameObject.SetActive(false);
        }
    }

    GameObject BuildHome(Transform parent)
    {
        GameObject root = MenuUi.CreateRect("Home", parent);
        MenuUi.Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        GameObject card = MenuUi.CreateCard(root.transform, new Vector2(0.18f, 0.12f), new Vector2(0.82f, 0.88f));

        MenuUi.AddText(card.transform, "Tokyo RPG", font, 64, FontStyle.Bold, MenuUi.Paper, 80, TextAnchor.MiddleCenter);
        MenuUi.AddText(card.transform, "Learn everyday Japanese", font, 28, FontStyle.Normal, MenuUi.Muted, 40, TextAnchor.MiddleCenter);

        GameObject avatarRow = MenuUi.CreateRect("Avatar", card.transform);
        LayoutElement avatarLayout = avatarRow.AddComponent<LayoutElement>();
        avatarLayout.preferredHeight = 72;
        HorizontalLayoutGroup row = avatarRow.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 16;
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        GameObject badge = MenuUi.CreateRect("Badge", avatarRow.transform);
        LayoutElement badgeLayout = badge.AddComponent<LayoutElement>();
        badgeLayout.preferredWidth = 72;
        badgeLayout.preferredHeight = 72;
        avatarImage = badge.AddComponent<Image>();
        avatarImage.raycastTarget = false;
        avatarLetter = MenuUi.AddText(badge.transform, "T", font, 32, FontStyle.Bold, Color.white, 72, TextAnchor.MiddleCenter);
        MenuUi.Stretch(avatarLetter.rectTransform, Vector2.zero, Vector2.one);
        avatarLetter.GetComponent<LayoutElement>().ignoreLayout = true;

        welcomeText = MenuUi.AddText(card.transform, "No saved profile yet.", font, 26, FontStyle.Normal, Color.white, 40, TextAnchor.MiddleCenter);
        MenuUi.CreateButton(card.transform, "New Game", font, MenuUi.Amber, OnNewGame, 76);
        continueButton = MenuUi.CreateButton(card.transform, "Continue", font, MenuUi.Stone, ContinueGame, 76);
        savedProfilesButton = MenuUi.CreateButton(card.transform, "Saved profiles", font, MenuUi.Stone, ShowProfiles, 64);
        MenuUi.CreateButton(card.transform, "Settings", font, MenuUi.Stone, ShowSettings, 76);
        MenuUi.AddText(
            card.transform,
            "Your dictionary and XP stay saved on this device.",
            font,
            22,
            FontStyle.Normal,
            MenuUi.Muted,
            50,
            TextAnchor.MiddleCenter);
        return root;
    }

    void OnNewGame()
    {
        if (PlayerProfile.HasStarted)
        {
            homeRoot.SetActive(false);
            confirmRoot.SetActive(true);
            return;
        }

        OpenCreate();
    }

    void OpenCreate()
    {
        homeRoot.SetActive(false);
        confirmRoot.SetActive(false);
        profilesRoot.SetActive(false);
        createRoot.SetActive(true);
        settings.Root.SetActive(false);
        selectedAvatarId = PlayerProfile.Data.avatarId;
        selectedLanguageCode = PlayerProfile.CurrentLanguageCode("en");
        nicknameField.text = PlayerProfile.HasStarted ? PlayerProfile.Data.nickname : "";
        errorText.text = "";
        PaintCreateChoices();
    }

    void ShowSettings()
    {
        homeRoot.SetActive(false);
        createRoot.SetActive(false);
        confirmRoot.SetActive(false);
        profilesRoot.SetActive(false);
        settings.Refresh();
        settings.Root.SetActive(true);
    }

    GameObject BuildConfirm(Transform parent)
    {
        GameObject root = MenuUi.CreateRect("Confirm", parent);
        MenuUi.Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        GameObject card = MenuUi.CreateCard(root.transform, new Vector2(0.16f, 0.18f), new Vector2(0.84f, 0.82f));
        MenuUi.AddText(card.transform, "Start a new profile?", font, 40, FontStyle.Bold, Color.white, 56, TextAnchor.MiddleCenter);
        MenuUi.AddText(
            card.transform,
            "This creates a new profile at Level 1 with 0 XP. Your current profile, dictionary, grammar and revision stay in Saved profiles. Continue opens the profile you used most recently.",
            font,
            26,
            FontStyle.Normal,
            MenuUi.Muted,
            150,
            TextAnchor.MiddleCenter);
        MenuUi.CreateButton(card.transform, "Create new profile", font, MenuUi.Amber, OpenCreate, 72);
        MenuUi.CreateButton(card.transform, "Cancel", font, MenuUi.Stone, ShowHome, 72);
        return root;
    }

    GameObject BuildCreate(Transform parent)
    {
        GameObject root = MenuUi.CreateRect("Create", parent);
        MenuUi.Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        GameObject card = MenuUi.CreateCard(root.transform, new Vector2(0.1f, 0.06f), new Vector2(0.9f, 0.94f));
        MenuUi.AddText(card.transform, "New Game", font, 40, FontStyle.Bold, Color.white, 52, TextAnchor.MiddleCenter);
        MenuUi.AddText(card.transform, "Nickname", font, 24, FontStyle.Bold, MenuUi.Paper, 32, TextAnchor.MiddleLeft);
        nicknameField = MenuUi.CreateNicknameField(card.transform, font);
        errorText = MenuUi.AddText(card.transform, "", font, 22, FontStyle.Normal, new Color(1f, 0.7f, 0.55f), 32, TextAnchor.MiddleLeft);

        MenuUi.AddText(card.transform, "Avatar", font, 24, FontStyle.Bold, MenuUi.Paper, 32, TextAnchor.MiddleLeft);
        GameObject avatars = CreateChoiceRow(card.transform, 80);
        avatarImages = new Image[AvatarChoices.All.Length];
        for (int i = 0; i < AvatarChoices.All.Length; i++)
        {
            AvatarChoice choice = AvatarChoices.All[i];
            Button button = MenuUi.CreateButton(avatars.transform, choice.label, font, choice.color, () => SelectAvatar(choice.id), 80);
            avatarImages[i] = button.GetComponent<Image>();
        }

        MenuUi.AddText(card.transform, "Interface language", font, 24, FontStyle.Bold, MenuUi.Paper, 32, TextAnchor.MiddleLeft);
        GameObject languages = CreateChoiceRow(card.transform, 72);
        languageImages = new Image[InterfaceLanguageChoices.All.Length];
        for (int i = 0; i < InterfaceLanguageChoices.All.Length; i++)
        {
            LanguageChoice choice = InterfaceLanguageChoices.All[i];
            Button button = MenuUi.CreateButton(languages.transform, choice.label, font, MenuUi.Stone, () => SelectLanguage(choice.code), 72);
            languageImages[i] = button.GetComponent<Image>();
        }

        MenuUi.AddText(
            card.transform,
            "Only English translations are in the game so far. Other languages keep the English words until translations are added.",
            font,
            22,
            FontStyle.Normal,
            MenuUi.Muted,
            72,
            TextAnchor.MiddleLeft);
        MenuUi.CreateButton(card.transform, "Start", font, MenuUi.Amber, StartGame, 76);
        MenuUi.CreateButton(card.transform, "Back", font, MenuUi.Stone, ShowHome, 64);
        return root;
    }

    static GameObject CreateChoiceRow(Transform parent, float height)
    {
        GameObject rowObject = MenuUi.CreateRect("Choices", parent);
        LayoutElement layout = rowObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        HorizontalLayoutGroup row = rowObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 12;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = true;
        row.childForceExpandHeight = true;
        return rowObject;
    }

    void SelectAvatar(string id)
    {
        selectedAvatarId = id;
        PaintCreateChoices();
    }

    void SelectLanguage(string code)
    {
        selectedLanguageCode = code;
        PaintCreateChoices();
    }

    void PaintCreateChoices()
    {
        for (int i = 0; i < AvatarChoices.All.Length; i++)
        {
            bool selected = AvatarChoices.All[i].id == selectedAvatarId;
            Color color = AvatarChoices.All[i].color;
            if (!selected)
            {
                color *= 0.45f;
                color.a = 1f;
            }

            avatarImages[i].color = color;
        }

        for (int i = 0; i < InterfaceLanguageChoices.All.Length; i++)
        {
            bool selected = InterfaceLanguageChoices.All[i].code == selectedLanguageCode;
            languageImages[i].color = selected ? MenuUi.Amber : MenuUi.Stone;
        }
    }

    void StartGame()
    {
        string nickname = nicknameField.text == null ? "" : nicknameField.text.Trim();
        if (string.IsNullOrEmpty(nickname))
        {
            errorText.text = "Enter a nickname.";
            return;
        }

        SaveSlots.BeginNewProfile();
        PlayerProfile.SaveNew(nickname, selectedAvatarId, selectedLanguageCode);
        GameScenes.Load(GameScenes.Bedroom);
    }

    void ContinueGame()
    {
        if (!PlayerProfile.HasStarted)
        {
            return;
        }

        GameScenes.Load(GameScenes.CityMap);
    }

    void ShowProfiles()
    {
        homeRoot.SetActive(false);
        createRoot.SetActive(false);
        confirmRoot.SetActive(false);
        settings.Root.SetActive(false);
        profilesRoot.SetActive(true);
        RebuildProfileList();
    }

    GameObject BuildProfiles(Transform parent)
    {
        GameObject root = MenuUi.CreateRect("Profiles", parent);
        MenuUi.Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        GameObject card = MenuUi.CreateCard(root.transform, new Vector2(0.14f, 0.1f), new Vector2(0.86f, 0.9f));
        MenuUi.AddText(card.transform, "Saved profiles", font, 40, FontStyle.Bold, Color.white, 52, TextAnchor.MiddleCenter);
        MenuUi.AddText(card.transform, "Choose a profile to continue. New Game does not delete these.", font, 22, FontStyle.Normal, MenuUi.Muted, 56, TextAnchor.MiddleCenter);
        MenuUi.CreateScroll(card.transform, out profileList);
        MenuUi.CreateButton(card.transform, "Back", font, MenuUi.Stone, ShowHome, 64);
        return root;
    }

    void RebuildProfileList()
    {
        for (int i = profileList.childCount - 1; i >= 0; i--)
        {
            Destroy(profileList.GetChild(i).gameObject);
        }

        List<SaveSlotSummary> slots = SaveSlots.List();
        for (int i = 0; i < slots.Count; i++)
        {
            SaveSlotSummary slot = slots[i];
            int level = 1 + slot.totalXp / 50;
            string label = slot.nickname + "   ·   Level " + level + "   ·   " + slot.totalXp + " XP";
            if (slot.active)
            {
                label += "   ·   Current";
            }

            string id = slot.id;
            MenuUi.CreateButton(profileList, label, font, slot.active ? MenuUi.Amber : MenuUi.Stone, () => PlayProfile(id), 72);
        }
    }

    void PlayProfile(string id)
    {
        SaveSlots.Activate(id);
        PlayerProfile.Reload();
        GameScenes.Load(GameScenes.CityMap);
    }
}
