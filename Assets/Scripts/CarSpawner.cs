using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using Mono.Cecil.Cil;

public class CarSpawner : MonoBehaviour
{
    [Header("Car Settings")]
    public GameObject[] carPrefabs;
    public RoadNode[] spawnNodes;

    [Header("Spawn Settings")]
    public int maxCars = 10;
    public float spawnInterval = 2f;
    public float spawnCheckRadius = 3f;

    private int currentCars = 0;

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            if (currentCars < maxCars)
                SpawnCar();

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnCar()
    {
        if (carPrefabs.Length == 0 || spawnNodes.Length == 0)
            return;

        RoadNode spawnNode = spawnNodes[Random.Range(0, spawnNodes.Length)];

        if (spawnNode == null)
        {
            Debug.LogWarning("CarSpawner: Null spawn node found.");
            return;
        }


        Collider[] hits = Physics.OverlapSphere(spawnNode.transform.position, spawnCheckRadius);
        foreach (Collider hit in hits)
        {
            if (hit.GetComponent<AICar>() != null)
                return;
        }


        NavMeshHit navHit;
        if (!NavMesh.SamplePosition(spawnNode.transform.position, out navHit, 5f, NavMesh.AllAreas))
        {
            Debug.LogWarning("CarSpawner: Spawn node " + spawnNode.name + " is not near NavMesh. Skipping.");
            return;
        }

        StartCoroutine(SpawnCarAfterDelay(spawnNode, navHit.position));
    }

    IEnumerator SpawnCarAfterDelay(RoadNode spawnNode, Vector3 spawnPosition)
    {

        yield return null;
        yield return null;

        GameObject selectedCar = carPrefabs[Random.Range(0, carPrefabs.Length)];


        GameObject car = Instantiate(selectedCar, spawnPosition, spawnNode.transform.rotation);
        car.name = selectedCar.name + "_Car";

        NavMeshAgent agent = car.GetComponent<NavMeshAgent>();
        AICar ai = car.GetComponent<AICar>();

        if (agent == null) { Debug.LogError(car.name + ": Missing NavMeshAgent!"); Destroy(car); yield break; }
        if (ai == null) { Debug.LogError(car.name + ": Missing AICar!"); Destroy(car); yield break; }


        yield return null;
        yield return null;


        if (!agent.isOnNavMesh)
        {
            agent.enabled = false;
            yield return null;
            agent.enabled = true;
            yield return null;
        }


        if (!agent.isOnNavMesh)
        {
            Debug.LogError(car.name + ": Failed to attach to NavMesh at " + spawnPosition +
                   ". Make sure your spawn nodes are placed ON the blue NavMesh surface.");
            Destroy(car);
            yield break;
        }


        agent.Warp(spawnPosition);


        ai.currentNode = spawnNode;


        CarDestroyNotifier notifier = car.AddComponent<CarDestroyNotifier>();
        notifier.spawner = this;

        currentCars++;
        //Debug.Log("Car spawned: " + car.name + " | isOnNavMesh: " + agent.isOnNavMesh + " | Total: " + currentCars);
    }

    public void OnCarDestroyed()
    {
        currentCars = Mathf.Max(0, currentCars - 1);
    }
}
