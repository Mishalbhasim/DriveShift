using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ParkingGameManager : MonoBehaviour
{
    public static ParkingGameManager Instance { get; private set; }

    [Header("Level Settings")]
    public float timeLimit = 60f;
    public int maxCrashes = 5;
    private float currentTime;
    private int crashCount = 0;
    private bool isGameActive = true;

    [Header("UI Panels")]
    public GameObject gameOverPanel;
    public GameObject nextLevelPanel;

    [Header("HUD References")]
    public TextMeshProUGUI hudTimerText;
    public TextMeshProUGUI hudCrashText;

    [Header("Game Over Panel Text")]
    public TextMeshProUGUI gameOverTimeText;
    public TextMeshProUGUI gameOverCrashText;
    public TextMeshProUGUI gameOverReasonText;

    [Header("Next Level Panel Text")]
    public TextMeshProUGUI nextLevelTimeText;
    public TextMeshProUGUI nextLevelCrashText;
    public TextMeshProUGUI nextLevelScoreText;

    [Header("Final Level Settings")]
    public int lastLevelIndex = 10;
    public GameObject nextLevelButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        
        Time.timeScale = 1f;
    }

    void Start()
    {
        currentTime = timeLimit;
        crashCount = 0;

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (nextLevelPanel) nextLevelPanel.SetActive(false);

        UpdateHUD();
    }

    void Update()
    {
        if (isGameActive)
        {
            HandleTimer();
        }
    }

    void HandleTimer()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            UpdateTimerDisplay(hudTimerText, currentTime);
        }
        else
        {
            currentTime = 0;
            TriggerGameOver("OUT OF TIME");
        }
    }

    public void AddCrash()
    {
        if (!isGameActive) return;

        crashCount++;
        UpdateHUD();

        if (crashCount >= maxCrashes)
        {
            TriggerGameOver("TOO MANY CRASHES");
        }
    }

    void UpdateHUD()
    {
        if (hudCrashText != null) hudCrashText.text = "Crashes: " + crashCount;
    }

    string FormatTime(float time)
    {
        float displayTime = Mathf.Max(0, time);
        int minutes = Mathf.FloorToInt(displayTime / 60);
        int seconds = Mathf.FloorToInt(displayTime % 60);
        return string.Format("Time: {0:00}:{1:00}", minutes, seconds);
    }

    void UpdateTimerDisplay(TextMeshProUGUI textTarget, float time)
    {
        if (textTarget != null) textTarget.text = FormatTime(time);
    }

    public void TriggerGameOver(string reason)
    {
        if (!isGameActive) return;
        isGameActive = false;

        if (gameOverTimeText) gameOverTimeText.text = FormatTime(timeLimit - currentTime);
        if (gameOverCrashText) gameOverCrashText.text = "CRASHES: " + crashCount;
        if (gameOverReasonText) gameOverReasonText.text = "(" + reason + ")";

        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void TriggerLevelComplete(float accuracy = 1.0f)
    {
        if (!isGameActive) return;
        isGameActive = false;

        if (nextLevelTimeText) nextLevelTimeText.text = FormatTime(timeLimit - currentTime);
        if (nextLevelCrashText) nextLevelCrashText.text = "CRASHES: " + crashCount;

        // Score Calculation
        int finalScore = 500 + Mathf.RoundToInt(currentTime * 10) - (crashCount * 50);
        if (nextLevelScoreText) nextLevelScoreText.text = "SCORE: " + Mathf.Max(0, finalScore);

        nextLevelPanel.SetActive(true);
        Time.timeScale = 0f;

        // Check for Last Level
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (currentSceneIndex >= lastLevelIndex)
        {
            if (nextLevelButton != null) nextLevelButton.SetActive(false);
        }
    }

    // --- BUTTON FUNCTIONS ---

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
    }
}