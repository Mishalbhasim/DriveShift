using UnityEngine;
using UnityEngine.AI;

public class AICar : MonoBehaviour
{
    public RoadNode currentNode;

    public enum CarState
    {
        Driving,
        Waiting
    }

    private CarState currentState;

    [Header("Obstacle Detection")]
    public float detectionDistance = 3f;
    public float waitTime = 1.5f;

    private NavMeshAgent agent;
    private float waitTimer = 0f;
    


    


    void Start()
    {
        
        StartCoroutine(InitWithDelay());
    }

    void OnEnable()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(30, 70);

        agent.isStopped = false;
        waitTimer = 0f;

        currentState = CarState.Driving;
    }

    void OnDisable()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
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

        switch (currentState)
        {
            case CarState.Driving:
                DrivingState();
                break;

            case CarState.Waiting:
                WaitingState();
                break;
        }

        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            MoveToNextNode();
        }
    }

    void DrivingState()
    {
        CheckForObstacle();

        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            MoveToNextNode();
        }
    }

    void WaitingState()
    {
        waitTimer += Time.deltaTime;

        if (waitTimer >= waitTime)
        {
            waitTimer = 0f;
            agent.isStopped = false;
            currentState = CarState.Driving;
        }
    }

    void CheckForObstacle()
    {
        if (!agent.isOnNavMesh) return;

        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(origin, transform.forward, out hit, detectionDistance))
        {
            if (hit.collider.GetComponentInParent<AICar>() != null ||
                hit.collider.CompareTag("Player"))
            {
                agent.isStopped = true;
                currentState = CarState.Waiting;
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