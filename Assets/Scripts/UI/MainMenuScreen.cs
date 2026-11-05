using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The title screen. Put this on an empty object in the Main Menu scene.
/// It builds the buttons when play starts.
/// </summary>
public class MainMenuScreen : MonoBehaviour
{
    Font font;
    GameObject homeRoot;
    GameObject createRoot;
    GameObject confirmRoot;
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

    void Awake()
    {
        PlayerProfile.EnsureLoaded();
        AccessEntitlement.EnsureLoaded();
        MenuUi.ShowCursor();
        MenuUi.PaintNightSky();
        font = MenuUi.FindFont();
        MenuUi.EnsureEventSystem();

        Canvas canvas = MenuUi.CreateCanvas(transform, "Menu Canvas", 10);
        homeRoot = BuildHome(canvas.transform);
        createRoot = BuildCreate(canvas.transform);
        confirmRoot = BuildConfirm(canvas.transform);
        settings = new SettingsPanel(canvas.transform, font, ShowHome);
        ShowHome();
    }

    void ShowHome()
    {
        homeRoot.SetActive(true);
        createRoot.SetActive(false);
        confirmRoot.SetActive(false);
        settings.Root.SetActive(false);
        RefreshHome();
    }

    void RefreshHome()
    {
        bool saved = PlayerProfile.HasStarted;
        continueButton.interactable = saved;
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
        continueButton = MenuUi.CreateButton(card.transform, "Continue", font, MenuUi.Stone, () => GameScenes.Load(GameScenes.CityMap), 76);
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
        settings.Refresh();
        settings.Root.SetActive(true);
    }

    GameObject BuildConfirm(Transform parent)
    {
        GameObject root = MenuUi.CreateRect("Confirm", parent);
        MenuUi.Stretch(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        GameObject card = MenuUi.CreateCard(root.transform, new Vector2(0.18f, 0.28f), new Vector2(0.82f, 0.72f));
        MenuUi.AddText(card.transform, "Start a new profile?", font, 40, FontStyle.Bold, Color.white, 56, TextAnchor.MiddleCenter);
        MenuUi.AddText(
            card.transform,
            "This replaces your nickname and avatar. Words in your dictionary and your XP stay on this device.",
            font,
            26,
            FontStyle.Normal,
            MenuUi.Muted,
            100,
            TextAnchor.MiddleCenter);
        MenuUi.CreateButton(card.transform, "Replace profile", font, MenuUi.Amber, OpenCreate, 72);
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

        PlayerProfile.SaveNew(nickname, selectedAvatarId, selectedLanguageCode);
        GameScenes.Load(GameScenes.CityMap);
    }
}
