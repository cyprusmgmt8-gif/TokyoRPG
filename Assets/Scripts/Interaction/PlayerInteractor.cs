using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Looks from the player camera. The closest hit within reach decides the action.
/// A product opens the study card. The register opens checkout. A person opens talk.
/// A doorway loads its scene. Study and talk are checked first.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    /// <summary>
    /// Raised once when a study card opens for an object.
    /// The vocabulary id is the id stored on that object.
    /// </summary>
    public static event Action<string> Studied;

    [Tooltip("How far in front of the camera the player can study an object, in metres.")]
    [SerializeField] float reach = 2.5f;

    [Tooltip("The camera the player looks through. Leave empty to use the Main Camera.")]
    [SerializeField] Transform eye;

    [Tooltip("The learning panel. Leave empty and one is created when the scene plays.")]
    [SerializeField] LearningPanel learningPanel;

    readonly RaycastHit[] hits = new RaycastHit[16];
    InteractableObject current;
    CheckoutPoint checkout;
    DialoguePoint talk;
    SceneEntrance entrance;
    FirstPersonController movement;
    GameObject entrancePrompt;
    Text entrancePromptText;

    void Awake()
    {
        movement = GetComponent<FirstPersonController>();

        if (eye == null && Camera.main != null)
        {
            eye = Camera.main.transform;
        }

        if (learningPanel == null)
        {
            learningPanel = FindAnyObjectByType<LearningPanel>();
        }

        if (learningPanel == null)
        {
            GameObject panelObject = new GameObject("Learning Panel");
            learningPanel = panelObject.AddComponent<LearningPanel>();
        }

        if (FindAnyObjectByType<DialoguePoint>() != null && GetComponent<DialoguePanel>() == null)
        {
            gameObject.AddComponent<DialoguePanel>();
        }

        BuildEntrancePrompt();
    }

    void OnEnable()
    {
        if (learningPanel != null)
        {
            learningPanel.PromptClicked += Interact;
        }
    }

    void OnDisable()
    {
        if (learningPanel != null)
        {
            learningPanel.PromptClicked -= Interact;
        }

        ClearFocus();
        entrance = null;
        ShowEntrancePrompt(false);
    }

    void Update()
    {
        if (ScreenBlocksLook())
        {
            ClearFocus();
            checkout = null;
            talk = null;
            entrance = null;
            ShowEntrancePrompt(false);
            if (learningPanel != null)
            {
                learningPanel.SetPrompt(null);
            }

            return;
        }

        FindLookTarget(out InteractableObject lookedAt, out CheckoutPoint lookedCheckout, out DialoguePoint lookedTalk, out SceneEntrance lookedEntrance);
        if (lookedAt != current)
        {
            if (current != null)
            {
                current.SetFocused(false);
            }

            current = lookedAt;

            if (current != null)
            {
                current.SetFocused(true);
            }
        }

        checkout = lookedCheckout;
        talk = lookedTalk;
        entrance = lookedEntrance;

        if (learningPanel != null)
        {
            learningPanel.SetPrompt(current != null ? current.PromptName : null);
        }

        ShowEntrancePrompt(current == null && talk == null && entrance != null);

        if (!WasInteractPressed())
        {
            return;
        }

        if (current != null)
        {
            Interact();
        }
        else if (talk != null)
        {
            OpenTalk();
        }
        else if (entrance != null)
        {
            SceneEntrance selected = entrance;
            entrance = null;
            ShowEntrancePrompt(false);
            selected.Enter();
        }
    }

    bool ScreenBlocksLook()
    {
        bool study = learningPanel != null && learningPanel.IsOpen;
        return study || DictionaryPanel.IsOpenNow || GrammarPanel.IsOpenNow || RevisionPanel.IsOpenNow || BasketPanel.IsOpenNow || QuestPanel.IsOpenNow || CheckoutPanel.IsOpenNow || DialoguePanel.IsOpenNow || CityMapScreen.IsOpenNow || MainMenuScreen.IsOpenNow;
    }

    public bool HasLookTarget => current != null;

    /// <summary>
    /// The object currently in the centre of the view, within reach.
    /// The basket uses this. It does not cast its own ray.
    /// </summary>
    public InteractableObject LookTarget => current;

    /// <summary>
    /// The register currently in the centre of the view, when no study object is closer.
    /// </summary>
    public CheckoutPoint CheckoutTarget => checkout;

    /// <summary>
    /// The person currently in the centre of the view, when no product or register is closer.
    /// </summary>
    public DialoguePoint TalkTarget => talk;

    void OpenTalk()
    {
        if (talk == null)
        {
            return;
        }

        DialoguePanel panel = GetComponent<DialoguePanel>();
        if (panel == null)
        {
            return;
        }

        DialoguePoint selected = talk;
        talk = null;
        if (learningPanel != null)
        {
            learningPanel.SetPrompt(null);
        }

        panel.Open(selected);
    }

    public void Interact()
    {
        if (current == null || learningPanel == null || learningPanel.IsOpen)
        {
            return;
        }

        InteractableObject selected = current;
        string vocabularyId = selected.VocabularyId;
        ClearFocus();
        learningPanel.SetPrompt(null);
        learningPanel.Open(selected, movement);
        if (!learningPanel.IsOpen || string.IsNullOrWhiteSpace(vocabularyId))
        {
            return;
        }

        Studied?.Invoke(vocabularyId);
    }

    void FindLookTarget(out InteractableObject studyTarget, out CheckoutPoint checkoutTarget, out DialoguePoint talkTarget, out SceneEntrance entranceTarget)
    {
        studyTarget = null;
        checkoutTarget = null;
        talkTarget = null;
        entranceTarget = null;
        if (eye == null)
        {
            return;
        }

        Ray ray = new Ray(eye.position, eye.forward);
        int count = Physics.RaycastNonAlloc(ray, hits, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        float nearest = float.MaxValue;
        RaycastHit closestHit = default;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            Transform hitTransform = hits[i].collider.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
            {
                continue;
            }

            if (hits[i].distance < nearest)
            {
                nearest = hits[i].distance;
                closestHit = hits[i];
                found = true;
            }
        }

        if (!found)
        {
            return;
        }

        studyTarget = closestHit.collider.GetComponentInParent<InteractableObject>();
        if (studyTarget != null)
        {
            return;
        }

        talkTarget = closestHit.collider.GetComponentInParent<DialoguePoint>();
        if (talkTarget != null)
        {
            return;
        }

        entranceTarget = closestHit.collider.GetComponentInParent<SceneEntrance>();
    }

    void BuildEntrancePrompt()
    {
        Font font = MenuUi.FindFont();
        Canvas canvas = MenuUi.CreateCanvas(transform, "Entrance Prompt Canvas", 17);
        entrancePrompt = MenuUi.CreateRect("Entrance Prompt", canvas.transform);
        MenuUi.Stretch(entrancePrompt.GetComponent<RectTransform>(), new Vector2(0.30f, 0.40f), new Vector2(0.70f, 0.52f));
        Image image = entrancePrompt.AddComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.1f, 0.88f);
        Button button = entrancePrompt.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(EnterFromPrompt);
        entrancePromptText = MenuUi.AddText(entrancePrompt.transform, "E  —  Enter", font, 32, FontStyle.Bold, MenuUi.Paper, 56, TextAnchor.MiddleCenter);
        MenuUi.Stretch(entrancePromptText.rectTransform, Vector2.zero, Vector2.one);
        entrancePromptText.GetComponent<LayoutElement>().ignoreLayout = true;
        entrancePrompt.SetActive(false);
    }

    void ShowEntrancePrompt(bool show)
    {
        if (entrancePrompt == null)
        {
            return;
        }

        if (show && entrance != null && entrancePromptText != null)
        {
            entrancePromptText.text = "E  —  " + entrance.PromptLabel;
        }

        if (entrancePrompt.activeSelf != show)
        {
            entrancePrompt.SetActive(show);
        }
    }

    void EnterFromPrompt()
    {
        if (entrance == null || ScreenBlocksLook())
        {
            return;
        }

        SceneEntrance selected = entrance;
        entrance = null;
        ShowEntrancePrompt(false);
        selected.Enter();
    }

    bool WasInteractPressed()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.eKey.wasPressedThisFrame;
    }

    void ClearFocus()
    {
        if (current != null)
        {
            current.SetFocused(false);
            current = null;
        }
    }
}
