using UnityEngine;
using UnityEngine.InputSystem;

public class NpcDialogue : MonoBehaviour
{
    public static NpcDialogue Instance { get; private set; }

    [Header("NPC")]
    public string npcName = "Uncle";
    [TextArea(2, 4)]
    public string dialogueLine = "So you finally got your license, huh? Take my car for a spin — show me what you can do!";
    public float interactRange = 3f;

    public bool CarUnlocked { get; private set; } = false;
    public bool IsDialogOpen { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void OpenDialog()
    {
        IsDialogOpen = true;
    }

    public void CloseDialog()
    {
        IsDialogOpen = false;
        CarUnlocked = true;
    }

    private void OnGUI()
    {
        if (!IsDialogOpen) return;

        const float boxW = 520f;
        const float boxH = 160f;
        float boxX = Screen.width / 2f - boxW / 2f;
        float boxY = Screen.height * 0.62f;

        GUI.color = new Color(0f, 0f, 0f, 0.78f);
        GUI.DrawTexture(new Rect(boxX, boxY, boxW, boxH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
        nameStyle.fontSize = 18;
        nameStyle.fontStyle = FontStyle.Bold;
        nameStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
        GUI.Label(new Rect(boxX + 18f, boxY + 14f, boxW - 36f, 26f), npcName, nameStyle);

        GUIStyle lineStyle = new GUIStyle(GUI.skin.label);
        lineStyle.fontSize = 16;
        lineStyle.wordWrap = true;
        lineStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(boxX + 18f, boxY + 46f, boxW - 36f, 82f), dialogueLine, lineStyle);

        GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = 14;
        hintStyle.fontStyle = FontStyle.Bold;
        hintStyle.alignment = TextAnchor.MiddleRight;
        hintStyle.normal.textColor = new Color(0.65f, 0.65f, 0.65f);
        GUI.Label(new Rect(boxX, boxY + boxH - 30f, boxW - 14f, 26f), "[E] Continue", hintStyle);
    }
}
