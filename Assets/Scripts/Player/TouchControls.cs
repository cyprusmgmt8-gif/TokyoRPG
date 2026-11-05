using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Landscape touch controls for the bedroom.
/// The joystick walks, a drag on the right side looks, and Study uses the object in front of the camera.
/// Keyboard and mouse keep working beside these controls.
/// </summary>
public class TouchControls : MonoBehaviour
{
    FirstPersonController movement;
    PlayerInteractor interactor;
    GameObject controlsRoot;
    RectTransform safeArea;
    VirtualJoystick joystick;
    Image studyImage;
    Rect lastSafe;

    void Awake()
    {
        movement = GetComponent<FirstPersonController>();
        interactor = GetComponent<PlayerInteractor>();
        MenuUi.EnsureEventSystem();
        PrepareCamera();
        BuildUi();
    }

    void Update()
    {
        ApplySafeArea();

        bool playing = movement == null || movement.enabled;
        if (controlsRoot.activeSelf != playing)
        {
            if (!playing)
            {
                joystick.ResetStick();
            }

            controlsRoot.SetActive(playing);
        }

        if (movement == null)
        {
            return;
        }

        if (!playing)
        {
            movement.SetTouchMove(Vector2.zero);
            return;
        }

        movement.SetTouchMove(joystick.Value);
        bool ready = interactor != null && interactor.HasLookTarget;
        studyImage.color = ready
            ? new Color(0.72f, 0.42f, 0.22f, 0.96f)
            : new Color(0.22f, 0.2f, 0.18f, 0.72f);
    }

    static void PrepareCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        camera.allowHDR = false;
        camera.allowMSAA = false;
    }

    void BuildUi()
    {
        Canvas canvas = MenuUi.CreateCanvas(transform, "Touch Canvas", 8);
        controlsRoot = MenuUi.CreateRect("Controls", canvas.transform);
        MenuUi.Stretch(controlsRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        GameObject safeObject = MenuUi.CreateRect("Safe Area", controlsRoot.transform);
        safeArea = safeObject.GetComponent<RectTransform>();
        MenuUi.Stretch(safeArea, Vector2.zero, Vector2.one);

        GameObject lookObject = MenuUi.CreateRect("Look", safeObject.transform);
        MenuUi.Stretch(lookObject.GetComponent<RectTransform>(), new Vector2(0.46f, 0.02f), new Vector2(1f, 0.78f));
        Image lookImage = lookObject.AddComponent<Image>();
        lookImage.color = new Color(1f, 1f, 1f, 0.02f);
        SwipeLookPad look = lookObject.AddComponent<SwipeLookPad>();
        look.Movement = movement;
        Text hint = MenuUi.AddText(lookObject.transform, "Drag to look", MenuUi.FindFont(), 28, FontStyle.Bold, new Color(1f, 0.93f, 0.8f, 0.45f), 40, TextAnchor.MiddleCenter);
        MenuUi.Stretch(hint.rectTransform, new Vector2(0.15f, 0.4f), new Vector2(0.85f, 0.6f));
        hint.GetComponent<LayoutElement>().ignoreLayout = true;

        GameObject stickObject = MenuUi.CreateRect("Joystick", safeObject.transform);
        MenuUi.Stretch(stickObject.GetComponent<RectTransform>(), new Vector2(0.02f, 0.03f), new Vector2(0.34f, 0.5f));
        Image stickHit = stickObject.AddComponent<Image>();
        stickHit.color = new Color(1f, 1f, 1f, 0.02f);
        joystick = stickObject.AddComponent<VirtualJoystick>();

        GameObject ring = MenuUi.CreateRect("Ring", stickObject.transform);
        RectTransform ringRect = ring.GetComponent<RectTransform>();
        ringRect.anchorMin = new Vector2(0.5f, 0.5f);
        ringRect.anchorMax = new Vector2(0.5f, 0.5f);
        ringRect.sizeDelta = new Vector2(190f, 190f);
        Image ringImage = ring.AddComponent<Image>();
        ringImage.color = new Color(0.14f, 0.13f, 0.12f, 0.55f);
        ringImage.raycastTarget = false;

        GameObject knob = MenuUi.CreateRect("Knob", stickObject.transform);
        RectTransform knobRect = knob.GetComponent<RectTransform>();
        knobRect.anchorMin = new Vector2(0.5f, 0.5f);
        knobRect.anchorMax = new Vector2(0.5f, 0.5f);
        knobRect.sizeDelta = new Vector2(96f, 96f);
        Image knobImage = knob.AddComponent<Image>();
        knobImage.color = new Color(1f, 0.93f, 0.8f, 0.95f);
        knobImage.raycastTarget = false;
        joystick.Configure(stickObject.GetComponent<RectTransform>(), knobRect);

        GameObject studyObject = MenuUi.CreateRect("Study", safeObject.transform);
        MenuUi.Stretch(studyObject.GetComponent<RectTransform>(), new Vector2(0.78f, 0.04f), new Vector2(0.98f, 0.34f));
        studyImage = studyObject.AddComponent<Image>();
        studyImage.color = new Color(0.22f, 0.2f, 0.18f, 0.72f);
        Button study = studyObject.AddComponent<Button>();
        study.targetGraphic = studyImage;
        study.onClick.AddListener(OnStudy);
        Text studyLabel = MenuUi.AddText(studyObject.transform, "Study", MenuUi.FindFont(), 32, FontStyle.Bold, Color.white, 48, TextAnchor.MiddleCenter);
        MenuUi.Stretch(studyLabel.rectTransform, Vector2.zero, Vector2.one);
        studyLabel.GetComponent<LayoutElement>().ignoreLayout = true;
    }

    void OnStudy()
    {
        if (interactor != null)
        {
            interactor.Interact();
        }
    }

    void ApplySafeArea()
    {
        Rect safe = Screen.safeArea;
        if (safe == lastSafe)
        {
            return;
        }

        lastSafe = safe;
        float width = Mathf.Max(1f, Screen.width);
        float height = Mathf.Max(1f, Screen.height);
        safeArea.anchorMin = new Vector2(safe.xMin / width, safe.yMin / height);
        safeArea.anchorMax = new Vector2(safe.xMax / width, safe.yMax / height);
        safeArea.offsetMin = Vector2.zero;
        safeArea.offsetMax = Vector2.zero;
    }
}

/// <summary>
/// Keeps a phone build in landscape and caps the frame rate.
/// The Unity Editor keeps its own game-view size and mouse controls.
/// </summary>
public static class MobileDisplay
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
#if !UNITY_EDITOR
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;
#endif
    }
}

public class SwipeLookPad : MonoBehaviour, IDragHandler
{
    public FirstPersonController Movement;

    public void OnDrag(PointerEventData eventData)
    {
        if (Movement != null)
        {
            Movement.AddTouchLook(eventData.delta);
        }
    }
}

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public Vector2 Value { get; private set; }

    RectTransform area;
    RectTransform knob;

    public void Configure(RectTransform stickArea, RectTransform stickKnob)
    {
        area = stickArea;
        knob = stickKnob;
    }

    public void ResetStick()
    {
        Value = Vector2.zero;
        if (knob != null)
        {
            knob.anchoredPosition = Vector2.zero;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Move(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Move(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetStick();
    }

    void Move(PointerEventData eventData)
    {
        if (area == null)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out Vector2 local))
        {
            return;
        }

        float travel = Mathf.Min(area.rect.width, area.rect.height) * 0.28f;
        if (travel < 1f)
        {
            travel = 70f;
        }

        Vector2 clamped = Vector2.ClampMagnitude(local, travel);
        if (knob != null)
        {
            knob.anchoredPosition = clamped;
        }

        Value = clamped / travel;
    }
}
