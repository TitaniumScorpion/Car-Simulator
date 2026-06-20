using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool StartInCar = false;

    public enum GameState  { OnFoot, InCar }
    public enum EngineState { Off, Starting, Running }

    public GameState   State               { get; private set; } = GameState.OnFoot;
    public EngineState CurrentEngineState  { get; private set; } = EngineState.Off;
    public bool        IsEngineRunning     => CurrentEngineState == EngineState.Running;

    private const float EngineStartDuration = 4f;
    private float engineStartTimer;

    [Header("References")]
    public FpsController fpsController;
    public CarController carController;
    public CameraManager cameraManager;
    public Transform exitPoint;

    [Header("Settings")]
    public bool canExitCar = true;

    private Rigidbody carRigidbody;
    private CharacterController playerCollider;
    private Renderer[] playerRenderers;
    private float interactCooldown;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        carRigidbody = carController.GetComponent<Rigidbody>();
        carRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        carRigidbody.isKinematic = true;
        carController.enabled = false;
        playerCollider    = fpsController.GetComponent<CharacterController>();
        playerRenderers   = fpsController.GetComponentsInChildren<Renderer>();

        if (StartInCar)
        {
            StartInCar = false;
            if (NpcDialogue.Instance != null) NpcDialogue.Instance.CloseDialog();
            EnterCar();
        }
    }

    private void Update()
    {
        if (interactCooldown > 0f) interactCooldown -= Time.deltaTime;

        if (State != GameState.InCar) return;

        bool ePressed = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                     || (Gamepad.current  != null && Gamepad.current.buttonNorth.wasPressedThisFrame);

        switch (CurrentEngineState)
        {
            case EngineState.Off:
                if (ePressed && interactCooldown <= 0f)
                    BeginEngineStart();
                break;

            case EngineState.Starting:
                engineStartTimer += Time.deltaTime;
                if (engineStartTimer >= EngineStartDuration)
                    CurrentEngineState = EngineState.Running;
                break;

            case EngineState.Running:
                if (ePressed && interactCooldown <= 0f && canExitCar && !ParkingZone.IsReadyToPark)
                    ExitCar();
                break;
        }
    }

    private void BeginEngineStart()
    {
        CurrentEngineState = EngineState.Starting;
        engineStartTimer   = 0f;
    }

    private void OnGUI()
    {
        if (State != GameState.InCar) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize  = 28;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;

        Rect rect = new Rect(Screen.width / 2f - 220, Screen.height * 0.75f, 440, 50);

        if (CurrentEngineState == EngineState.Off && interactCooldown <= 0f)
        {
            GUI.Label(rect, "[E]  Start Engine", style);
        }
        else if (CurrentEngineState == EngineState.Starting)
        {
            string dots = new string('.', Mathf.FloorToInt(Time.time * 2f) % 4);
            style.normal.textColor = new Color(1f, 0.85f, 0.3f);
            GUI.Label(rect, $"Starting engine{dots}", style);
        }
    }

    public void EnterCar()
    {
        if (State == GameState.InCar || interactCooldown > 0f) return;
        if (NpcDialogue.Instance != null && !NpcDialogue.Instance.CarUnlocked) return;
        interactCooldown   = 3f;
        State              = GameState.InCar;
        CurrentEngineState = EngineState.Off;
        engineStartTimer   = 0f;
        fpsController.enabled = false;
        if (playerCollider != null) playerCollider.enabled = false;
        SetPlayerVisible(false);
        carRigidbody.isKinematic = false;
        carController.enabled = true;
        fpsController.transform.SetParent(carController.transform);
        fpsController.transform.localPosition = Vector3.zero;
        fpsController.transform.localRotation = Quaternion.identity;
        cameraManager.SwitchToTPS();
    }

    public void ExitCar()
    {
        if (State == GameState.OnFoot) return;
        interactCooldown   = 1f;
        State              = GameState.OnFoot;
        CurrentEngineState = EngineState.Off;
        carController.enabled = false;
        carRigidbody.linearVelocity = Vector3.zero;
        carRigidbody.angularVelocity = Vector3.zero;
        carRigidbody.isKinematic = true;
        fpsController.transform.SetParent(null);
        fpsController.transform.position = exitPoint.position;
        fpsController.transform.rotation = Quaternion.Euler(0f, carController.transform.eulerAngles.y, 0f);
        cameraManager.SwitchToFPS();
        SetPlayerVisible(true);
        if (playerCollider != null) playerCollider.enabled = true;
        fpsController.enabled = true;
    }

    private void SetPlayerVisible(bool visible)
    {
        if (playerRenderers == null) return;
        foreach (var r in playerRenderers) if (r != null) r.enabled = visible;
    }
}
