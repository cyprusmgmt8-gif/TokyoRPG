using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Looks from the player camera. When the centre of the view hits an Interactable Object
/// within reach, it highlights that object and shows a prompt.
/// Press E, or tap the prompt, to open the learning panel.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Tooltip("How far in front of the camera the player can study an object, in metres.")]
    [SerializeField] float reach = 2.5f;

    [Tooltip("The camera the player looks through. Leave empty to use the Main Camera.")]
    [SerializeField] Transform eye;

    [Tooltip("The learning panel. Leave empty and one is created when the scene plays.")]
    [SerializeField] LearningPanel learningPanel;

    readonly RaycastHit[] hits = new RaycastHit[16];
    InteractableObject current;
    FirstPersonController movement;

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
    }

    void Update()
    {
        if (DictionaryPanel.IsOpenNow || (learningPanel != null && learningPanel.IsOpen))
        {
            ClearFocus();
            if (learningPanel != null)
            {
                learningPanel.SetPrompt(null);
            }

            return;
        }

        InteractableObject lookedAt = FindLookTarget();
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

        if (learningPanel != null)
        {
            learningPanel.SetPrompt(current != null ? current.PromptName : null);
        }

        if (current != null && WasInteractPressed())
        {
            Interact();
        }
    }

    public bool HasLookTarget => current != null;

    public void Interact()
    {
        if (current == null || learningPanel == null || learningPanel.IsOpen)
        {
            return;
        }

        InteractableObject selected = current;
        ClearFocus();
        learningPanel.SetPrompt(null);
        learningPanel.Open(selected, movement);
    }

    InteractableObject FindLookTarget()
    {
        if (eye == null)
        {
            return null;
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
            return null;
        }

        return closestHit.collider.GetComponentInParent<InteractableObject>();
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
