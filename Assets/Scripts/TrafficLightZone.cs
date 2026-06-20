using UnityEngine;

public class TrafficLightZone : MonoBehaviour
{
    [SerializeField] private TrafficLight trafficLight;

    private const float TriggerCooldown = 3f;
    private float cooldownTimer;

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (cooldownTimer > 0f) return;
        if (!other.transform.root.CompareTag("PlayerCar")) return;

        if (trafficLight.CurrentState == LightState.Red)
        {
            ScoreManager.Instance.AddPenalty(20, "Ran a red light!");
            cooldownTimer = TriggerCooldown;
        }
        else if (trafficLight.CurrentState == LightState.Yellow ||
                 trafficLight.CurrentState == LightState.YellowToGreen)
        {
            ScoreManager.Instance.AddPenalty(15, "Ignored yellow light!");
            cooldownTimer = TriggerCooldown;
        }
    }
}
