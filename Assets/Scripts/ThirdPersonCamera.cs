using UnityEngine;
using UnityEngine.Rendering.HighDefinition;


[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(HDAdditionalCameraData))]
public class SmoothThirdPersonCamera : MonoBehaviour
{

    [Header("Target")]
    public Transform target;


    [Header("Normal Follow")]
    public float distance = 6f;
    public float height = 3.5f;


    // ─── REVERSE VIEW ───────────────────────────────────────────────────────────
    // Camera moves to the FRONT of the car and looks BACKWARD so you can see
    // any obstacles you're about to back into.
    [Header("Reverse Mode")]
    public float reverseDistance = 7f;
    public float reverseHeight = 4f;
    [Tooltip("Seconds of continuous reversing before the camera starts moving to the front.")]
    public float reverseEnterDelay = 0.35f;
    [Tooltip("Seconds after stopping reverse before blending back behind the car.")]
    public float reverseExitDelay = 0.55f;
    [Tooltip("How long (seconds) the camera takes to fully travel from back to front of car (and back). Higher = smoother arc.")]
    public float reverseBlendDuration = 0.65f;


    [Header("Speed-Adaptive Distance")]
    [Tooltip("Camera pulls in when nearly stopped — better parking precision.")]
    public bool useSpeedAdaptiveDistance = true;
    [Tooltip("Distance used when the car is fully stopped.")]
    public float parkingDistance = 4.5f;
    [Tooltip("Speed (m/s) at which the camera reaches its normal distance.")]
    public float parkingSpeedThreshold = 5f;
    [Tooltip("Hard cap — the camera NEVER goes further than this.")]
    public float maxCameraDistance = 7f;


    // ─── OVERHEAD VIEW ──────────────────────────────────────────────────────────
    // The pivot smoothly follows actual velocity direction, so the car stays
    // centred whether driving forward, reversing, or stopped.
    [Header("Overhead / Parking-Assist View")]
    [Tooltip("Key to toggle the top-down overhead view.")]
    public KeyCode overheadToggleKey = KeyCode.V;
    public float overheadHeight = 18f;
    [Tooltip("Camera pitch in overhead mode (90 = straight down).")]
    [Range(60f, 90f)]
    public float overheadTiltAngle = 80f;
    [Tooltip("Max look-ahead offset from car centre toward travel direction.")]
    public float overheadLookahead = 2.5f;
    [Tooltip("How quickly the overhead pivot tracks velocity direction changes.")]
    public float overheadLookaheadSmooth = 3f;


    [Header("Turn Offset (Lookahead)")]
    public float turnOffsetAmount = 1.5f;
    public float offsetSmoothTime = 0.2f;


    [Header("Camera Smoothing")]
    public float positionSmoothTime = 0.15f;
    [Tooltip("Rotation smoothing time in seconds.")]
    public float rotationSmoothTime = 0.12f;
    [Tooltip("How fast the internal forward vector tracks the car's nose.")]
    public float forwardSmoothSpeed = 5f;


    [Header("Dynamic FOV")]
    [Tooltip("Animates FOV with speed.")]
    public bool useDynamicFOV = true;
    public float fovAtRest = 55f;
    public float fovAtFullSpeed = 58f;
    [Tooltip("FOV while in reverse-front view.")]
    public float fovReverse = 60f;
    [Tooltip("Overhead view FOV.")]
    public float fovOverhead = 45f;
    [Tooltip("Speed (m/s) mapped to fovAtFullSpeed.")]
    public float maxSpeedForFOV = 15f;
    public float fovSmoothTime = 0.4f;


    [Header("Obstacle Avoidance")]
    public LayerMask obstacleMask;
    [Tooltip("Push-back from surface when occluded.")]
    public float obstacleOffset = 0.4f;


    [Header("HDRP Antialiasing")]
    [Tooltip("AA while driving normally.")]
    public HDAdditionalCameraData.AntialiasingMode drivingAA =
        HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
    [Tooltip("AA in overhead view.")]
    public HDAdditionalCameraData.AntialiasingMode overheadAA =
        HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;


    [Header("Camera Reset")]
    [Tooltip("Instantly snaps the camera behind the car.")]
    public KeyCode resetKey = KeyCode.R;


    // ─── PRIVATE STATE ───────────────────────────────────────────────────────────
    private Camera cam;
    private HDAdditionalCameraData hdCam;
    private Rigidbody carRb;

    private Vector3 posVelocity = Vector3.zero;
    private Quaternion rotDerivative;
    private Vector3 smoothedForward;
    private Vector3 smoothedTargetPos;
    private float currentTurnOffset;
    private float turnOffsetVelocity;
    private float currentFOV;
    private float fovVelocity;
    private bool isOverheadMode;
    private bool isPhysicalCamera;

    // Hysteresis timer: positive = car has been reversing, negative = has stopped
    private float reverseTimer = 0f;
    // True while the camera should be in the front-facing reverse position
    private bool isReverseCamera = false;
    // 0 = fully behind car, 1 = fully in front of car — smoothly interpolated
    private float reverseCameraBlend = 0f;
    private float reverseCameraBlendVelocity = 0f;

    // Smoothed horizontal velocity direction used by the overhead view so the
    // pivot glides toward travel direction rather than flipping instantly.
    private Vector3 smoothedOverheadOffset = Vector3.zero;


    // ────────────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (!target)
        {
            Debug.LogError("SmoothThirdPersonCamera: Target not assigned!", this);
            enabled = false;
            return;
        }

        cam = GetComponent<Camera>();
        hdCam = GetComponent<HDAdditionalCameraData>();
        carRb = target.GetComponent<Rigidbody>();

        hdCam.stopNaNs = true;
        isPhysicalCamera = cam.usePhysicalProperties;

        smoothedForward = target.forward;
        smoothedTargetPos = target.position;
        rotDerivative = Quaternion.identity;
        currentFOV = fovAtRest;
        ApplyFOV(currentFOV);

        SnapBehindCar();
        ApplyAntialiasing();
    }


    // ────────────────────────────────────────────────────────────────────────────
    void LateUpdate()
    {
        if (!target) return;

        HandleInput();

        float fwdT = forwardSmoothSpeed * Time.deltaTime;
        smoothedTargetPos = Vector3.Lerp(smoothedTargetPos, target.position, fwdT);
        smoothedForward = Vector3.Slerp(smoothedForward, target.forward, fwdT);

        float carSpeed = 0f;
        float forwardVel = 0f;
        if (carRb != null)
        {
            forwardVel = Vector3.Dot(carRb.velocity, smoothedForward);
            carSpeed = carRb.velocity.magnitude;
        }

        // ── Reverse detection with enter/exit hysteresis ────────────────────────
        bool physicallyReversing = forwardVel < -0.3f;
        if (physicallyReversing)
            reverseTimer = Mathf.Min(reverseTimer + Time.deltaTime, reverseEnterDelay);
        else
            reverseTimer = Mathf.Max(reverseTimer - Time.deltaTime, -reverseExitDelay);

        if (!isReverseCamera && reverseTimer >= reverseEnterDelay) isReverseCamera = true;
        if (isReverseCamera && reverseTimer <= -reverseExitDelay) isReverseCamera = false;

        // ── Smooth blend: 0 = behind car, 1 = in front of car ───────────────────
        // This replaces the old binary flag so the camera arcs smoothly across
        // the car instead of teleporting the target position.
        float blendTarget = isReverseCamera ? 1f : 0f;
        reverseCameraBlend = Mathf.SmoothDamp(reverseCameraBlend, blendTarget,
                                              ref reverseCameraBlendVelocity,
                                              reverseBlendDuration);

        // ── Early-out for overhead ───────────────────────────────────────────────
        if (isOverheadMode)
        {
            UpdateOverheadView();
            UpdateFOV(carSpeed);
            return;
        }

        // ── Turn-offset lookahead (only in normal forward drive) ─────────────────
        float steerInput = isReverseCamera ? 0f : Input.GetAxis("Horizontal");
        float targetOffset = steerInput * turnOffsetAmount;
        currentTurnOffset = Mathf.SmoothDamp(currentTurnOffset, targetOffset,
                                                ref turnOffsetVelocity, offsetSmoothTime);

        // ── Speed-adaptive base distance (forward only) ──────────────────────────
        float camDist = distance;
        float camH = height;
        if (useSpeedAdaptiveDistance && reverseCameraBlend < 0.01f)
        {
            float t = Mathf.InverseLerp(0f, parkingSpeedThreshold, carSpeed);
            camDist = Mathf.Lerp(parkingDistance, distance, t);
            camH = Mathf.Lerp(height * 0.75f, height, t);
        }
        camDist = Mathf.Min(camDist, maxCameraDistance);

        // ── Desired position: lerp between behind-car and front-of-car ───────────
        // Because reverseCameraBlend is a smooth float we get a cinematic arc
        // rather than the camera snapping across the car body.
        Vector3 behindPos = smoothedTargetPos
                           - smoothedForward * camDist
                           + Vector3.up * camH
                           + target.right * currentTurnOffset;

        Vector3 frontPos = smoothedTargetPos
                           + smoothedForward * reverseDistance
                           + Vector3.up * reverseHeight;

        Vector3 desiredPos = Vector3.Lerp(behindPos, frontPos, reverseCameraBlend);

        // Look-at also blends: forward view looks ahead of car, reverse view looks
        // past the rear so obstacles are centred in the frame.
        Vector3 behindLook = smoothedTargetPos + smoothedForward * 1.5f + Vector3.up * 0.8f;
        Vector3 frontLook = smoothedTargetPos - smoothedForward * 2.0f + Vector3.up * 0.6f;
        Vector3 lookAt = Vector3.Lerp(behindLook, frontLook, reverseCameraBlend);

        // ── Obstacle avoidance ───────────────────────────────────────────────────
        Vector3 castOrigin = smoothedTargetPos + Vector3.up * 1f;
        if (obstacleMask != 0 &&
            Physics.Linecast(castOrigin, desiredPos, out RaycastHit hit, obstacleMask))
        {
            desiredPos = hit.point + hit.normal * obstacleOffset;
        }

        // ── Apply smoothed position & rotation ───────────────────────────────────
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos,
                                                ref posVelocity, positionSmoothTime);

        Quaternion targetRot = Quaternion.LookRotation(lookAt - transform.position);
        transform.rotation = SmoothDampQuaternion(transform.rotation, targetRot,
                                                   ref rotDerivative, rotationSmoothTime);

        UpdateFOV(carSpeed);
    }


    // ────────────────────────────────────────────────────────────────────────────
    void UpdateOverheadView()
    {
        // ── Velocity-driven look-ahead offset ────────────────────────────────────
        // Instead of a binary sign flip, we build an offset that continuously
        // tracks the car's actual movement direction in the horizontal plane.
        // When moving forward the pivot sits ahead of the car; when reversing it
        // sits behind it — and it GLIDES between the two so the car never drifts
        // out of frame during the transition.
        Vector3 velOffset = Vector3.zero;
        if (carRb != null)
        {
            Vector3 flatVel = Vector3.ProjectOnPlane(carRb.velocity, Vector3.up);
            if (flatVel.magnitude > 0.3f)
                velOffset = flatVel.normalized * overheadLookahead;
            // else velOffset stays zero → car centred when stopped
        }

        // Smooth the offset so it glides rather than snaps
        smoothedOverheadOffset = Vector3.Lerp(smoothedOverheadOffset, velOffset,
                                              overheadLookaheadSmooth * Time.deltaTime);

        // Track the actual car position directly (not the lagged smoothedTargetPos)
        // so the car never drifts out of frame at speed.
        Vector3 overheadTarget = target.position
                               + smoothedOverheadOffset
                               + Vector3.up * overheadHeight;

        transform.position = Vector3.SmoothDamp(transform.position, overheadTarget,
                                                ref posVelocity, positionSmoothTime * 0.8f);

        // Yaw tracks the car; pitch set by overheadTiltAngle
        float yaw = Quaternion.LookRotation(smoothedForward).eulerAngles.y;
        Quaternion wantRot = Quaternion.Euler(overheadTiltAngle, yaw, 0f);
        transform.rotation = SmoothDampQuaternion(transform.rotation, wantRot,
                                                  ref rotDerivative,
                                                  rotationSmoothTime * 1.5f);
    }


    // ────────────────────────────────────────────────────────────────────────────
    void UpdateFOV(float speed)
    {
        if (!useDynamicFOV || cam == null) return;

        float t = Mathf.Clamp01(speed / maxSpeedForFOV);
        float forwardFOV = Mathf.Lerp(fovAtRest, fovAtFullSpeed, t);
        float targetFOV;

        if (isOverheadMode)
            targetFOV = fovOverhead;
        else
            // Smoothly blend FOV as camera arcs around the car
            targetFOV = Mathf.Lerp(forwardFOV, fovReverse, reverseCameraBlend);

        currentFOV = Mathf.SmoothDamp(currentFOV, targetFOV, ref fovVelocity, fovSmoothTime);
        ApplyFOV(currentFOV);
    }


    void ApplyFOV(float fov)
    {
        if (cam == null) return;
        if (isPhysicalCamera)
        {
            float halfRad = fov * 0.5f * Mathf.Deg2Rad;
            cam.focalLength = (cam.sensorSize.y * 0.5f) / Mathf.Tan(halfRad);
        }
        else
        {
            cam.fieldOfView = fov;
        }
    }


    void ApplyAntialiasing()
    {
        if (hdCam == null) return;
        hdCam.antialiasing = isOverheadMode ? overheadAA : drivingAA;
    }


    // ────────────────────────────────────────────────────────────────────────────
    void HandleInput()
    {
        if (Input.GetKeyDown(overheadToggleKey))
        {
            isOverheadMode = !isOverheadMode;
            ApplyAntialiasing();
        }

        if (Input.GetKeyDown(resetKey))
            SnapBehindCar();
    }


    public void SnapBehindCar()
    {
        if (!target) return;

        transform.position = target.position
                           - target.forward * distance
                           + Vector3.up * height;

        Vector3 lookAt = target.position + target.forward * 1.5f + Vector3.up * 0.8f;
        transform.rotation = Quaternion.LookRotation(lookAt - transform.position);

        rotDerivative = Quaternion.identity;
        posVelocity = Vector3.zero;
        reverseTimer = 0f;
        isReverseCamera = false;
        reverseCameraBlend = 0f;
        reverseCameraBlendVelocity = 0f;
        smoothedOverheadOffset = Vector3.zero;
    }


    // ────────────────────────────────────────────────────────────────────────────
    // Quaternion smooth-damp (standard component-wise approach)
    // ────────────────────────────────────────────────────────────────────────────
    static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target,
                                           ref Quaternion deriv, float smoothTime)
    {
        if (Time.deltaTime < Mathf.Epsilon) return current;

        if (Quaternion.Dot(current, target) < 0f)
        {
            target.x = -target.x; target.y = -target.y;
            target.z = -target.z; target.w = -target.w;
        }

        Vector4 v4 = SmoothDampV4(
            new Vector4(current.x, current.y, current.z, current.w),
            new Vector4(target.x, target.y, target.z, target.w),
            ref deriv, smoothTime);

        return new Quaternion(v4.x, v4.y, v4.z, v4.w).normalized;
    }

    static Vector4 SmoothDampV4(Vector4 cur, Vector4 tgt,
                                ref Quaternion deriv, float smoothTime)
    {
        float dt = Time.deltaTime;
        float omega = 2f / smoothTime;
        float x = omega * dt;
        float e = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);

        Vector4 change = cur - tgt;
        Vector4 d = new Vector4(deriv.x, deriv.y, deriv.z, deriv.w);
        Vector4 temp = (d + omega * change) * dt;

        deriv = new Quaternion(
            (d.x - omega * temp.x) * e,
            (d.y - omega * temp.y) * e,
            (d.z - omega * temp.z) * e,
            (d.w - omega * temp.w) * e);

        return tgt + (change + temp) * e;
    }
}