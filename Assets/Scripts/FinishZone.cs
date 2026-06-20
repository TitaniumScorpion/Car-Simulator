using UnityEngine;

public class FinishZone : MonoBehaviour
{
    [SerializeField] private bool showWaypoint = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<CarController>() != null)
            ScoreManager.Instance.TriggerWin();
    }

    private void OnGUI()
    {
        if (!showWaypoint) return;
        if (GameManager.Instance == null || GameManager.Instance.State != GameManager.GameState.InCar) return;
        if (ScoreManager.Instance == null || ScoreManager.Instance.IsGameEnded) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(transform.position);
        sp.y = Screen.height - sp.y;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize  = 22;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = new Color(0.2f, 0.8f, 1f);

        bool onScreen = sp.z > 0 && sp.x > 50 && sp.x < Screen.width - 50
                                  && sp.y > 50 && sp.y < Screen.height - 50;
        if (onScreen)
        {
            GUI.Label(new Rect(sp.x - 100, sp.y - 22, 200, 44), "[DESTINATION]", style);
        }
        else
        {
            Vector2 dir = new Vector2(sp.x - Screen.width / 2f, sp.y - Screen.height / 2f);
            if (sp.z < 0) dir = -dir;
            dir.Normalize();
            const float margin = 70f;
            float ex = Mathf.Clamp(Screen.width  / 2f + dir.x * (Screen.width  / 2f - margin), margin, Screen.width  - margin);
            float ey = Mathf.Clamp(Screen.height / 2f + dir.y * (Screen.height / 2f - margin), margin, Screen.height - margin);
            float lx = Mathf.Clamp(ex - 100f, 10f, Screen.width - 210f);
            float ly = Mathf.Clamp(ey - 22f, 10f, Screen.height - 54f);
            GUI.Label(new Rect(lx, ly, 200, 44), "► DESTINATION", style);
        }
    }
}
