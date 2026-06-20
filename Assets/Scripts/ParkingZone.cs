using UnityEngine;
using UnityEngine.InputSystem;

public class ParkingZone : MonoBehaviour
{
    public static bool IsReadyToPark { get; private set; }

    [Header("Parking Requirements")]
    [SerializeField] private float maxParkSpeedKmh = 2f;
    [SerializeField] private float maxAlignAngle = 35f;

    [Header("Visual")]
    [SerializeField] private Renderer parkingVisual;
    [SerializeField] private Color idleColor   = new Color(1f, 0.85f, 0f, 0.8f);
    [SerializeField] private Color insideColor = new Color(0.2f, 1f, 0.3f, 0.9f);
    [SerializeField] private float blinkSpeed  = 2f;

    private Material visualMat;
    private bool carInside;
    private Rigidbody carRb;
    private Transform carTransform;

    private void Awake()
    {
        if (parkingVisual != null)
            visualMat = parkingVisual.material;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.transform.root.CompareTag("PlayerCar")) return;
        carInside = true;
        carTransform = other.transform.root;
        carRb = carTransform.GetComponent<Rigidbody>();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.transform.root.CompareTag("PlayerCar")) return;
        carInside = false;
        IsReadyToPark = false;
    }

    private void Update()
    {
        UpdateVisual();

        if (ScoreManager.Instance == null || ScoreManager.Instance.IsGameEnded) return;
        if (GameManager.Instance == null || GameManager.Instance.State != GameManager.GameState.InCar) return;

        IsReadyToPark = carInside && CanPark();

        if (!IsReadyToPark) return;

        bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool yPressed = Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
        if (ePressed || yPressed)
            ScoreManager.Instance.TriggerWin();
    }

    private bool CanPark()
    {
        if (carRb == null) return false;
        bool stopped = carRb.linearVelocity.magnitude * 3.6f <= maxParkSpeedKmh;
        float angle = Vector3.Angle(carTransform.forward, transform.forward);
        bool aligned = angle <= maxAlignAngle || angle >= 180f - maxAlignAngle;
        return stopped && aligned;
    }

    private void UpdateVisual()
    {
        if (visualMat == null) return;
        float t = Mathf.PingPong(Time.time * blinkSpeed, 1f);
        Color baseColor = carInside ? insideColor : idleColor;
        Color dimColor  = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * 0.15f);
        visualMat.color = Color.Lerp(dimColor, baseColor, t);
    }

    private void OnDisable() => IsReadyToPark = false;

    private void OnGUI()
    {
        if (ScoreManager.Instance == null || ScoreManager.Instance.IsGameEnded) return;
        if (GameManager.Instance == null || GameManager.Instance.State != GameManager.GameState.InCar) return;

        if (!carInside)
        {
            DrawWaypoint();
            return;
        }

        if (CanPark())
            DrawParkPrompt();
        else
            DrawAlignHint();
    }

    private void DrawWaypoint()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(transform.position);
        sp.y = Screen.height - sp.y;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 22;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = new Color(1f, 0.85f, 0.1f);

        bool onScreen = sp.z > 0 && sp.x > 50 && sp.x < Screen.width - 50 && sp.y > 50 && sp.y < Screen.height - 50;

        if (onScreen)
        {
            GUI.Label(new Rect(sp.x - 70, sp.y - 22, 140, 44), "[PARK]", style);
        }
        else
        {
            // Clamp arrow to screen edge
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
        style.fontSize = 28;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = new Color(0.2f, 1f, 0.3f);
        GUI.Label(new Rect(Screen.width / 2f - 200, Screen.height * 0.75f, 400, 50), "[E]  Park Here", style);
    }

    private void DrawAlignHint()
    {
        if (carRb == null) return;
        bool stopped = carRb.linearVelocity.magnitude * 3.6f <= maxParkSpeedKmh;
        string hint = stopped ? "Align with the parking spot" : "Slow down to park";

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 22;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = new Color(1f, 0.85f, 0.3f);
        GUI.Label(new Rect(Screen.width / 2f - 220, Screen.height * 0.75f, 440, 44), hint, style);
    }
}
