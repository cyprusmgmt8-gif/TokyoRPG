using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Language choice and the premium test switch.
/// The same panel is used by the main menu and the city map.
/// </summary>
public class SettingsPanel
{
    public GameObject Root { get; }

    readonly Font font;
    readonly Action onClose;
    readonly Text premiumLabel;
    readonly Image[] languageImages;
    readonly Text statusText;

    public SettingsPanel(Transform parent, Font font, Action onClose)
    {
        this.font = font;
        this.onClose = onClose;

        Root = MenuUi.CreateRect("Settings", parent);
        MenuUi.Stretch(Root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        Image dim = Root.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject card = MenuUi.CreateCard(Root.transform, new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.9f));
        MenuUi.AddText(card.transform, "Settings", font, 40, FontStyle.Bold, Color.white, 52, TextAnchor.MiddleCenter);
        statusText = MenuUi.AddText(card.transform, "", font, 24, FontStyle.Normal, MenuUi.Muted, 36, TextAnchor.MiddleCenter);

        MenuUi.AddText(card.transform, "Interface language", font, 26, FontStyle.Bold, MenuUi.Paper, 36, TextAnchor.MiddleLeft);
        GameObject languages = MenuUi.CreateRect("Languages", card.transform);
        LayoutElement languageLayout = languages.AddComponent<LayoutElement>();
        languageLayout.preferredHeight = 72;
        HorizontalLayoutGroup languageRow = languages.AddComponent<HorizontalLayoutGroup>();
        languageRow.spacing = 12;
        languageRow.childControlWidth = true;
        languageRow.childControlHeight = true;
        languageRow.childForceExpandWidth = true;
        languageRow.childForceExpandHeight = true;

        languageImages = new Image[InterfaceLanguageChoices.All.Length];
        for (int i = 0; i < InterfaceLanguageChoices.All.Length; i++)
        {
            LanguageChoice choice = InterfaceLanguageChoices.All[i];
            Button button = MenuUi.CreateButton(languages.transform, choice.label, font, MenuUi.Stone, () => ChooseLanguage(choice.code), 72);
            languageImages[i] = button.GetComponent<Image>();
        }

        MenuUi.AddText(
            card.transform,
            "Only English translations are in the game so far. Other languages keep the English words until translations are added.",
            font,
            22,
            FontStyle.Normal,
            MenuUi.Muted,
            80,
            TextAnchor.MiddleLeft);

        MenuUi.AddText(card.transform, "Premium test", font, 26, FontStyle.Bold, MenuUi.Paper, 36, TextAnchor.MiddleLeft);
        Button premiumButton = MenuUi.CreateButton(card.transform, "Simulate premium", font, MenuUi.Amber, TogglePremium, 72);
        premiumLabel = premiumButton.GetComponentInChildren<Text>();
        MenuUi.AddText(
            card.transform,
            "This switch is for testing in the Unity Editor. It does not buy anything, and no payment is taken.",
            font,
            22,
            FontStyle.Normal,
            MenuUi.Muted,
            80,
            TextAnchor.MiddleLeft);

        MenuUi.CreateButton(card.transform, "Back", font, MenuUi.Stone, () => onClose(), 72);
        Refresh();
    }

    public void Refresh()
    {
        PlayerProfile.EnsureLoaded();
        AccessEntitlement.EnsureLoaded();
        string code = PlayerProfile.CurrentLanguageCode("en");
        statusText.text = PlayerProfile.HasStarted
            ? "Playing as " + PlayerProfile.Data.nickname
            : "No nickname yet. Use New Game on the main menu.";

        for (int i = 0; i < InterfaceLanguageChoices.All.Length; i++)
        {
            bool selected = string.Equals(InterfaceLanguageChoices.All[i].code, code, StringComparison.OrdinalIgnoreCase);
            languageImages[i].color = selected ? MenuUi.Amber : MenuUi.Stone;
        }

        premiumLabel.text = AccessEntitlement.HasPremium ? "Simulate premium: On" : "Simulate premium: Off";
    }

    void ChooseLanguage(string code)
    {
        PlayerProfile.SetLanguage(code);
        Refresh();
    }

    void TogglePremium()
    {
        AccessEntitlement.SetSimulatedPremium(!AccessEntitlement.HasPremium);
        Refresh();
    }
}
