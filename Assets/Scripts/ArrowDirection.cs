using UnityEngine;

public class ArrowDirection : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    public Transform player;

    // ── Set by LevelConfig ────────────────────────────────────────────────────
    private bool guideEnabled = true;

    // Called by LevelConfig
    public void Configure(bool enabled, float fadeStart)
    {
        guideEnabled = enabled;
        gameObject.SetActive(enabled);
    }

    // ─────────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!guideEnabled || target == null || player == null) return;

        Vector3 direction = target.position - player.position;
        direction.y = 0f;
        Vector3 localDirection = player.InverseTransformDirection(direction);
        float angle = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, -angle);
    }
}