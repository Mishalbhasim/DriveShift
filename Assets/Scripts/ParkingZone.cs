using UnityEngine;

public class ParkingZone : MonoBehaviour
{
    private float stopTimer = 0f;
    private float timeToWait = 1.5f; // Must stay still for 1.5 seconds
    private bool isCarInside = false;
    private Rigidbody carRB;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isCarInside = true;
            carRB = other.GetComponent<Rigidbody>();
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

    private void Update()
    {
        if (isCarInside && carRB != null)
        {
            // 1. Check if car is stopped
            if (carRB.velocity.magnitude < 0.1f)
            {
                // 2. Check alignment (Are they straight?)
                float angle = Quaternion.Angle(transform.rotation, carRB.transform.rotation);
                float alignment = Mathf.Abs(Mathf.Cos(angle * Mathf.Deg2Rad));

                if (alignment > 0.95f) // Only start timer if they are parked straight
                {
                    stopTimer += Time.deltaTime;

                    if (stopTimer >= timeToWait)
                    {
                        // Pass the alignment to the manager for the score calculation
                        GameManager.Instance.TriggerLevelComplete(alignment);
                        isCarInside = false;
                    }
                }
                else
                {
                    stopTimer = 0f; // Reset if they are crooked
                }
            }
            else
            {
                stopTimer = 0f; // Reset if they move
            }
        }
    }
}