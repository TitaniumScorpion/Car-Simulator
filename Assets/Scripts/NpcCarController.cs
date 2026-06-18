using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class NpcCarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheelCollider;
    public WheelCollider frontRightWheelCollider;
    public WheelCollider rearLeftWheelCollider;
    public WheelCollider rearRightWheelCollider;

    [Header("Wheel Transforms (Visuals)")]
    public Transform frontLeftWheelTransform;
    public Transform frontRightWheelTransform;
    public Transform rearLeftWheelTransform;
    public Transform rearRightWheelTransform;

    [Header("Car Settings")]
    public float motorForce = 800f;
    public float brakeForce = 2000f;
    public float maxSteeringAngle = 30f;
    public float targetSpeed = 8f;

    [Header("AI Settings")]
    public float detectionRange = 15f;
    public float stopDistance = 3f;
    public float playerDetectRange = 10f;
    [SerializeField] private LayerMask detectionMask = ~0;

    private NavMeshAgent agent;
    private Rigidbody rb;
    private Transform playerCar;
    private bool forceStopped;
    private float currentSteerAngle;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.speed = targetSpeed;
    }

    private void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerCar = player.transform;
            Collider[] npcColliders = GetComponentsInChildren<Collider>();
            Collider[] playerColliders = player.GetComponentsInChildren<Collider>();
            foreach (var nc in npcColliders)
                foreach (var pc in playerColliders)
                    Physics.IgnoreCollision(nc, pc);
        }
    }

    private void Update()
    {
        UpdateWheelVisuals();
    }

    private void FixedUpdate()
    {
        agent.nextPosition = transform.position;
        agent.speed = targetSpeed;

        if (forceStopped)
        {
            rearLeftWheelCollider.motorTorque = 0f;
            rearRightWheelCollider.motorTorque = 0f;
            ApplyBrake(brakeForce);
            return;
        }

        HandleSteering();
        HandleMotor();
    }

    private void HandleSteering()
    {
        Vector3 localTarget = transform.InverseTransformPoint(agent.steeringTarget);
        float targetSteerAngle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
        targetSteerAngle = Mathf.Clamp(targetSteerAngle, -maxSteeringAngle, maxSteeringAngle);
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetSteerAngle, Time.fixedDeltaTime * 5f);

        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;
    }

    private void HandleMotor()
    {
        float desiredSpeed = CalculateDesiredSpeed();
        float currentSpeed = rb.linearVelocity.magnitude;
        float speedDiff = desiredSpeed - currentSpeed;

        if (speedDiff > 0.1f)
        {
            rearLeftWheelCollider.motorTorque = motorForce;
            rearRightWheelCollider.motorTorque = motorForce;
            ApplyBrake(0f);
        }
        else
        {
            rearLeftWheelCollider.motorTorque = 0f;
            rearRightWheelCollider.motorTorque = 0f;
            float brakePower = Mathf.Clamp01(-speedDiff / targetSpeed) * brakeForce;
            ApplyBrake(brakePower);
        }
    }

    private float CalculateDesiredSpeed()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(rayOrigin, transform.forward, out RaycastHit hit, detectionRange, detectionMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.distance <= stopDistance)
                return 0f;
            return targetSpeed * (hit.distance / detectionRange);
        }

        if (playerCar != null)
        {
            Vector3 toPlayer = playerCar.position - transform.position;
            if (toPlayer.magnitude < playerDetectRange &&
                Vector3.Dot(transform.forward, toPlayer.normalized) > 0.5f)
                return targetSpeed * 0.7f;
        }

        return targetSpeed;
    }

    private void ApplyBrake(float force)
    {
        frontLeftWheelCollider.brakeTorque = force;
        frontRightWheelCollider.brakeTorque = force;
        rearLeftWheelCollider.brakeTorque = force;
        rearRightWheelCollider.brakeTorque = force;
    }

    private void UpdateWheelVisuals()
    {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
    }

    private void UpdateSingleWheel(WheelCollider col, Transform t)
    {
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        t.SetPositionAndRotation(pos, rot);
    }

    public void ForceStop(bool stop)
    {
        forceStopped = stop;
    }
}
