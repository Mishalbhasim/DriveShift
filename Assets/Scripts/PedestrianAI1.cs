using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PedestrianAI : MonoBehaviour
{
    [Header("Waypoint Settings")]
    public GameObject walkableArea;
    public float minWaitTime = 1f;
    public float maxWaitTime = 4f;

    [Header("Movement Settings")]
    public float walkSpeed = 1.5f;

    [Header("Animation")]
    public Animator animator;

    private NavMeshAgent agent;
    private Renderer currentPlane; 

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();
        agent.speed = walkSpeed;
    }

    void Update()
    {
        if (animator != null)
            animator.SetFloat("Speed", agent.isOnNavMesh ? agent.velocity.magnitude : 0f);
    }

    public void Initialize()
    {
        
        currentPlane = GetNearestPlane();
        StartCoroutine(WanderRoutine());
    }

    IEnumerator WanderRoutine()
    {
        yield return null;
        yield return null;
        yield return null;

        
        float waitTimer = 0f;
        while (!agent.isOnNavMesh)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer > 5f)
            {
                Debug.LogError(gameObject.name + ": FAILED - Agent never got on NavMesh.");
                yield break;
            }
            yield return null;
        }

        
        yield return new WaitForSeconds(Random.Range(0f, 2f));

        while (true)
        {
            
            if (Random.value < 0.2f)
                currentPlane = GetRandomPlane();

            Vector3 destination = GetRandomPointOnPlane(currentPlane);

            if (destination == Vector3.zero)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            agent.SetDestination(destination);
            yield return null;

            
            while (agent.pathPending)
                yield return null;

            
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                
                currentPlane = GetRandomPlane();
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            
            float timeout = Mathf.Clamp(agent.remainingDistance / walkSpeed * 2f, 10f, 60f);
            float elapsed = 0f;

            while (true)
            {
                elapsed += Time.deltaTime;

                if (elapsed > timeout)
                {
                    agent.ResetPath();
                    
                    currentPlane = GetNearestPlane();
                    break;
                }

                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                    break;

                yield return null;
            }

            
            yield return new WaitForSeconds(Random.Range(minWaitTime, maxWaitTime));
        }
    }

    
    Vector3 GetRandomPointOnPlane(Renderer plane)
    {
        if (plane == null) plane = GetRandomPlane();
        if (plane == null) return Vector3.zero;

        Bounds bounds = plane.bounds;

        for (int i = 0; i < 15; i++)
        {
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);

            Vector3 rayOrigin = new Vector3(randomX, bounds.max.y + 5f, randomZ);
            float exactY = bounds.center.y;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit rayHit, 20f))
                exactY = rayHit.point.y;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(new Vector3(randomX, exactY, randomZ), out hit, 2f, NavMesh.AllAreas))
                return hit.position;
        }

        return Vector3.zero;
    }

    
    Renderer GetNearestPlane()
    {
        if (walkableArea == null) return null;

        Renderer[] planes = walkableArea.GetComponentsInChildren<Renderer>();
        if (planes.Length == 0) return null;

        Renderer nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Renderer plane in planes)
        {
            float dist = Vector3.Distance(transform.position, plane.bounds.center);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = plane;
            }
        }

        return nearest;
    }

    
    Renderer GetRandomPlane()
    {
        if (walkableArea == null) return null;

        Renderer[] planes = walkableArea.GetComponentsInChildren<Renderer>();
        if (planes.Length == 0) return null;

        return planes[Random.Range(0, planes.Length)];
    }

    void OnDrawGizmosSelected()
    {
        if (agent != null && agent.isOnNavMesh && agent.hasPath)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(agent.destination, 0.3f);
        }
    }
}