using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        Time.timeScale   = 1f;
    }

    private void OnGUI()
    {
        float sw = Screen.width;
        float sh = Screen.height;
        float cx = sw / 2f;
        float cy = sh / 2f;

        // Background
        GUI.color = new Color(0.06f, 0.06f, 0.1f, 1f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Title
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize  = 52;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(cx - 450, cy - 210, 900, 100), "FIRST DRIVE SIMULATOR", titleStyle);

        // Subtitle
        GUIStyle subStyle = new GUIStyle(GUI.skin.label);
        subStyle.fontSize  = 22;
        subStyle.alignment = TextAnchor.MiddleCenter;
        subStyle.normal.textColor = new Color(0.65f, 0.65f, 0.65f);
        GUI.Label(new Rect(cx - 450, cy - 110, 900, 40), "Serious Driving Simulator", subStyle);

        // Divider
        GUI.color = new Color(1f, 1f, 1f, 0.12f);
        GUI.DrawTexture(new Rect(cx - 130, cy - 55, 260, 2), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Buttons
        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize  = 26;
        btnStyle.fontStyle = FontStyle.Bold;

        if (GUI.Button(new Rect(cx - 130, cy - 40, 260, 60), "Start Game", btnStyle))
            SceneManager.LoadScene(1);

        if (GUI.Button(new Rect(cx - 130, cy + 35, 260, 60), "Quit", btnStyle))
            Application.Quit();
    }
}
