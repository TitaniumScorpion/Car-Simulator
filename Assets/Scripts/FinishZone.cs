using UnityEngine;

public class FinishZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.CompareTag("PlayerCar"))
            ScoreManager.Instance.TriggerWin();
    }
}
