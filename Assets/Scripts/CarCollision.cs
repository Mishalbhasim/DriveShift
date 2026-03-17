using UnityEngine;

public class CarCollision : MonoBehaviour
{
    public float crashCooldown = 1.5f;
    private float lastCrashTime = -999f;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground")) return;
        if (collision.relativeVelocity.magnitude <= 0.5f) return;
        if (Time.time - lastCrashTime < crashCooldown) return;

        lastCrashTime = Time.time;

        AudioManager.Instance?.PlayCrash();
        GameManager.Instance.AddCrash();
    }
}