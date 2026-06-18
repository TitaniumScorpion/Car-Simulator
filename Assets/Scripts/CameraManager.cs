using System.Collections;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Header("References")]
    public Transform fpsHead;
    public Transform carTarget;

    [Header("TPS Settings")]
    public Vector3 tpsOffset = new Vector3(0f, 2f, -5f);
    public float tpsLookAhead = 12f;
    public float tpsLookHeightOffset = 0.5f;
    public float followSpeed = 8f;
    public float rotateSpeed = 8f;

    [Header("Transition")]
    public float transitionDuration = 0.7f;

    private bool isTPS;
    private bool transitioning;

    private void Start()
    {
        transform.position = fpsHead.position;
        transform.rotation = fpsHead.rotation;
    }

    private void LateUpdate()
    {
        if (transitioning) return;

        if (isTPS)
        {
            Vector3 targetPos = carTarget.position + carTarget.rotation * tpsOffset;
            Vector3 lookAt = carTarget.position + carTarget.forward * tpsLookAhead + Vector3.up * tpsLookHeightOffset;
            Quaternion targetRot = Quaternion.LookRotation(lookAt - targetPos, Vector3.up);
            transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = fpsHead.position;
            transform.rotation = fpsHead.rotation;
        }
    }

    public void SwitchToTPS()
    {
        StopAllCoroutines();
        StartCoroutine(TransitionTo(true));
    }

    public void SwitchToFPS()
    {
        StopAllCoroutines();
        StartCoroutine(TransitionTo(false));
    }

    private IEnumerator TransitionTo(bool toTPS)
    {
        transitioning = true;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

            Vector3 tpsPos = carTarget.position + carTarget.rotation * tpsOffset;
            Vector3 lookAt = carTarget.position + carTarget.forward * tpsLookAhead + Vector3.up * tpsLookHeightOffset;
            Vector3 endPos = toTPS ? tpsPos : fpsHead.position;
            Quaternion endRot = toTPS
                ? Quaternion.LookRotation(lookAt - tpsPos, Vector3.up)
                : fpsHead.rotation;

            transform.position = Vector3.Lerp(startPos, endPos, t);
            transform.rotation = Quaternion.Lerp(startRot, endRot, t);
            yield return null;
        }

        isTPS = toTPS;
        transitioning = false;
    }
}
