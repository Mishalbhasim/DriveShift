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

   

    [Header("Reverse Mode")]
    public float reverseDistance = 7.5f;
    public float reverseHeight = 4.5f;

    

    [Header("Speed-Adaptive Distance")]
    [Tooltip("Camera pulls in when nearly stopped — better parking precision.")]
    public bool useSpeedAdaptiveDistance = true;
    [Tooltip("Distance used when the car is fully stopped.")]
    public float parkingDistance = 4.5f;
    [Tooltip("Speed (m/s) at which the camera reaches its normal distance. Keep low for a parking sim.")]
    public float parkingSpeedThreshold = 5f;
    [Tooltip("Hard cap — the camera NEVER goes further than this, regardless of speed. Key setting for a parking sim.")]
    public float maxCameraDistance = 7f;

   

    [Header("Overhead / Parking-Assist View")]
    [Tooltip("Key to toggle the top-down overhead view.")]
    public KeyCode overheadToggleKey = KeyCode.V;
    public float overheadHeight = 18f;
    [Tooltip("Camera pitch in overhead mode (90 = straight down).")]
    [Range(60f, 90f)]
    public float overheadTiltAngle = 80f;
    public float overheadDistance = 2f;


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
    [Tooltip("Animates FOV with speed. Works with physical camera (focal length) too.")]
    public bool useDynamicFOV = true;
    public float fovAtRest = 55f;
    [Tooltip("Keep this close to fovAtRest for a parking sim — large values cause the racing-game feel.")]
    public float fovAtFullSpeed = 58f;
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
    [Tooltip("AA while driving. SMAA = crisp edges on the car body and road markings.")]
    public HDAdditionalCameraData.AntialiasingMode drivingAA =
        HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;

    [Tooltip("AA in overhead view. TAA = smoother ground geometry, less shimmer.")]
    public HDAdditionalCameraData.AntialiasingMode overheadAA =
        HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;

    

    [Header("Camera Reset")]
    [Tooltip("Instantly snaps the camera behind the car.")]
    public KeyCode resetKey = KeyCode.R;

    

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
        bool isReversing = forwardVel < -0.1f;

        
        if (isOverheadMode)
        {
            UpdateOverheadView();
            UpdateFOV(carSpeed);
            return;
        }

        
        float steerInput = Input.GetAxis("Horizontal");
        float targetOffset = steerInput * turnOffsetAmount;
        currentTurnOffset = Mathf.SmoothDamp(currentTurnOffset, targetOffset,
                                               ref turnOffsetVelocity, offsetSmoothTime);

        
        float camDist = isReversing ? reverseDistance : distance;
        float camH = isReversing ? reverseHeight : height;

        if (useSpeedAdaptiveDistance && !isReversing)
        {
            float t = Mathf.InverseLerp(0f, parkingSpeedThreshold, carSpeed);
            camDist = Mathf.Lerp(parkingDistance, camDist, t);
            camH = Mathf.Lerp(camH * 0.75f, camH, t);
        }

        
        camDist = Mathf.Min(camDist, maxCameraDistance);

        
        Vector3 desiredPos = smoothedTargetPos
                           - smoothedForward * camDist
                           + Vector3.up * camH
                           + target.right * currentTurnOffset;

        
        Vector3 castOrigin = smoothedTargetPos + Vector3.up * 1f;
        if (obstacleMask != 0 &&
            Physics.Linecast(castOrigin, desiredPos, out RaycastHit hit, obstacleMask))
        {
            desiredPos = hit.point + hit.normal * obstacleOffset;
        }

        
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos,
                                                 ref posVelocity, positionSmoothTime);

        
        Vector3 lookAt = smoothedTargetPos + smoothedForward * 1.5f + Vector3.up * 0.8f;
        Quaternion targetRot = Quaternion.LookRotation(lookAt - transform.position);
        transform.rotation = SmoothDampQuaternion(transform.rotation, targetRot,
                                                    ref rotDerivative, rotationSmoothTime);

        
        UpdateFOV(carSpeed);
    }

    

    void UpdateOverheadView()
    {
        Vector3 overheadTarget = smoothedTargetPos
                               + smoothedForward * overheadDistance
                               + Vector3.up * overheadHeight;

        transform.position = Vector3.SmoothDamp(transform.position, overheadTarget,
                                                 ref posVelocity, positionSmoothTime * 1.5f);

        float yaw = Quaternion.LookRotation(smoothedForward).eulerAngles.y;
        Quaternion wantRot = Quaternion.Euler(overheadTiltAngle, yaw, 0f);
        transform.rotation = SmoothDampQuaternion(transform.rotation, wantRot,
                                                  ref rotDerivative, rotationSmoothTime * 1.5f);
    }

    

    void UpdateFOV(float speed)
    {
        if (!useDynamicFOV || cam == null) return;

        float t = Mathf.Clamp01(speed / maxSpeedForFOV);
        float targetFOV = isOverheadMode
                        ? fovOverhead
                        : Mathf.Lerp(fovAtRest, fovAtFullSpeed, t);

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
    }

   

    static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target,
                                            ref Quaternion deriv, float smoothTime)
    {
        if (Time.deltaTime < Mathf.Epsilon) return current;

        
        if (Quaternion.Dot(current, target) < 0f)
        {
            target.x = -target.x;
            target.y = -target.y;
            target.z = -target.z;
            target.w = -target.w;
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