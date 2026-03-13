using UnityEngine;

public class CarDestroyNotifier : MonoBehaviour
{
    public CarSpawner spawner;

    void OnDestroy()
    {
        if (spawner != null)
            spawner.OnCarDestroyed();
    }
}