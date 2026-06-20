using UnityEngine;

[RequireComponent(typeof(CarController))]
public class CarAudio : MonoBehaviour
{
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
    private AudioSource brakeSource;
    private AudioSource impactSource;
    private float lastImpactTime = -10f;

    private void Awake()
    {
        car = GetComponent<CarController>();

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
        UpdateBrakeSound();
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
        if (Time.time - lastImpactTime < impactCooldown) return;

        float magnitude = collision.relativeVelocity.magnitude;
        if (magnitude < minImpactMagnitude) return;

        float t = Mathf.Clamp01((magnitude - minImpactMagnitude) / (maxImpactMagnitude - minImpactMagnitude));
        impactSource.PlayOneShot(impactClip, impactMaxVolume * t);
        lastImpactTime = Time.time;
    }
}
