using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
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
    public float motorForce = 1500f;
    public float brakeForce = 3000f;
    public float maxSteeringAngle = 30f;

    [Header("Transmission")]
    public bool isAutomatic = true;
    [Tooltip("Speed thresholds in km/h to shift up: 1->2, 2->3, 3->4, 4->5")]
    public float[] gearUpSpeeds = { 20f, 45f, 75f, 110f };
    [Tooltip("Torque multiplier per forward gear (index 0 = gear 1)")]
    public float[] gearTorqueMultipliers = { 2.0f, 1.5f, 1.0f, 0.75f, 0.5f };

    private float horizontalInput;
    private float verticalInput;
    private bool isBraking;
    private float currentSteeringAngle;
    private float currentBrakeForce;
    private int currentGear = 1;
    private Rigidbody rb;

    public float SpeedKmh => rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;
    public int CurrentGear => currentGear;
    public bool IsAutomatic => isAutomatic;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        GetInput();
        UpdateWheelVisuals();
    }

    private void FixedUpdate()
    {
        if (isAutomatic) AutoShift();
        HandleMotor();
        HandleSteering();
    }

    private void GetInput()
    {
        horizontalInput = 0f;
        verticalInput = 0f;
        isBraking = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)    verticalInput  += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)  verticalInput  -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  horizontalInput -= 1f;

            isBraking = Keyboard.current.spaceKey.isPressed;

            if (Keyboard.current.tKey.wasPressedThisFrame)
                ToggleTransmission();

            if (!isAutomatic)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame)
                    ShiftUp();
                if (Keyboard.current.zKey.wasPressedThisFrame)
                    ShiftDown();
            }
        }

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.01f)
            {
                horizontalInput = stick.x;
                verticalInput = stick.y;
            }

            if (Gamepad.current.buttonSouth.isPressed || Gamepad.current.leftTrigger.isPressed)
                isBraking = true;

            if (Gamepad.current.selectButton.wasPressedThisFrame)
                ToggleTransmission();

            if (!isAutomatic)
            {
                if (Gamepad.current.rightShoulder.wasPressedThisFrame)
                    ShiftUp();
                if (Gamepad.current.leftShoulder.wasPressedThisFrame)
                    ShiftDown();
            }
        }
    }

    private void ToggleTransmission()
    {
        isAutomatic = !isAutomatic;
        if (isAutomatic) currentGear = 1;
    }

    private void ShiftUp()
    {
        if (currentGear == -1) currentGear = 1;
        else currentGear = Mathf.Min(currentGear + 1, gearTorqueMultipliers.Length);
    }

    private void ShiftDown()
    {
        if (currentGear == 1) currentGear = -1;
        else if (currentGear > 1) currentGear--;
    }

    private void AutoShift()
    {
        bool movingBackward = Vector3.Dot(rb.linearVelocity, transform.forward) < -0.5f;
        bool inputtingBackward = verticalInput < -0.1f;

        if (movingBackward || (inputtingBackward && SpeedKmh < 3f))
        {
            currentGear = -1;
            return;
        }

        if (currentGear == -1 && !movingBackward)
            currentGear = 1;

        int newGear = 1;
        for (int i = 0; i < gearUpSpeeds.Length; i++)
        {
            if (SpeedKmh >= gearUpSpeeds[i]) newGear = i + 2;
        }
        currentGear = Mathf.Clamp(newGear, 1, gearTorqueMultipliers.Length);
    }

    private void HandleMotor()
    {
        float motorTorque;

        if (currentGear == -1)
        {
            // Reverse: only S-key input (negative) drives the car; W is ignored
            float input = Mathf.Min(verticalInput, 0f);
            motorTorque = input * motorForce * 1.5f;
        }
        else
        {
            // Forward gears: only W-key input (positive) drives the car; S is ignored
            float input = Mathf.Max(verticalInput, 0f);
            float multiplier = gearTorqueMultipliers[Mathf.Clamp(currentGear - 1, 0, gearTorqueMultipliers.Length - 1)];
            motorTorque = input * motorForce * multiplier;
        }

        frontLeftWheelCollider.motorTorque = motorTorque;
        frontRightWheelCollider.motorTorque = motorTorque;

        currentBrakeForce = isBraking ? brakeForce : 0f;
        ApplyBraking();
    }

    private void ApplyBraking()
    {
        frontLeftWheelCollider.brakeTorque = currentBrakeForce;
        frontRightWheelCollider.brakeTorque = currentBrakeForce;
        rearLeftWheelCollider.brakeTorque = currentBrakeForce;
        rearRightWheelCollider.brakeTorque = currentBrakeForce;
    }

    private void HandleSteering()
    {
        currentSteeringAngle = maxSteeringAngle * horizontalInput;
        frontLeftWheelCollider.steerAngle = currentSteeringAngle;
        frontRightWheelCollider.steerAngle = currentSteeringAngle;
    }

    private void UpdateWheelVisuals()
    {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot * Quaternion.Euler(0f, 0f, 90f);
    }
}
