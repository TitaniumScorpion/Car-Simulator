using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score")]
    public int startingScore = 100;
    public int passingScore = 50;

    private int currentScore;
    private bool gameEnded;
    private bool isWin;
    private string endReason;

    private string notificationText;
    private float notificationTimer;
    private const float NotificationDuration = 2.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        currentScore = startingScore;
    }

    public bool IsGameEnded => gameEnded;

    public void TriggerWin()
    {
        if (gameEnded) return;
        gameEnded = true;
        isWin = currentScore >= passingScore;
        endReason = isWin
            ? "Destination reached!"
            : "Reached destination, but score is too low.";
        EndGame();
    }

    public void TriggerLose(string reason)
    {
        if (gameEnded) return;
        isWin = false;
        gameEnded = true;
        endReason = reason;
        EndGame();
    }

    public void AddPenalty(int points, string reason)
    {
        if (gameEnded) return;
        currentScore = Mathf.Max(0, currentScore - points);
        notificationText = $"-{points}  {reason}";
        notificationTimer = NotificationDuration;
    }

    private void EndGame()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (notificationTimer > 0f)
            notificationTimer -= Time.deltaTime;

        if (!gameEnded && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            Restart();
    }

    private void OnGUI()
    {
        if (!gameEnded)
            DrawHUD();

        if (notificationTimer > 0f)
            DrawNotification();

        if (gameEnded)
            DrawEndScreen();
    }

    private void DrawHUD()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 24;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(20, 20, 200, 40), $"Score: {currentScore}", style);
    }

    private void DrawNotification()
    {
        float alpha = Mathf.Clamp01(notificationTimer / NotificationDuration);
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 26;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.wordWrap = false;
        style.normal.textColor = Color.red;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.Label(new Rect(Screen.width / 2f - 300, Screen.height * 0.2f, 600, 50), notificationText, style);
        GUI.color = Color.white;
    }

    private void DrawEndScreen()
    {
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 52;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = isWin ? new Color(0.2f, 1f, 0.2f) : new Color(1f, 0.3f, 0.3f);

        GUIStyle infoStyle = new GUIStyle(GUI.skin.label);
        infoStyle.fontSize = 24;
        infoStyle.alignment = TextAnchor.MiddleCenter;
        infoStyle.normal.textColor = Color.white;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 22;

        GUI.Label(new Rect(cx - 300, cy - 130, 600, 80), isWin ? "LEVEL COMPLETE" : "LEVEL FAILED", titleStyle);
        GUI.Label(new Rect(cx - 300, cy - 40, 600, 40), endReason, infoStyle);
        GUI.Label(new Rect(cx - 300, cy + 10, 600, 40), $"Final Score: {currentScore} / {startingScore}", infoStyle);

        int nextScene = SceneManager.GetActiveScene().buildIndex + 1;
        bool hasNextScene = nextScene < SceneManager.sceneCountInBuildSettings;

        if (isWin && hasNextScene)
        {
            if (GUI.Button(new Rect(cx - 230, cy + 70, 210, 55), "Next Level", buttonStyle))
                LoadNext(nextScene);
            if (GUI.Button(new Rect(cx + 20, cy + 70, 210, 55), "Restart Level", buttonStyle))
                Restart();
        }
        else if (isWin)
        {
            if (GUI.Button(new Rect(cx - 230, cy + 70, 210, 55), "Restart Level", buttonStyle))
                Restart();
            if (GUI.Button(new Rect(cx + 20, cy + 70, 210, 55), "Exit Game", buttonStyle))
                Application.Quit();
        }
        else
        {
            if (GUI.Button(new Rect(cx - 110, cy + 70, 220, 55), "Restart Level", buttonStyle))
                Restart();
        }
    }

    private void LoadNext(int index)
    {
        Time.timeScale = 1f;
        GameManager.StartInCar = true;
        SceneManager.LoadScene(index);
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        GameManager.StartInCar = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
