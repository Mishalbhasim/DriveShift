using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// CarAudioController
//
// Drop this on your player car.
// It reads car state every frame and tells AudioManager what to play.
// ─────────────────────────────────────────────────────────────────────────────
[RequireComponent(typeof(Rigidbody))]
public class CarAudioController : MonoBehaviour
{
    [Header("Screech Settings")]
    public float screechMinSpeed = 3f;
    public float screechSlipThreshold = 0.4f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (AudioManager.Instance == null) return;

        float speed = rb.velocity.magnitude;

        // ── Engine ────────────────────────────────────────────────────────────
        AudioManager.Instance.UpdateEngine(speed);

        // ── Screech ───────────────────────────────────────────────────────────
        bool braking = Input.GetKey(KeyCode.Space) && speed > screechMinSpeed;

        Vector3 localVel = transform.InverseTransformDirection(rb.velocity);
        float lateralSlip = Mathf.Abs(localVel.x) / Mathf.Max(speed, 0.1f);
        bool slipping = lateralSlip > screechSlipThreshold
                              && speed > screechMinSpeed;

        AudioManager.Instance.UpdateScreech(braking || slipping);
    }
}