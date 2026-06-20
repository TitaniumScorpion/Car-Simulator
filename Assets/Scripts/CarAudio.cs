using UnityEngine;

[RequireComponent(typeof(CarController))]
public class CarAudio : MonoBehaviour
{
    [Header("Engine")]
    public AudioClip engineStartClip;
    public AudioClip engineClip;
    [Range(0f, 1f)] public float engineVolume = 0.6f;
    [Tooltip("Pitch at idle / zero speed.")]
    public float enginePitchMin = 0.5f;
    [Tooltip("Pitch at top speed.")]
    public float enginePitchMax = 2.5f;
    [Tooltip("Speed in km/h mapped to max pitch.")]
    public float engineTopSpeed = 120f;

    [Header("Brake Squeal")]
    public AudioClip brakeSquealClip;
    [Range(0f, 1f)] public float brakeSquealMaxVolume = 1f;
    [Tooltip("Minimum speed in km/h before squeal plays.")]
    public float minSpeedForSqueal = 8f;

    [Header("Collision Impact")]
    public AudioClip impactClip;
    [Range(0f, 1f)] public float impactMaxVolume = 1f;
    [Tooltip("Minimum relative velocity (m/s) to trigger a sound.")]
    public float minImpactMagnitude = 1f;
    [Tooltip("Relative velocity at which impact plays at full volume.")]
    public float maxImpactMagnitude = 15f;
    [Tooltip("Minimum seconds between impact sounds to avoid spam.")]
    public float impactCooldown = 0.3f;

    private CarController car;
    private AudioSource engineStartSource;
    private AudioSource engineSource;
    private AudioSource brakeSource;
    private AudioSource impactSource;
    private float lastImpactTime = -10f;

    private void Awake()
    {
        car = GetComponent<CarController>();

        engineStartSource = gameObject.AddComponent<AudioSource>();
        engineStartSource.loop        = false;
        engineStartSource.playOnAwake = false;

        engineSource = gameObject.AddComponent<AudioSource>();
        engineSource.clip        = engineClip;
        engineSource.loop        = true;
        engineSource.playOnAwake = false;
        engineSource.volume      = 0f;
        engineSource.pitch       = enginePitchMin;

        brakeSource = gameObject.AddComponent<AudioSource>();
        brakeSource.clip = brakeSquealClip;
        brakeSource.loop = true;
        brakeSource.playOnAwake = false;
        brakeSource.volume = 0f;

        impactSource = gameObject.AddComponent<AudioSource>();
        impactSource.loop = false;
        impactSource.playOnAwake = false;
    }

    private void Update()
    {
        if (ScoreManager.Instance != null && ScoreManager.Instance.IsGameEnded)
        {
            if (engineStartSource.isPlaying) engineStartSource.Stop();
            if (engineSource.isPlaying)      engineSource.Stop();
            if (brakeSource.isPlaying)       brakeSource.Stop();
            return;
        }

        UpdateEngineSound();
        UpdateBrakeSound();
    }

    private void UpdateEngineSound()
    {
        if (GameManager.Instance == null) return;

        switch (GameManager.Instance.CurrentEngineState)
        {
            case GameManager.EngineState.Off:
                if (engineStartSource.isPlaying) engineStartSource.Stop();
                if (engineSource.isPlaying)      engineSource.Stop();
                engineSource.volume = 0f;
                break;

            case GameManager.EngineState.Starting:
                if (engineSource.isPlaying) engineSource.Stop();
                if (!engineStartSource.isPlaying && engineStartClip != null)
                {
                    engineStartSource.clip = engineStartClip;
                    engineStartSource.Play();
                }
                break;

            case GameManager.EngineState.Running:
                if (engineStartSource.isPlaying) engineStartSource.Stop();
                if (engineClip == null) break;
                if (!engineSource.isPlaying) engineSource.Play();
                engineSource.volume = Mathf.MoveTowards(engineSource.volume, engineVolume, Time.deltaTime * 3f);
                float speedRatio = Mathf.Clamp01(car.SpeedKmh / engineTopSpeed);
                engineSource.pitch = Mathf.Lerp(enginePitchMin, enginePitchMax, speedRatio);
                break;
        }
    }

    private void UpdateBrakeSound()
    {
        bool shouldSqueal = car.enabled && car.IsBraking && car.SpeedKmh > minSpeedForSqueal;
        float targetVolume = shouldSqueal ? brakeSquealMaxVolume : 0f;

        brakeSource.volume = Mathf.MoveTowards(brakeSource.volume, targetVolume, Time.deltaTime * 5f);

        if (shouldSqueal && !brakeSource.isPlaying && brakeSquealClip != null)
            brakeSource.Play();
        else if (brakeSource.volume <= 0f && brakeSource.isPlaying)
            brakeSource.Stop();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (impactClip == null) return;
        if (ScoreManager.Instance != null && ScoreManager.Instance.IsGameEnded) return;
        if (Time.time - lastImpactTime < impactCooldown) return;

        float magnitude = collision.relativeVelocity.magnitude;
        if (magnitude < minImpactMagnitude) return;

        float t = Mathf.Clamp01((magnitude - minImpactMagnitude) / (maxImpactMagnitude - minImpactMagnitude));
        impactSource.PlayOneShot(impactClip, impactMaxVolume * t);
        lastImpactTime = Time.time;
    }
}
