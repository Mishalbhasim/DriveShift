using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PedestrianSpawner : MonoBehaviour
{
    [Header("Pedestrian Prefabs")]
    public GameObject[] pedestrianPrefabs;

    [Header("Walkable Area")]
    public GameObject walkableArea;

    [Header("Spawn Settings")]
    public int pedestriansPerPrefab = 2;

    void Start()
    {
        SpawnAllPedestrians();
    }

    void SpawnAllPedestrians()
    {
        if (pedestrianPrefabs == null || pedestrianPrefabs.Length == 0)
        {
            Debug.LogWarning("PedestrianSpawner: No prefabs assigned!");
            return;
        }
        if (walkableArea == null)
        {
            Debug.LogWarning("PedestrianSpawner: Walkable Area not assigned!");
            return;
        }

        foreach (GameObject prefab in pedestrianPrefabs)
        {
            if (prefab == null) continue;
            for (int i = 0; i < pedestriansPerPrefab; i++)
                SpawnSinglePedestrian(prefab);
        }
    }

    void SpawnSinglePedestrian(GameObject prefab)
    {
        Renderer[] planes = walkableArea.GetComponentsInChildren<Renderer>();
        if (planes.Length == 0)
        {
            Debug.LogWarning("PedestrianSpawner: No renderers found.");
            return;
        }

        for (int attempt = 0; attempt < 30; attempt++)
        {
            Renderer randomPlane = planes[Random.Range(0, planes.Length)];
            Bounds bounds = randomPlane.bounds;

            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);

            
            Vector3 rayOrigin = new Vector3(randomX, bounds.max.y + 5f, randomZ);
            float exactY = bounds.center.y;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit rayHit, 20f))
                exactY = rayHit.point.y;

            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(new Vector3(randomX, exactY, randomZ), out hit, 5f, NavMesh.AllAreas))
            {
                //Debug.Log("NavMesh sample found at: " + hit.position + " distance: " + hit.distance);
                StartCoroutine(SpawnAfterDelay(prefab, hit.position));
                return;
            }
        }

        Debug.LogWarning("PedestrianSpawner: Could not find NavMesh point for " + prefab.name);
    }

    IEnumerator SpawnAfterDelay(GameObject prefab, Vector3 position)
    {
        
        yield return null;
        yield return null;

        
        GameObject ped = Instantiate(prefab, position, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
        ped.name = prefab.name + "_Pedestrian";

        NavMeshAgent agent = ped.GetComponent<NavMeshAgent>();
        PedestrianAI ai = ped.GetComponent<PedestrianAI>();

        if (agent == null) { Debug.LogError(ped.name + ": Missing NavMeshAgent!"); Destroy(ped); yield break; }
        if (ai == null) { Debug.LogError(ped.name + ": Missing PedestrianAI!"); Destroy(ped); yield break; }

        
        yield return null;
        yield return null;

        //Debug.Log("Spawned: " + ped.name +
        //          " | isOnNavMesh: " + agent.isOnNavMesh +
        //          " | Position: " + ped.transform.position);

        if (!agent.isOnNavMesh)
        {
                
            agent.enabled = false;
            yield return null;
            agent.enabled = true;
            yield return null;

            Debug.Log("After re-enable | isOnNavMesh: " + agent.isOnNavMesh);

            if (!agent.isOnNavMesh)
            {
                Debug.LogError(ped.name + ": Still not on NavMesh. " +
                               "Open Window > AI > Navigation > Bake and confirm " +
                               "blue surface covers position: " + position);
                Destroy(ped);
                yield break;
            }
        }

        ai.walkableArea = walkableArea;
        ai.Initialize();
    }
}