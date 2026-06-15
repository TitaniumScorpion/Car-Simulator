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

    private float horizontalInput;
    private float verticalInput;
    private bool isBraking;
    private float currentSteeringAngle;
    private float currentBrakeForce;

    private void Update()
    {
        GetInput();
        UpdateWheelVisuals();
    }

    private void FixedUpdate()
    {
        HandleMotor();
        HandleSteering();
    }

    private void GetInput()
    {
        horizontalInput = 0f;
        verticalInput = 0f;
        isBraking = false;

        // Keyboard input
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) verticalInput += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) verticalInput -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontalInput -= 1f;
            
            isBraking = Keyboard.current.spaceKey.isPressed;
        }

        // Gamepad input (for flexible peripheral support)
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.01f)
            {
                horizontalInput = stick.x;
                verticalInput = stick.y;
            }
            
            if (Gamepad.current.buttonSouth.isPressed || Gamepad.current.leftTrigger.isPressed) isBraking = true;
        }
    }

    private void HandleMotor()
    {
        // Applying torque to the front wheels (Front-Wheel Drive)
        float motorTorque = verticalInput * motorForce;
        frontLeftWheelCollider.motorTorque = motorTorque;
        frontRightWheelCollider.motorTorque = motorTorque;

        currentBrakeForce = isBraking ? brakeForce : 0f;
        ApplyBraking();
    }

    private void ApplyBraking()
    {
        // Apply brakes to all four wheels
        frontLeftWheelCollider.brakeTorque = currentBrakeForce;
        frontRightWheelCollider.brakeTorque = currentBrakeForce;
        rearLeftWheelCollider.brakeTorque = currentBrakeForce;
        rearRightWheelCollider.brakeTorque = currentBrakeForce;
    }

    private void HandleSteering()
    {
        // Steer the front wheels
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
        // Get the position and rotation from the physics collider and apply to the visual mesh
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }
}