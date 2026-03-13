using UnityEngine;

public class ArrowDirection : MonoBehaviour
{
    public Transform target;   
    public Transform player;  
        
    void Update()
    {
        Vector3 direction = target.position - player.position;
        direction.y = 0f;

      
        Vector3 localDirection = player.InverseTransformDirection(direction);

        float angle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, -angle);
    }
} 