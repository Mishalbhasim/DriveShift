using UnityEngine;

public class CarMovement : MonoBehaviour
{

    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;

    [Header("Wheel Meshes (Visual)")]
    public Transform frontLeftTransform;
    public Transform frontRightTransform;
    public Transform rearLeftTransform;
    public Transform rearRightTransform;


    [Header("Engine")]
    public float motorForce = 1200f;
    public float maxSpeedKMH = 60f;

    [Header("Steering")]
    public float maxSteerAngle = 28f;
    public float steerSpeed = 5f;
    public AnimationCurve steerCurve = AnimationCurve.Linear(0, 1f, 100f, 0.35f);

    [Header("Brakes")]
    public float brakeForce = 8000f;
    public float handbrakeForce = 5000f;
    // ── Engine braking fix ─────────────────────────────────────────────────────
    // Previously this value was written in HandleMotor and then immediately
    // overwritten to 0 in the HandleBrakesAndDrift else-branch, so engine
    // braking had no effect.  It is now applied inside HandleBrakesAndDrift
    // (the last function to write brakeTorque) so it is never discarded.
    public float engineBraking = 300f;

    [Header("Stability")]
    public float antiRollStrength = 3000f;
    public Vector3 centerOfMass = new Vector3(0f, -0.4f, 0.1f);

    [Header("Drift")]
    public float driftStiffness = 0.55f;
    public float normalSideStiffness = 1.8f;
    public float driftSpeedThreshold = 20f;


    private Rigidbody rb;
    private float currentSteerAngle;
    private float horizontalInput;
    private float verticalInput;
    private bool isBraking;
    private bool isHandbraking;

    public float SpeedKMH => rb.velocity.magnitude * 3.6f;


    // ────────────────────────────────────────────────────────────────────────────
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass;
        rb.drag = 0.05f;
        rb.angularDrag = 0.3f;
        SetupWheelFriction();
    }


    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        isBraking = Input.GetKey(KeyCode.Space);
        isHandbraking = Input.GetKey(KeyCode.LeftShift);
    }


    void FixedUpdate()
    {
        HandleMotor();
        HandleSteering();
        HandleBrakesAndDrift();   // ← must run AFTER HandleMotor; owns all brakeTorque writes
        ApplyAntiRoll();
        SyncWheelMeshes();
    }


    // ────────────────────────────────────────────────────────────────────────────
    void HandleMotor()
    {
        float speed = SpeedKMH;
        float torque = (speed < maxSpeedKMH) ? verticalInput * motorForce : 0f;

        rearLeftWheel.motorTorque = torque;
        rearRightWheel.motorTorque = torque;

        // brakeTorque is intentionally NOT set here — HandleBrakesAndDrift owns it.
    }


    void HandleSteering()
    {
        float speed = SpeedKMH;
        float speedFactor = steerCurve.Evaluate(speed);
        float targetAngle = maxSteerAngle * horizontalInput * speedFactor;

        currentSteerAngle = Mathf.Lerp(
            currentSteerAngle,
            targetAngle,
            Time.fixedDeltaTime * steerSpeed * (speed < 5f ? 2f : 1f)
        );

        frontLeftWheel.steerAngle = currentSteerAngle;
        frontRightWheel.steerAngle = currentSteerAngle;
    }


    // ────────────────────────────────────────────────────────────────────────────
    void HandleBrakesAndDrift()
    {
        float speed = SpeedKMH;

        if (isBraking)
        {
            // Full braking on all four wheels
            frontLeftWheel.brakeTorque = brakeForce;
            frontRightWheel.brakeTorque = brakeForce;
            rearLeftWheel.brakeTorque = brakeForce;
            rearRightWheel.brakeTorque = brakeForce;

            rearLeftWheel.motorTorque = 0f;
            rearRightWheel.motorTorque = 0f;

            SetSidewaysStiffness(speed > driftSpeedThreshold
                ? driftStiffness
                : normalSideStiffness);
        }
        else if (isHandbraking)
        {
            frontLeftWheel.brakeTorque = 0f;
            frontRightWheel.brakeTorque = 0f;
            rearLeftWheel.brakeTorque = handbrakeForce;
            rearRightWheel.brakeTorque = handbrakeForce;

            if (speed > 10f)
                SetSidewaysStiffness(driftStiffness);
        }
        else
        {
            // ── Engine braking ─────────────────────────────────────────────────
            // Apply only when the driver releases the throttle and the car is
            // moving.  Using a tiny dead-zone (0.05) avoids fighting gentle inputs.
            float engBrake = (Mathf.Abs(verticalInput) < 0.05f && speed > 1f)
                             ? engineBraking
                             : 0f;

            frontLeftWheel.brakeTorque = 0f;
            frontRightWheel.brakeTorque = 0f;
            rearLeftWheel.brakeTorque = engBrake;
            rearRightWheel.brakeTorque = engBrake;

            SetSidewaysStiffness(normalSideStiffness);
        }
    }


    void SetSidewaysStiffness(float stiffness)
    {
        SetWheelStiffness(frontLeftWheel, stiffness);
        SetWheelStiffness(frontRightWheel, stiffness);
        SetWheelStiffness(rearLeftWheel, stiffness);
        SetWheelStiffness(rearRightWheel, stiffness);
    }

    void SetWheelStiffness(WheelCollider wheel, float stiffness)
    {
        WheelFrictionCurve curve = wheel.sidewaysFriction;
        curve.stiffness = stiffness;
        wheel.sidewaysFriction = curve;
    }


    // ────────────────────────────────────────────────────────────────────────────
    void ApplyAntiRoll()
    {
        ApplyAntiRollToAxle(frontLeftWheel, frontRightWheel);
        ApplyAntiRollToAxle(rearLeftWheel, rearRightWheel);
    }

    void ApplyAntiRollToAxle(WheelCollider leftWheel, WheelCollider rightWheel)
    {
        bool leftGrounded = leftWheel.GetGroundHit(out WheelHit leftHit);
        bool rightGrounded = rightWheel.GetGroundHit(out WheelHit rightHit);

        float leftTravel = leftGrounded
            ? (-leftWheel.transform.InverseTransformPoint(leftHit.point).y
               - leftWheel.radius) / leftWheel.suspensionDistance
            : 1f;

        float rightTravel = rightGrounded
            ? (-rightWheel.transform.InverseTransformPoint(rightHit.point).y
               - rightWheel.radius) / rightWheel.suspensionDistance
            : 1f;

        float antiRollForce = (leftTravel - rightTravel) * antiRollStrength;

        if (leftGrounded)
            rb.AddForceAtPosition(leftWheel.transform.up * -antiRollForce,
                                  leftWheel.transform.position);
        if (rightGrounded)
            rb.AddForceAtPosition(rightWheel.transform.up * antiRollForce,
                                  rightWheel.transform.position);
    }


    // ────────────────────────────────────────────────────────────────────────────
    void SyncWheelMeshes()
    {
        SyncWheel(frontLeftWheel, frontLeftTransform);
        SyncWheel(frontRightWheel, frontRightTransform);
        SyncWheel(rearLeftWheel, rearLeftTransform);
        SyncWheel(rearRightWheel, rearRightTransform);
    }

    void SyncWheel(WheelCollider col, Transform t)
    {
        if (t == null) return;
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        t.SetPositionAndRotation(pos, rot);
    }


    // ────────────────────────────────────────────────────────────────────────────
    void SetupWheelFriction()
    {
        foreach (var wheel in new[] { frontLeftWheel, frontRightWheel,
                                      rearLeftWheel,  rearRightWheel })
        {
            WheelFrictionCurve fwd = wheel.forwardFriction;
            fwd.extremumSlip = 0.4f;
            fwd.extremumValue = 1f;
            fwd.asymptoteSlip = 0.8f;
            fwd.asymptoteValue = 0.75f;
            fwd.stiffness = 1.5f;
            wheel.forwardFriction = fwd;

            WheelFrictionCurve side = wheel.sidewaysFriction;
            side.extremumSlip = 0.25f;
            side.extremumValue = 1f;
            side.asymptoteSlip = 0.5f;
            side.asymptoteValue = 0.85f;
            side.stiffness = normalSideStiffness;
            wheel.sidewaysFriction = side;
        }
    }
}                   