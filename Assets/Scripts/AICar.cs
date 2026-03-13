using UnityEngine;
using UnityEngine.AI;

public class AICar : MonoBehaviour
{
    public RoadNode currentNode;

    [Header("Obstacle Detection")]
    public float detectionDistance = 3f;
    public float waitTime = 1.5f;

    private NavMeshAgent agent;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        
        StartCoroutine(InitWithDelay());
    }

    System.Collections.IEnumerator InitWithDelay()
    {
        yield return null;
        yield return null;

        if (!agent.isOnNavMesh)
        {
            Debug.LogError(gameObject.name + ": Car is not on NavMesh! " +
                           "Check prefab has NavMeshAgent and car is placed on baked road NavMesh.");
            yield break;
        }

        MoveToNextNode();
    }

    void Update()
    {
        
        if (!agent.isOnNavMesh) return;

        CheckForObstacle();

        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                isWaiting = false;
                waitTimer = 0f;
                agent.isStopped = false;
            }
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            MoveToNextNode();
        }
    }

    void CheckForObstacle()
    {
        if (!agent.isOnNavMesh) return;

        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(origin, transform.forward, out hit, detectionDistance))
        {
            if (hit.collider.GetComponent<AICar>() != null ||
                hit.collider.CompareTag("Player"))
            {
                agent.isStopped = true;
                isWaiting = true;
                return;
            }
        }
    }

    void MoveToNextNode()
    {
        if (!agent.isOnNavMesh) return;

        if (currentNode == null || currentNode.nextNodes.Count == 0)
            return;

        int index = Random.Range(0, currentNode.nextNodes.Count);
        currentNode = currentNode.nextNodes[index];
        agent.SetDestination(currentNode.transform.position);
    }
}