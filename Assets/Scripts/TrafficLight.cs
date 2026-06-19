using UnityEngine;

public enum LightState { Green, Yellow, Red }

public class TrafficLight : MonoBehaviour
{
    [Header("Timings (seconds)")]
    public float greenDuration = 5f;
    public float yellowDuration = 2f;
    public float redDuration = 5f;

    [Header("Start")]
    public LightState startState = LightState.Green;
    [Tooltip("Seconds already elapsed at game start. Use to stagger multiple lights.")]
    public float startTimeOffset = 0f;

    [Header("Light Visuals")]
    public GameObject redLightObject;
    public GameObject yellowLightObject;
    public GameObject greenLightObject;

    public LightState CurrentState { get; private set; }
    private float timer;

    private void Start()
    {
        CurrentState = startState;
        timer = Mathf.Max(0f, GetDuration(startState) - startTimeOffset);
        ApplyVisuals();
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            CurrentState = NextState(CurrentState);
            timer = GetDuration(CurrentState);
            ApplyVisuals();
        }
    }

    private LightState NextState(LightState state)
    {
        switch (state)
        {
            case LightState.Green:  return LightState.Yellow;
            case LightState.Yellow: return LightState.Red;
            default:                return LightState.Green;
        }
    }

    private float GetDuration(LightState state)
    {
        switch (state)
        {
            case LightState.Green:  return greenDuration;
            case LightState.Yellow: return yellowDuration;
            default:                return redDuration;
        }
    }

    private void ApplyVisuals()
    {
        if (redLightObject)    redLightObject.SetActive(CurrentState == LightState.Red);
        if (yellowLightObject) yellowLightObject.SetActive(CurrentState == LightState.Yellow);
        if (greenLightObject)  greenLightObject.SetActive(CurrentState == LightState.Green);
    }
}
