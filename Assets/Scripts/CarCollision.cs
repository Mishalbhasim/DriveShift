using UnityEngine;

public class CarCollision : MonoBehaviour
{
    [Tooltip("Seconds to wait before another crash can be registered. " +
             "Prevents one bump from firing multiple times.")]
    public float crashCooldown = 1.5f;

    private float lastCrashTime = -999f;

    private void OnCollisionEnter(Collision collision)
    {
        // Ignore ground and road
        if (collision.gameObject.CompareTag("Ground")) return;

        // Ignore weak contacts (rolling against a kerb slowly etc.)
        if (collision.relativeVelocity.magnitude <= 0.5f) return;

        // Cooldown — ignore if we just registered a crash
        if (Time.time - lastCrashTime < crashCooldown) return;

        lastCrashTime = Time.time;
        GameManager.Instance.AddCrash();
    }
}