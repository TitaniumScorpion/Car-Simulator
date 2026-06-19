using UnityEngine;

public class DashboardUI : MonoBehaviour
{
    [SerializeField] private CarController car;

    private void Start()
    {
        if (car == null && GameManager.Instance != null)
            car = GameManager.Instance.carController;
    }

    private void OnGUI()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameManager.GameState.InCar) return;
        if (car == null) return;

        const float panelW = 180f;
        const float panelH = 110f;
        const float margin = 20f;
        float x = Screen.width  - panelW - margin;
        float y = Screen.height - panelH - margin;

        if (!car.IsAutomatic)
            DrawShiftHint(x, y, panelW);

        // Background
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(x, y, panelW, panelH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Transmission mode label
        GUIStyle modeStyle = new GUIStyle(GUI.skin.label);
        modeStyle.fontSize = 14;
        modeStyle.fontStyle = FontStyle.Bold;
        modeStyle.alignment = TextAnchor.UpperCenter;
        modeStyle.normal.textColor = car.IsAutomatic
            ? new Color(0.4f, 0.9f, 1f)
            : new Color(1f, 0.75f, 0.2f);
        GUI.Label(new Rect(x, y + 8f, panelW, 22f), car.IsAutomatic ? "AUTO" : "MANUAL", modeStyle);

        // Gear
        GUIStyle bigStyle = new GUIStyle(GUI.skin.label);
        bigStyle.fontSize = 38;
        bigStyle.fontStyle = FontStyle.Bold;
        bigStyle.normal.textColor = Color.white;

        bigStyle.alignment = TextAnchor.MiddleLeft;
        string gearLabel = car.CurrentGear == -1 ? "R" : car.CurrentGear.ToString();
        GUI.Label(new Rect(x + 14f, y + 28f, 60f, 50f), gearLabel, bigStyle);

        // Speed value
        bigStyle.alignment = TextAnchor.MiddleRight;
        GUI.Label(new Rect(x + 60f, y + 28f, 106f, 50f), Mathf.RoundToInt(car.SpeedKmh).ToString(), bigStyle);

        // km/h unit
        GUIStyle unitStyle = new GUIStyle(GUI.skin.label);
        unitStyle.fontSize = 14;
        unitStyle.alignment = TextAnchor.LowerRight;
        unitStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
        GUI.Label(new Rect(x, y + 76f, panelW - 10f, 26f), "km/h", unitStyle);
    }

    private void DrawShiftHint(float panelX, float panelY, float panelW)
    {
        string hint = GetShiftHint();
        if (hint == null) return;

        bool isShiftUp = hint.StartsWith("▲");
        Color hintColor = isShiftUp
            ? new Color(1f, 0.85f, 0.15f)   // amber  — shift up
            : new Color(1f, 0.35f, 0.2f);   // red-orange — shift down

        float pulse = Mathf.Lerp(0.45f, 1f, Mathf.PingPong(Time.time * 2.5f, 1f));
        const float hintH = 34f;
        float hintY = panelY - hintH - 6f;

        GUI.color = new Color(0f, 0f, 0f, 0.55f * pulse);
        GUI.DrawTexture(new Rect(panelX, hintY, panelW, hintH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 17;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = new Color(hintColor.r, hintColor.g, hintColor.b, pulse);
        GUI.Label(new Rect(panelX, hintY, panelW, hintH), hint, style);
    }

    private string GetShiftHint()
    {
        int gear = car.CurrentGear;
        if (gear < 1) return null;

        float speed = car.SpeedKmh;
        float[] thresholds = car.gearUpSpeeds;

        // Shift up: speed has exceeded the threshold for the current gear
        if (gear <= thresholds.Length && speed >= thresholds[gear - 1])
            return "▲  SHIFT UP  [Q]";

        // Shift down: speed is below 75 % of the previous gear's threshold
        if (gear > 1 && speed < thresholds[gear - 2] * 0.75f)
            return "▼  SHIFT DOWN  [Z]";

        return null;
    }
}
