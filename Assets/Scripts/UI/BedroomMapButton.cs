using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puts a Map button on the bedroom screen and returns to the city map.
/// This is added on the Player object.
/// </summary>
public class BedroomMapButton : MonoBehaviour
{
    void Awake()
    {
        MenuUi.EnsureEventSystem();
        Canvas canvas = MenuUi.CreateCanvas(transform, "Map Button Canvas", 30);
        GameplayHud.Register(canvas.gameObject);
        GameObject buttonObject = MenuUi.CreateRect("Map", canvas.transform);
        MenuUi.Stretch(buttonObject.GetComponent<RectTransform>(), new Vector2(0.56f, 0.86f), new Vector2(0.73f, 0.98f));
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.14f, 0.13f, 0.12f, 0.92f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => GameScenes.Load(GameScenes.CityMap));
        Text label = MenuUi.AddText(buttonObject.transform, "Map", MenuUi.FindFont(), 28, FontStyle.Bold, new Color(1f, 0.93f, 0.8f), 40, TextAnchor.MiddleCenter);
        MenuUi.Stretch(label.rectTransform, Vector2.zero, Vector2.one);
        label.GetComponent<LayoutElement>().ignoreLayout = true;
    }
}
