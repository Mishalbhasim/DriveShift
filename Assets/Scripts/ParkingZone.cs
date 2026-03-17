using UnityEngine;

public class ParkingZone : MonoBehaviour
{
    private float stopTimer = 0f;
    private float timeToWait = 1.5f;
    private bool isCarInside = false;
    private Rigidbody carRB;
    private float precisionRadius = 1f;
    private int parkingType = 0;   // 0=Forward  1=Reverse  2=Parallel

    // ── Called by LevelConfig ─────────────────────────────────────────────────
    public void Configure(float radius, int type = 0)
    {
        precisionRadius = radius;
        parkingType = type;
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isCarInside = true;
            carRB = other.attachedRigidbody;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isCarInside = false;
            stopTimer = 0f;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (!isCarInside || carRB == null) return;

        if (carRB.velocity.magnitude < 0.1f)
        {
            float distance = Vector3.Distance(carRB.transform.position, transform.position);

            // Forward:  car nose must point same direction as zone forward
            // Reverse:  car nose must point OPPOSITE to zone forward
            // Parallel: same as reverse
            Vector3 expectedDir = (parkingType == 0)
                                  ? transform.forward
                                  : -transform.forward;

            // Dot product: 1.0 = perfect, 0 = 90° off, -1 = opposite direction
            // Using Dot instead of Abs(Cos) so nose-first fails on reverse levels
            float alignment = Vector3.Dot(carRB.transform.forward.normalized,
                                          expectedDir.normalized);

            if (distance <= precisionRadius && alignment > 0.85f)
            {
                stopTimer += Time.deltaTime;
                if (stopTimer >= timeToWait)
                {
                    GameManager.Instance.TriggerLevelComplete(alignment);
                    isCarInside = false;
                }
            }
            else
            {
                stopTimer = 0f;
            }
        }
        else
        {
            stopTimer = 0f;
        }
    }
}