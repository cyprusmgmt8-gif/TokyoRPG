using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puts a Map button on the gameplay screen and opens the city map over the room.
/// This is added on the Player object. Bedroom and Konbini both use it.
/// </summary>
public class BedroomMapButton : MonoBehaviour
{
    void Awake()
    {
        MenuUi.EnsureEventSystem();
        Canvas canvas = MenuUi.CreateCanvas(transform, "Map Button Canvas", 30);
        GameplayHud.Register(canvas.gameObject);
        GameObject buttonObject = MenuUi.CreateRect("Map", canvas.transform);
        MenuUi.Stretch(buttonObject.GetComponent<RectTransform>(), new Vector2(0.710f, 0.945f), new Vector2(0.798f, 0.985f));
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.11f, 0.105f, 0.1f, 0.72f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => CityMapScreen.OpenOverGameplay(transform));
        Text label = MenuUi.AddText(buttonObject.transform, "Map", MenuUi.FindFont(), 20, FontStyle.Bold, MenuUi.Paper, 36, TextAnchor.MiddleCenter);
        MenuUi.Stretch(label.rectTransform, Vector2.zero, Vector2.one);
        label.GetComponent<LayoutElement>().ignoreLayout = true;
    }
}
