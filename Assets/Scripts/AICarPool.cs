using System.Collections.Generic;
using UnityEngine;

public class AICarPool : MonoBehaviour
{
    public static AICarPool Instance;

    [Header("Pool Settings")]
    public GameObject[] carPrefabs;
    public int poolSize = 20;

    private Queue<GameObject> pool = new Queue<GameObject>();
    private List<GameObject> activeCars = new List<GameObject>();

    void Awake()
    {
        Instance = this;
        InitializePool();
    }

    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject prefab = carPrefabs[Random.Range(0, carPrefabs.Length)];
            GameObject car = Instantiate(prefab);
            car.SetActive(false);
            pool.Enqueue(car);
        }
    }

    public GameObject GetCar()
    {
        if (pool.Count == 0) return null;
        GameObject car = pool.Dequeue();
        car.SetActive(true);
        activeCars.Add(car);
        return car;
    }

    public void ReturnCar(GameObject car)
    {
        activeCars.Remove(car);
        car.SetActive(false);
        pool.Enqueue(car);
    }

    // Returns all currently active cars — used by CarSpawner.SetMaxCars
    public List<GameObject> GetActiveCars() => activeCars;
}