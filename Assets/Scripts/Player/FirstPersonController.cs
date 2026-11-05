using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// First-person walking and mouse look for the bedroom prototype.
/// The body turns left and right. The child camera looks up and down.
/// Gravity and walls are handled by the Character Controller on this object.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Walking speed in metres per second.")]
    [SerializeField] float walkSpeed = 3.2f;

    [Tooltip("How fast the player falls. Higher means a heavier landing.")]
    [SerializeField] float gravity = 9.81f;

    [Header("Mouse look")]
    [Tooltip("How quickly the view turns. This is degrees per pixel of mouse movement.")]
    [SerializeField] float lookSensitivity = 0.1f;

    [Tooltip("Highest upward look angle.")]
    [SerializeField] float maxLookUp = 80f;

    [Tooltip("Lowest downward look angle.")]
    [SerializeField] float maxLookDown = 80f;

    [Header("References")]
    [Tooltip("The camera that sits at the player's eye height. Drag the child Camera here if this is empty.")]
    [SerializeField] Transform eyeCamera;

    CharacterController body;
    float verticalSpeed;
    float pitch;
    Vector2 touchMove;
    Vector2 pendingLook;
    readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    /// <summary>
    /// Joystick input. X is strafe, Y is forward. Each axis is from -1 to 1.
    /// </summary>
    public void SetTouchMove(Vector2 axes)
    {
        touchMove = Vector2.ClampMagnitude(axes, 1f);
    }

    /// <summary>
    /// Swipe input in screen pixels, using the same turn speed as the mouse.
    /// </summary>
    public void AddTouchLook(Vector2 pixelDelta)
    {
        pendingLook += pixelDelta;
    }

    void Awake()
    {
        body = GetComponent<CharacterController>();

        if (eyeCamera == null && Camera.main != null)
        {
            eyeCamera = Camera.main.transform;
        }
    }

    void OnEnable()
    {
        // The pointer stays free so the on-screen controls can be used.
        // A click on the room itself still locks the mouse for desktop look.
    }

    void OnDisable()
    {
        touchMove = Vector2.zero;
        pendingLook = Vector2.zero;
        UnlockCursor();
    }

    void Update()
    {
        UpdateCursor();
        Look();
        Move();
    }

    void UpdateCursor()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            UnlockCursor();
        }

        // A click on Map, Dictionary, or the study prompt must stay a button click.
        // Locking the mouse on that same click moves the pointer and the button never opens.
        bool fingerDown = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
        if (!fingerDown && mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            if (!IsPointerOverUi(mouse.position.ReadValue()))
            {
                LockCursor();
            }
        }
    }

    void Look()
    {
        Vector2 delta = pendingLook;
        pendingLook = Vector2.zero;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                delta += mouse.delta.ReadValue();
            }
        }

        if (delta.sqrMagnitude < 0.0001f)
        {
            return;
        }

        transform.Rotate(0f, delta.x * lookSensitivity, 0f);

        pitch -= delta.y * lookSensitivity;
        pitch = Mathf.Clamp(pitch, -maxLookDown, maxLookUp);

        if (eyeCamera != null)
        {
            eyeCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    void Move()
    {
        Keyboard keyboard = Keyboard.current;
        float inputX = touchMove.x;
        float inputZ = touchMove.y;

        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                inputX -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                inputX += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                inputZ -= 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                inputZ += 1f;
            }
        }

        Vector3 direction = (transform.right * inputX) + (transform.forward * inputZ);
        if (direction.sqrMagnitude > 1f)
        {
            direction.Normalize();
        }

        // A small downward speed keeps the controller pressed onto the floor.
        if (body.isGrounded && verticalSpeed < 0f)
        {
            verticalSpeed = -2f;
        }
        else
        {
            verticalSpeed -= gravity * Time.deltaTime;
        }

        Vector3 velocity = direction * walkSpeed;
        velocity.y = verticalSpeed;
        body.Move(velocity * Time.deltaTime);
    }

    bool IsPointerOverUi(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointer = new PointerEventData(EventSystem.current);
        pointer.position = screenPosition;
        uiHits.Clear();
        EventSystem.current.RaycastAll(pointer, uiHits);
        return uiHits.Count > 0;
    }

    static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
