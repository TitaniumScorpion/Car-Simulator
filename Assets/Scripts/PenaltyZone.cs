using UnityEngine;

public class PenaltyZone : MonoBehaviour
{
    [SerializeField] private bool instantLose = false;
    [SerializeField] private int penaltyPoints = 20;
    [SerializeField] private string message = "Off road!";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.transform.root.CompareTag("PlayerCar")) return;
        ApplyPenalty();
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!other.transform.root.CompareTag("PlayerCar")) return;
        ApplyPenalty();
    }

    private void ApplyPenalty()
    {
        if (instantLose)
            ScoreManager.Instance.TriggerLose(message);
        else
            ScoreManager.Instance.AddPenalty(penaltyPoints, message);
    }
}
