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
    [Tooltip("Speed thresholds in km/h to shift up: 1->2, 2->3, 3->4, 4->5")]
    public float[] gearUpSpeeds = { 20f, 45f, 75f, 110f };
    [Tooltip("Torque multiplier per forward gear (index 0 = gear 1)")]
    public float[] gearTorqueMultipliers = { 2.0f, 1.5f, 1.0f, 0.75f, 0.5f };

    [Header("Turn Signal Lights")]
    public Light[] leftTurnLights;
    public Light[] rightTurnLights;

    [Header("Horn")]
    public AudioClip hornClip;
    public AudioSource hornSource;

    private float horizontalInput;
    private float verticalInput;
    private bool isBraking;
    private float currentSteeringAngle;
    private float currentBrakeForce;
    private int currentGear = 1;
    private Rigidbody rb;

    private bool leftSignalOn;
    private bool rightSignalOn;
    private float blinkTimer;
    private bool blinkState;

    public float SpeedKmh  => rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;
    public int   CurrentGear  => currentGear;
    public bool  LeftSignalOn  => leftSignalOn;
    public bool  RightSignalOn => rightSignalOn;
    public bool  BlinkState    => blinkState;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        SetLights(leftTurnLights,  false);
        SetLights(rightTurnLights, false);
    }

    private void Update()
    {
        GetInput();
        UpdateWheelVisuals();
        UpdateBlinkers();
    }

    private void FixedUpdate()
    {
        HandleMotor();
        HandleSteering();
    }

    private void GetInput()
    {
        horizontalInput = 0f;
        verticalInput   = 0f;
        isBraking       = false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)    verticalInput  += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)  verticalInput  -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)  horizontalInput -= 1f;

            isBraking = Keyboard.current.spaceKey.isPressed;

            if (Keyboard.current.qKey.wasPressedThisFrame) ShiftUp();
            if (Keyboard.current.zKey.wasPressedThisFrame) ShiftDown();

            if (Keyboard.current.xKey.wasPressedThisFrame) ToggleLeftSignal();
            if (Keyboard.current.cKey.wasPressedThisFrame) ToggleRightSignal();

            HandleHorn(Keyboard.current.fKey.isPressed);
        }

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.01f)
            {
                horizontalInput = stick.x;
                verticalInput   = stick.y;
            }

            if (Gamepad.current.buttonSouth.isPressed || Gamepad.current.leftTrigger.isPressed)
                isBraking = true;

            if (Gamepad.current.rightShoulder.wasPressedThisFrame) ShiftUp();
            if (Gamepad.current.leftShoulder.wasPressedThisFrame)  ShiftDown();
        }
    }

    private void ToggleLeftSignal()
    {
        leftSignalOn  = !leftSignalOn;
        rightSignalOn = false;
        if (!leftSignalOn) SetLights(leftTurnLights, false);
    }

    private void ToggleRightSignal()
    {
        rightSignalOn = !rightSignalOn;
        leftSignalOn  = false;
        if (!rightSignalOn) SetLights(rightTurnLights, false);
    }

    private void UpdateBlinkers()
    {
        if (leftSignalOn || rightSignalOn)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= 0.5f) { blinkState = !blinkState; blinkTimer = 0f; }
        }
        else
        {
            blinkState = false;
            blinkTimer = 0f;
        }

        SetLights(leftTurnLights,  leftSignalOn  && blinkState);
        SetLights(rightTurnLights, rightSignalOn && blinkState);
    }

    private void HandleHorn(bool pressed)
    {
        if (hornSource == null || hornClip == null) return;
        if (pressed && !hornSource.isPlaying)
            hornSource.PlayOneShot(hornClip);
        else if (!pressed && hornSource.isPlaying)
            hornSource.Stop();
    }

    private static void SetLights(Light[] lights, bool on)
    {
        if (lights == null) return;
        foreach (var l in lights) if (l != null) l.enabled = on;
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

    private void HandleMotor()
    {
        float motorTorque;

        if (currentGear == -1)
        {
            float input = Mathf.Min(verticalInput, 0f);
            motorTorque = input * motorForce * 1.5f;
        }
        else
        {
            float input      = Mathf.Max(verticalInput, 0f);
            float multiplier = gearTorqueMultipliers[Mathf.Clamp(currentGear - 1, 0, gearTorqueMultipliers.Length - 1)];
            motorTorque      = input * motorForce * multiplier;
        }

        frontLeftWheelCollider.motorTorque  = motorTorque;
        frontRightWheelCollider.motorTorque = motorTorque;

        currentBrakeForce = isBraking ? brakeForce : 0f;
        ApplyBraking();
    }

    private void ApplyBraking()
    {
        frontLeftWheelCollider.brakeTorque  = currentBrakeForce;
        frontRightWheelCollider.brakeTorque = currentBrakeForce;
        rearLeftWheelCollider.brakeTorque   = currentBrakeForce;
        rearRightWheelCollider.brakeTorque  = currentBrakeForce;
    }

    private void HandleSteering()
    {
        currentSteeringAngle = maxSteeringAngle * horizontalInput;
        frontLeftWheelCollider.steerAngle  = currentSteeringAngle;
        frontRightWheelCollider.steerAngle = currentSteeringAngle;
    }

    private void UpdateWheelVisuals()
    {
        UpdateSingleWheel(frontLeftWheelCollider,  frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider,   rearLeftWheelTransform);
        UpdateSingleWheel(rearRightWheelCollider,  rearRightWheelTransform);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot * Quaternion.Euler(0f, 0f, 90f);
    }
}
