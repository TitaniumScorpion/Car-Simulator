using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FpsController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.8f;
    public float jumpHeight = 1.2f;
    public float gravity = -9.81f;

    [Header("Look")]
    public float mouseSensitivity = 0.15f;
    public Transform fpsHead;

    [Header("Interaction")]
    public float interactRange = 3f;

    private CharacterController cc;
    private float xRotation;
    private Vector3 velocity;
    private CarController nearbyVehicle;
    private NpcDialogue nearbyNpc;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        velocity.y = 0f;
    }

    private void Update()
    {
        HandleLook();
        HandleMovement();
        DetectInteractable();
        HandleInteract();
    }

    private void HandleLook()
    {
        if (NpcDialogue.Instance != null && NpcDialogue.Instance.IsDialogOpen) return;
        if (Mouse.current == null) return;
        Vector2 look = Mouse.current.delta.ReadValue();
        xRotation -= look.y * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        fpsHead.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * look.x * mouseSensitivity);
    }

    private void HandleMovement()
    {
        if (NpcDialogue.Instance != null && NpcDialogue.Instance.IsDialogOpen) return;
        if (cc.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        Vector2 moveInput = Vector2.zero;
        bool sprinting = false;
        bool jump = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveInput.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveInput.y -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveInput.x += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveInput.x -= 1f;
            sprinting = Keyboard.current.leftShiftKey.isPressed;
            jump = Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.01f) moveInput = stick;
            if (Gamepad.current.leftStickButton.isPressed) sprinting = true;
            if (Gamepad.current.buttonSouth.wasPressedThisFrame) jump = true;
        }

        if (jump && cc.isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        float speed = moveSpeed * (sprinting ? sprintMultiplier : 1f);
        Vector3 moveDir = transform.right * moveInput.x + transform.forward * moveInput.y;
        cc.Move(moveDir * speed * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }

    private void DetectInteractable()
    {
        nearbyVehicle = null;
        nearbyNpc = null;
        if (!Physics.Raycast(fpsHead.position, fpsHead.forward, out RaycastHit hit, interactRange)) return;

        var car = hit.collider.GetComponentInParent<CarController>();
        if (car != null) { nearbyVehicle = car; return; }

        var npc = hit.collider.GetComponentInParent<NpcDialogue>();
        if (npc != null) nearbyNpc = npc;
    }

    private void HandleInteract()
    {
        bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool yPressed = Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;

        if (NpcDialogue.Instance != null && NpcDialogue.Instance.IsDialogOpen)
        {
            if (ePressed || yPressed) NpcDialogue.Instance.CloseDialog();
            return;
        }

        bool carUnlocked = NpcDialogue.Instance == null || NpcDialogue.Instance.CarUnlocked;

        if ((ePressed || yPressed) && nearbyNpc != null && !carUnlocked)
        {
            nearbyNpc.OpenDialog();
            return;
        }

        if ((ePressed || yPressed) && nearbyVehicle != null && carUnlocked)
            GameManager.Instance.EnterCar();
    }

    private void OnGUI()
    {
        if (NpcDialogue.Instance != null && NpcDialogue.Instance.IsDialogOpen) return;

        bool carUnlocked = NpcDialogue.Instance == null || NpcDialogue.Instance.CarUnlocked;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 28;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;

        if (nearbyNpc != null && !carUnlocked)
        {
            GUI.Label(new Rect(Screen.width / 2f - 160, Screen.height * 0.75f, 320, 50), "[E] Talk", style);
            return;
        }

        if (nearbyVehicle == null || !carUnlocked) return;
        GUI.Label(new Rect(Screen.width / 2f - 160, Screen.height * 0.75f, 320, 50), "[E] Enter Car", style);
    }
}
