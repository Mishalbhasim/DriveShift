using UnityEngine;

public class CarCollision : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        // Make sure the floor/road is tagged as "Ground"
        if (!collision.gameObject.CompareTag("Ground"))
        {
            // Only count if the hit is hard enough (prevents tiny scrapes from ending the game)
            if (collision.relativeVelocity.magnitude > 0.5f)
            {
                ParkingGameManager.Instance.AddCrash();
            }
        }
    }
}
