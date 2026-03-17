using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────────────
// MinimapController
//
// Drop on any persistent GameObject (e.g. your LevelConfig object).
// Wire up all references in the Inspector.
// ─────────────────────────────────────────────────────────────────────────────
public class MinimapController : MonoBehaviour
{
    [Header("Camera")]
    public Camera minimapCamera;
    [Tooltip("Orthographic size — how much of the world is visible. 50 = medium world.")]
    public float orthographicSize = 50f;

    [Header("Tracked Objects")]
    public Transform playerCar;
    public Transform parkingZone;

    [Header("UI")]
    public RawImage minimapDisplay;   // the RawImage showing the render texture
    public RectTransform playerDot;        // white dot
    public RectTransform zoneDot;          // yellow dot

    [Header("Zone Dot Pulse")]
    public float pulseSpeed = 3f;
    public float pulseMinScale = 0.8f;
    public float pulseMaxScale = 1.3f;

    private RectTransform displayRect;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (minimapCamera != null)
        {
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = orthographicSize;
        }

        if (minimapDisplay != null)
            displayRect = minimapDisplay.GetComponent<RectTransform>();
    }

    // Called by LevelConfig
    public void Configure(bool enabled)
    {
        gameObject.SetActive(enabled);
        if (minimapDisplay != null) minimapDisplay.gameObject.SetActive(enabled);
        if (playerDot != null) playerDot.gameObject.SetActive(enabled);
        if (zoneDot != null) zoneDot.gameObject.SetActive(enabled);
    }

    // ─────────────────────────────────────────────────────────────────────────
    void LateUpdate()
    {
        if (minimapCamera == null || playerCar == null) return;

        // ── 1. Lock camera above player, pointing down ────────────────────────
        minimapCamera.transform.position = new Vector3(
            playerCar.position.x,
            playerCar.position.y + 60f,
            playerCar.position.z);

        // Rotate camera with player so map "up" = car forward
        minimapCamera.transform.rotation = Quaternion.Euler(90f, playerCar.eulerAngles.y, 0f);

        // ── 2. Player dot — always dead centre ───────────────────────────────
        if (playerDot != null)
            playerDot.anchoredPosition = Vector2.zero;

        // ── 3. Zone dot — positioned relative to player on the minimap ────────
        if (zoneDot != null && parkingZone != null && displayRect != null)
        {
            // World offset from player to zone
            Vector3 delta = parkingZone.position - playerCar.position;

            // Use InverseTransformDirection so Unity handles the rotation correctly
            Vector3 localDelta = playerCar.InverseTransformDirection(delta);
            float localX = localDelta.x;
            float localZ = localDelta.z;

            // Normalise by camera view size and map to display rect
            float halfW = orthographicSize * minimapCamera.aspect;
            float halfH = orthographicSize;

            float u = Mathf.Clamp(localX / (halfW * 2f), -0.48f, 0.48f);
            float v = Mathf.Clamp(localZ / (halfH * 2f), -0.48f, 0.48f);

            float dispW = displayRect.rect.width;
            float dispH = displayRect.rect.height;

            zoneDot.anchoredPosition = new Vector2(u * dispW, v * dispH);

            // Pulse the zone dot so it's easy to spot
            float pulse = Mathf.Lerp(pulseMinScale, pulseMaxScale,
                                     (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            zoneDot.localScale = Vector3.one * pulse;
        }
    }
}