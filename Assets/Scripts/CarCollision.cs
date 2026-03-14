using UnityEngine;

public class CarCollision : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        
        if (!collision.gameObject.CompareTag("Ground"))
        {
            
            if (collision.relativeVelocity.magnitude > 0.5f)
            {
                GameManager.Instance.AddCrash();
            }
        }
    }
}
