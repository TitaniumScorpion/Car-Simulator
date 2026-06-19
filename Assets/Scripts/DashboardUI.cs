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

        DrawShiftHint(x, y, panelW);
        DrawControlsLegend(x, y, panelW);
        DrawDashboard(x, y, panelW, panelH);
        DrawTurnSignals(x, y, panelW, panelH);
    }

    private void DrawDashboard(float x, float y, float panelW, float panelH)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(x, y, panelW, panelH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle bigStyle = new GUIStyle(GUI.skin.label);
        bigStyle.fontSize  = 38;
        bigStyle.fontStyle = FontStyle.Bold;
        bigStyle.normal.textColor = Color.white;

        // Gear
        bigStyle.alignment = TextAnchor.MiddleLeft;
        string gearLabel = car.CurrentGear == -1 ? "R" : car.CurrentGear.ToString();
        GUI.Label(new Rect(x + 14f, y + 28f, 60f, 50f), gearLabel, bigStyle);

        // Speed value
        bigStyle.alignment = TextAnchor.MiddleRight;
        GUI.Label(new Rect(x + 60f, y + 28f, 106f, 50f), Mathf.RoundToInt(car.SpeedKmh).ToString(), bigStyle);

        // km/h unit
        GUIStyle unitStyle = new GUIStyle(GUI.skin.label);
        unitStyle.fontSize  = 14;
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
            ? new Color(1f, 0.85f, 0.15f)
            : new Color(1f, 0.35f, 0.2f);

        float pulse  = Mathf.Lerp(0.45f, 1f, Mathf.PingPong(Time.time * 2.5f, 1f));
        const float hintH = 34f;
        float hintY = panelY - hintH - 6f;

        GUI.color = new Color(0f, 0f, 0f, 0.55f * pulse);
        GUI.DrawTexture(new Rect(panelX, hintY, panelW, hintH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize  = 17;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = new Color(hintColor.r, hintColor.g, hintColor.b, pulse);
        GUI.Label(new Rect(panelX, hintY, panelW, hintH), hint, style);
    }

    private string GetShiftHint()
    {
        int gear = car.CurrentGear;
        if (gear < 1) return null;

        float speed      = car.SpeedKmh;
        float[] thresholds = car.gearUpSpeeds;

        if (gear <= thresholds.Length && speed >= thresholds[gear - 1])
            return "▲  SHIFT UP  [Q]";

        if (gear > 1 && speed < thresholds[gear - 2] * 0.75f)
            return "▼  SHIFT DOWN  [Z]";

        return null;
    }

    private void DrawTurnSignals(float panelX, float panelY, float panelW, float panelH)
    {
        if (!car.BlinkState) return;

        GUIStyle arrowStyle = new GUIStyle(GUI.skin.label);
        arrowStyle.fontSize  = 20;
        arrowStyle.fontStyle = FontStyle.Bold;
        arrowStyle.normal.textColor = new Color(1f, 0.75f, 0f);

        const float arrowW = 36f;
        const float arrowH = 22f;
        float arrowY = panelY + 5f;

        if (car.LeftSignalOn)
        {
            arrowStyle.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(panelX + 6f, arrowY, arrowW, arrowH), "◄", arrowStyle);
        }

        if (car.RightSignalOn)
        {
            arrowStyle.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(panelX + panelW - arrowW - 6f, arrowY, arrowW, arrowH), "►", arrowStyle);
        }
    }

    private void DrawControlsLegend(float panelX, float panelY, float panelW)
    {
        string[] lines =
        {
            "W/S/A/D  -  Drive",
            "Space      -  Brake",
            "Q / Z       -  Shift ↑↓",
            "X / C       -  Signals",
            "F              -  Horn",
            "E              -  Exit Car",
        };

        const float lineH   = 20f;
        const float padV    = 10f;
        const float padH    = 10f;
        const float gap     = 8f;
        const float hintH   = 34f;

        float legendH = padV * 2f + lines.Length * lineH;
        float legendY = panelY - hintH - gap - legendH - 6f;

        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(panelX, legendY, panelW, legendH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize  = 12;
        labelStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

        for (int i = 0; i < lines.Length; i++)
            GUI.Label(new Rect(panelX + padH, legendY + padV + i * lineH, panelW - padH * 2f, lineH), lines[i], labelStyle);
    }
}
