using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { OnFoot, InCar }
    public GameState State { get; private set; } = GameState.OnFoot;

    [Header("References")]
    public FpsController fpsController;
    public CarController carController;
    public CameraManager cameraManager;
    public Transform exitPoint;

    private Rigidbody carRigidbody;
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
    }

    private void Update()
    {
        if (interactCooldown > 0f) interactCooldown -= Time.deltaTime;

        if (State != GameState.InCar) return;
        bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool yPressed = Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
        if ((ePressed || yPressed) && interactCooldown <= 0f)
            ExitCar();
    }

    public void EnterCar()
    {
        if (State == GameState.InCar || interactCooldown > 0f) return;
        interactCooldown = 1f;
        State = GameState.InCar;
        fpsController.enabled = false;
        carRigidbody.isKinematic = false;
        carController.enabled = true;
        fpsController.transform.SetParent(carController.transform);
        fpsController.transform.localPosition = Vector3.zero;
        cameraManager.SwitchToTPS();
    }

    public void ExitCar()
    {
        if (State == GameState.OnFoot) return;
        interactCooldown = 1f;
        State = GameState.OnFoot;
        carController.enabled = false;
        carRigidbody.linearVelocity = Vector3.zero;
        carRigidbody.angularVelocity = Vector3.zero;
        carRigidbody.isKinematic = true;
        fpsController.transform.SetParent(null);
        fpsController.transform.position = exitPoint.position;
        fpsController.transform.rotation = Quaternion.Euler(0f, carController.transform.eulerAngles.y, 0f);
        cameraManager.SwitchToFPS();
        fpsController.enabled = true;
    }
}
