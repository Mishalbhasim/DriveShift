using System.Collections.Generic;
using UnityEngine;

public class AICarPool : MonoBehaviour
{
    public static AICarPool Instance;

    [Header("Pool Settings")]
    public GameObject[] carPrefabs;
    public int poolSize = 20;

    private Queue<GameObject> pool = new Queue<GameObject>();

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
        if (pool.Count == 0)
            return null;

        GameObject car = pool.Dequeue();
        car.SetActive(true);

        return car;
    }

    public void ReturnCar(GameObject car)
    {
        car.SetActive(false);
        pool.Enqueue(car);
    }
}