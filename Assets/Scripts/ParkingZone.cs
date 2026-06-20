using UnityEngine;
using UnityEngine.InputSystem;

public class ParkingZone : MonoBehaviour
{
    public static bool IsReadyToPark { get; private set; }

    [Header("Parking Requirements")]
    [SerializeField] private float maxParkSpeedKmh = 2f;
    [SerializeField] private float maxAlignAngle   = 35f;

    [Header("Visual")]
    [SerializeField] private Renderer parkingVisual;
    [SerializeField] private Color idleColor  = new Color(1f, 0.85f, 0f);
    [SerializeField] private Color readyColor = new Color(0.2f, 1f, 0.3f);
    [SerializeField] private float blinkSpeed = 2f;

    private Material   visualMat;
    private BoxCollider zoneBox;
    private bool        carInside;
    private Rigidbody   carRb;
    private Transform   carTransform;
    private CarController carController;

    private void Awake()
    {
        zoneBox = GetComponent<BoxCollider>();
        if (parkingVisual != null)
            visualMat = parkingVisual.material;
    }

    private void OnTriggerEnter(Collider other)
    {
        var car = other.GetComponentInParent<CarController>();
        if (car == null) return;
        carInside     = true;
        carController = car;
        carTransform  = car.transform;
        carRb         = car.GetComponent<Rigidbody>();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<CarController>() == null) return;
        carInside     = false;
        IsReadyToPark = false;
    }

    private void Update()
    {
        UpdateVisual();

        if (ScoreManager.Instance == null || ScoreManager.Instance.IsGameEnded) return;
        if (GameManager.Instance  == null || GameManager.Instance.State != GameManager.GameState.InCar) return;

        IsReadyToPark = carInside && IsFullyInside() && CanPark();

        if (!IsReadyToPark) return;

        bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool yPressed = Gamepad.current  != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
        if (ePressed || yPressed)
            ScoreManager.Instance.TriggerWin();
    }

    // All four wheel positions must be inside the zone box (handles rotation correctly).
    private bool IsFullyInside()
    {
        if (carController == null || zoneBox == null) return false;

        WheelCollider[] wheels =
        {
            carController.frontLeftWheelCollider,
            carController.frontRightWheelCollider,
            carController.rearLeftWheelCollider,
            carController.rearRightWheelCollider
        };

        Vector3 half = zoneBox.size * 0.5f;

        foreach (var wheel in wheels)
        {
            if (wheel == null) continue;
            Vector3 local = transform.InverseTransformPoint(wheel.transform.position) - zoneBox.center;
            if (Mathf.Abs(local.x) > half.x || Mathf.Abs(local.z) > half.z)
                return false;
        }
        return true;
    }

    private bool CanPark()
    {
        if (carRb == null) return false;
        bool stopped  = carRb.linearVelocity.magnitude * 3.6f <= maxParkSpeedKmh;
        float angle   = Vector3.Angle(carTransform.forward, transform.right);
        bool aligned  = angle <= maxAlignAngle || angle >= 180f - maxAlignAngle;
        return stopped && aligned;
    }

    private void UpdateVisual()
    {
        if (parkingVisual == null || visualMat == null) return;
        float t = Mathf.PingPong(Time.time * blinkSpeed, 1f);
        parkingVisual.enabled = t > 0.5f;
        if (parkingVisual.enabled)
            visualMat.SetColor("_Color", IsReadyToPark ? readyColor : idleColor);
    }

    private void OnDisable()  => IsReadyToPark = false;
    private void OnDestroy()  => IsReadyToPark = false;

    private void OnGUI()
    {
        if (this == null) return;
        if (ScoreManager.Instance == null || ScoreManager.Instance.IsGameEnded) return;
        if (GameManager.Instance  == null || GameManager.Instance.State != GameManager.GameState.InCar) return;

        if (!carInside)
        {
            DrawWaypoint();
            return;
        }

        bool fullyInside = IsFullyInside();
        if (fullyInside && CanPark())
            DrawParkPrompt();
        else
            DrawAlignHint(fullyInside);
    }

    private void DrawWaypoint()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(transform.position);
        sp.y = Screen.height - sp.y;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize  = 22;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = IsReadyToPark ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.85f, 0.1f);

        bool onScreen = sp.z > 0 && sp.x > 50 && sp.x < Screen.width - 50
                                  && sp.y > 50 && sp.y < Screen.height - 50;
        if (onScreen)
        {
            GUI.Label(new Rect(sp.x - 70, sp.y - 22, 140, 44), "[PARK]", style);
        }
        else
        {
            Vector2 dir = new Vector2(sp.x - Screen.width / 2f, sp.y - Screen.height / 2f);
            if (sp.z < 0) dir = -dir;
            dir.Normalize();
            const float margin = 70f;
            float ex = Mathf.Clamp(Screen.width  / 2f + dir.x * (Screen.width  / 2f - margin), margin, Screen.width  - margin);
            float ey = Mathf.Clamp(Screen.height / 2f + dir.y * (Screen.height / 2f - margin), margin, Screen.height - margin);
            GUI.Label(new Rect(ex - 70, ey - 22, 140, 44), "► PARK", style);
        }
    }

    private void DrawParkPrompt()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize  = 28;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = new Color(0.2f, 1f, 0.3f);
        GUI.Label(new Rect(Screen.width / 2f - 200, Screen.height * 0.75f, 400, 50), "[E]  Park Here", style);
    }

    private void DrawAlignHint(bool fullyInside)
    {
        if (carRb == null) return;
        string hint;
        if (!fullyInside)
            hint = "Pull fully into the parking spot";
        else if (carRb.linearVelocity.magnitude * 3.6f > maxParkSpeedKmh)
            hint = "Slow down to park";
        else
            hint = "Align with the parking spot";

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize  = 22;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = new Color(1f, 0.85f, 0.3f);
        GUI.Label(new Rect(Screen.width / 2f - 220, Screen.height * 0.75f, 440, 44), hint, style);
    }
}
