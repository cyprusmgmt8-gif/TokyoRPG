using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The city map. Put this on an empty object in the City Map scene.
/// The bedroom can be entered. Other places explain premium access until the test switch is on.
/// </summary>
public class CityMapScreen : MonoBehaviour
{
    Font font;
    Text nameText;
    Text accessText;
    Image avatarImage;
    Text avatarLetter;
    RectTransform listContent;
    GameObject detailRoot;
    Text detailText;
    SettingsPanel settings;

    void Awake()
    {
        PlayerProfile.EnsureLoaded();
        AccessEntitlement.EnsureLoaded();
        MenuUi.ShowCursor();
        MenuUi.PaintNightSky();
        font = MenuUi.FindFont();
        MenuUi.EnsureEventSystem();

        Canvas canvas = MenuUi.CreateCanvas(transform, "Map Canvas", 10);
        BuildMap(canvas.transform);
        settings = new SettingsPanel(canvas.transform, font, CloseSettings);
        settings.Root.SetActive(false);
        detailRoot.SetActive(false);
        RefreshHeader();
        RebuildList();
    }

    void OnEnable()
    {
        AccessEntitlement.Changed += OnAccessChanged;
    }

    void OnDisable()
    {
        AccessEntitlement.Changed -= OnAccessChanged;
    }

    void OnAccessChanged()
    {
        RefreshHeader();
        RebuildList();
        if (detailRoot != null)
        {
            detailRoot.SetActive(false);
        }
    }

    void BuildMap(Transform parent)
    {
        GameObject card = MenuUi.CreateCard(parent, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));
        MenuUi.AddText(card.transform, "Tokyo", font, 48, FontStyle.Bold, MenuUi.Paper, 58, TextAnchor.MiddleCenter);

        GameObject identity = MenuUi.CreateRect("Player", card.transform);
        LayoutElement identityLayout = identity.AddComponent<LayoutElement>();
        identityLayout.preferredHeight = 64;
        HorizontalLayoutGroup identityRow = identity.AddComponent<HorizontalLayoutGroup>();
        identityRow.spacing = 16;
        identityRow.childAlignment = TextAnchor.MiddleCenter;
        identityRow.childControlWidth = true;
        identityRow.childControlHeight = true;
        identityRow.childForceExpandWidth = false;
        identityRow.childForceExpandHeight = true;

        GameObject badge = MenuUi.CreateRect("Badge", identity.transform);
        LayoutElement badgeLayout = badge.AddComponent<LayoutElement>();
        badgeLayout.preferredWidth = 64;
        badgeLayout.preferredHeight = 64;
        avatarImage = badge.AddComponent<Image>();
        avatarImage.raycastTarget = false;
        avatarLetter = MenuUi.AddText(badge.transform, "T", font, 28, FontStyle.Bold, Color.white, 64, TextAnchor.MiddleCenter);
        MenuUi.Stretch(avatarLetter.rectTransform, Vector2.zero, Vector2.one);
        avatarLetter.GetComponent<LayoutElement>().ignoreLayout = true;

        nameText = MenuUi.AddText(identity.transform, "", font, 28, FontStyle.Bold, Color.white, 64, TextAnchor.MiddleLeft);
        LayoutElement nameLayout = nameText.GetComponent<LayoutElement>();
        nameLayout.preferredWidth = 700;
        nameLayout.flexibleWidth = 1;

        accessText = MenuUi.AddText(card.transform, "", font, 22, FontStyle.Normal, MenuUi.Muted, 64, TextAnchor.MiddleCenter);

        GameObject buttons = MenuUi.CreateRect("Buttons", card.transform);
        LayoutElement buttonLayout = buttons.AddComponent<LayoutElement>();
        buttonLayout.preferredHeight = 64;
        HorizontalLayoutGroup buttonRow = buttons.AddComponent<HorizontalLayoutGroup>();
        buttonRow.spacing = 12;
        buttonRow.childControlWidth = true;
        buttonRow.childControlHeight = true;
        buttonRow.childForceExpandWidth = true;
        buttonRow.childForceExpandHeight = true;
        MenuUi.CreateButton(buttons.transform, "Menu", font, MenuUi.Stone, () => GameScenes.Load(GameScenes.MainMenu), 64);
        MenuUi.CreateButton(buttons.transform, "Settings", font, MenuUi.Stone, OpenSettings, 64);

        MenuUi.CreateScroll(card.transform, out listContent);
        BuildDetail(parent);
    }

    void BuildDetail(Transform parent)
    {
        detailRoot = MenuUi.CreateRect("Detail", parent);
        MenuUi.Stretch(detailRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        Image dim = detailRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.62f);
        GameObject card = MenuUi.CreateCard(detailRoot.transform, new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.82f));
        detailText = MenuUi.AddText(card.transform, "", font, 26, FontStyle.Normal, Color.white, 420, TextAnchor.UpperLeft);
        MenuUi.CreateButton(card.transform, "Settings", font, MenuUi.Amber, OpenSettings, 72);
        MenuUi.CreateButton(card.transform, "Back", font, MenuUi.Stone, () => detailRoot.SetActive(false), 72);
    }

    void RefreshHeader()
    {
        AvatarChoice avatar = AvatarChoices.Find(PlayerProfile.Data.avatarId);
        avatarImage.color = avatar.color;
        avatarLetter.text = avatar.label.Substring(0, 1);
        nameText.text = PlayerProfile.HasStarted ? PlayerProfile.Data.nickname : "Guest";
        accessText.text = AccessEntitlement.HasPremium
            ? "Premium test is on. Places can be selected. Only the bedroom is built."
            : "Free version. The bedroom is open. Other places need premium access.";
    }

    void RebuildList()
    {
        if (listContent == null)
        {
            return;
        }

        for (int i = listContent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(listContent.GetChild(i).gameObject);
        }

        for (int i = 0; i < CityDirectory.All.Length; i++)
        {
            CreateRow(CityDirectory.All[i]);
        }
    }

    void CreateRow(CityLocation place)
    {
        bool openNow = place.includedInFreeVersion || (AccessEntitlement.HasPremium && !string.IsNullOrEmpty(place.sceneName));
        string status = place.includedInFreeVersion
            ? "Open"
            : AccessEntitlement.HasPremium ? "Not built yet" : "Locked";
        Color color = place.includedInFreeVersion
            ? MenuUi.Amber
            : AccessEntitlement.HasPremium
                ? new Color(0.24f, 0.34f, 0.46f, 1f)
                : new Color(0.18f, 0.19f, 0.22f, 1f);

        Button button = MenuUi.CreateButton(
            listContent,
            place.japaneseName + "\n" + place.reading + "    " + place.englishName + "    " + status,
            font,
            color,
            () => Select(place),
            118);
        Text label = button.GetComponentInChildren<Text>();
        label.fontSize = 26;
        label.alignment = TextAnchor.MiddleLeft;
        label.rectTransform.offsetMin = new Vector2(24f, 8f);
        label.rectTransform.offsetMax = new Vector2(-24f, -8f);
        if (!openNow)
        {
            label.color = MenuUi.Paper;
        }
    }

    void Select(CityLocation place)
    {
        bool unlocked = place.includedInFreeVersion || AccessEntitlement.HasPremium;
        if (unlocked && !string.IsNullOrEmpty(place.sceneName))
        {
            GameScenes.Load(place.sceneName);
            return;
        }

        if (unlocked)
        {
            detailText.text = place.japaneseName + "  " + place.reading + "\n"
                + place.englishName + "\n\n"
                + place.summary + "\n\n"
                + "This place is unlocked for the premium test. The room is not built yet. The bedroom is ready to visit.";
        }
        else
        {
            detailText.text = place.japaneseName + "  " + place.reading + "\n"
                + place.englishName + "\n\n"
                + place.summary + "\n\n"
                + "Premium access\n"
                + "The free game includes the bedroom only. This place opens with premium access.\n\n"
                + "This prototype does not take payment. Open Settings and turn on Simulate premium to test the unlocked map.";
        }

        detailRoot.SetActive(true);
    }

    void OpenSettings()
    {
        detailRoot.SetActive(false);
        settings.Refresh();
        settings.Root.SetActive(true);
    }

    void CloseSettings()
    {
        settings.Root.SetActive(false);
        RefreshHeader();
        RebuildList();
    }
}
