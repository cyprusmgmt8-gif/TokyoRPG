using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puts the Menu and Map buttons on the gameplay screen.
/// Menu opens the existing title menu over the room. Map opens the city map.
/// This is on the Player. Bedroom, Konbini and VisitorTest all use it.
/// </summary>
public class BedroomMapButton : MonoBehaviour
{
    void Awake()
    {
        MenuUi.EnsureEventSystem();
        Canvas canvas = MenuUi.CreateCanvas(transform, "Map Button Canvas", 30);
        GameplayHud.Register(canvas.gameObject);
        CreateChip(canvas.transform, "Menu", new Vector2(0.330f, 0.945f), new Vector2(0.418f, 0.985f), OpenMenu);
        CreateChip(canvas.transform, "Map", new Vector2(0.710f, 0.945f), new Vector2(0.798f, 0.985f), OpenMap);
    }

    void OpenMenu()
    {
        if (CityMapScreen.IsOpenNow)
        {
            return;
        }

        MainMenuScreen.OpenOverGameplay(transform);
    }

    void OpenMap()
    {
        if (MainMenuScreen.IsOpenNow)
        {
            return;
        }

        CityMapScreen.OpenOverGameplay(transform);
    }

    static void CreateChip(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = MenuUi.CreateRect(label, parent);
        MenuUi.Stretch(buttonObject.GetComponent<RectTransform>(), anchorMin, anchorMax);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.11f, 0.105f, 0.1f, 0.72f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        Text text = MenuUi.AddText(buttonObject.transform, label, MenuUi.FindFont(), 20, FontStyle.Bold, MenuUi.Paper, 36, TextAnchor.MiddleCenter);
        MenuUi.Stretch(text.rectTransform, Vector2.zero, Vector2.one);
        text.GetComponent<LayoutElement>().ignoreLayout = true;
    }
}
