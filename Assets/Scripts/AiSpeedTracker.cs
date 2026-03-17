using UnityEngine;

// Small helper component — stores the original NavMeshAgent speed so
// multipliers applied by LevelConfig never compound across level reloads.
public class AISpeedTracker : MonoBehaviour
{
    [HideInInspector] public float baseSpeed = 5f;
}