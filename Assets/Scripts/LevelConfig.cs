using UnityEngine;

[DefaultExecutionOrder(-10)]
public class LevelConfig : MonoBehaviour
{
    [Header("─── Timer & Crashes ───────────────────────")]
    public float timeLimit = 60f;
    public int maxCrashes = 5;

    [Header("─── AI Traffic ─────────────────────────────")]
    [Range(0, 12)]
    public int activeAICars = 0;
    [Range(0.5f, 1.5f)]
    public float aiSpeedMultiplier = 1.0f;

    [Header("─── Parking Zone ────────────────────────────")]
    [Tooltip("0 = Forward  1 = Reverse  2 = Parallel")]
    public int parkingType = 0;
    [Range(0.5f, 5f)]
    public float precisionRadius = 1f;

    [Header("─── Navigation ─────────────────────────────")]
    public bool showDirectionArrow = true;
    public float arrowFadeStartDistance = 999f;
    public bool showMinimap = true;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Configure(timeLimit, maxCrashes);
        else
            Debug.LogWarning("[LevelConfig] GameManager.Instance is null.");
    }

    void Start()
    {
        // ── Parking zone ──────────────────────────────────────────────────────
        var zone = FindObjectOfType<ParkingZone>();
        if (zone != null)
            zone.Configure(precisionRadius, parkingType);
        else
            Debug.LogWarning("[LevelConfig] No ParkingZone found in scene.");

        // ── Traffic ───────────────────────────────────────────────────────────
        var spawner = FindObjectOfType<CarSpawner>();
        if (spawner != null)
            spawner.SetMaxCars(activeAICars, aiSpeedMultiplier);
        else
            Debug.LogWarning("[LevelConfig] No CarSpawner found in scene.");

        // ── Navigation arrow ──────────────────────────────────────────────────
        var guide = FindObjectOfType<ArrowDirection>();
        if (guide != null)
            guide.Configure(showDirectionArrow, arrowFadeStartDistance);
        else
            Debug.LogWarning("[LevelConfig] No ArrowDirection found in scene.");

        // ── Minimap ───────────────────────────────────────────────────────────
        var minimap = FindObjectOfType<MinimapController>();
        if (minimap != null)
            minimap.Configure(showMinimap);
        else
            Debug.LogWarning("[LevelConfig] No MinimapController found in scene.");
    }
}