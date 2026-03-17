using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

[DefaultExecutionOrder(-20)]  // runs before LevelConfig (-10)
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level Settings")]
    public float timeLimit = 60f;
    public int maxCrashes = 5;

    private float currentTime;
    private int crashCount = 0;
    private GameState currentState;
    Coroutine timerRoutine;

    [Header("Final Level Settings")]
    public int lastLevelIndex = 10;
    public GameObject nextLevelButton;

    public enum GameState { Playing, GameOver, LevelComplete }

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // If a LevelConfig is present in this scene it will call Configure()
        // before Start() via Awake(). If not, we use the Inspector values.
        InitialiseLevel();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Called by LevelConfig.Awake() to override Inspector defaults.
    // Because LevelConfig has [DefaultExecutionOrder(-10)], its Awake fires
    // after GameManager.Awake() (which sets Instance) but before Start(),
    // so currentTime is always initialised with the correct value.
    public void Configure(float newTimeLimit, int newMaxCrashes)
    {
        timeLimit = newTimeLimit;
        maxCrashes = newMaxCrashes;

        Debug.Log($"[GameManager] Configure called — timeLimit={timeLimit}  maxCrashes={maxCrashes}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    void InitialiseLevel()
    {
        currentTime = timeLimit;
        crashCount = 0;
        currentState = GameState.Playing;
        Time.timeScale = 1f;
        UpdateHUD();

        if (timerRoutine != null) StopCoroutine(timerRoutine);

        // If timeLimit is 0 we skip the countdown (tutorial levels)
        if (timeLimit > 0f)
            timerRoutine = StartCoroutine(TimerRoutine());
        else
            UIManager.Instance.UpdateTimer("Timer : --:--");
    }

    // ─────────────────────────────────────────────────────────────────────────
    IEnumerator TimerRoutine()
    {
        while (currentTime > 0 && currentState == GameState.Playing)
        {
            currentTime -= Time.deltaTime;
            UIManager.Instance.UpdateTimer(FormatTime(currentTime));
            yield return null;
        }

        if (currentTime <= 0)
        {
            currentTime = 0;
            TriggerGameOver("OUT OF TIME");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void AddCrash()
    {
        if (currentState != GameState.Playing) return;
        crashCount++;
        UpdateHUD();
        if (crashCount >= maxCrashes)
            TriggerGameOver("TOO MANY CRASHES");
    }

    void UpdateHUD()
    {
        UIManager.Instance.UpdateCrash(crashCount);
    }

    string FormatTime(float time)
    {
        float t = Mathf.Max(0, time);
        int minutes = Mathf.FloorToInt(t / 60);
        int seconds = Mathf.FloorToInt(t % 60);
        return string.Format("Timer : {0:00}:{1:00}", minutes, seconds);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void TriggerGameOver(string reason)
    {
        if (currentState != GameState.Playing) return;
        currentState = GameState.GameOver;
        if (timerRoutine != null) StopCoroutine(timerRoutine);

        UIManager.Instance.ShowGameOver(
            FormatTime(timeLimit - currentTime),
            crashCount,
            reason);

        Time.timeScale = 0f;
    }

    public void TriggerLevelComplete(float accuracy = 1.0f)
    {
        if (currentState != GameState.Playing) return;
        currentState = GameState.LevelComplete;
        if (timerRoutine != null) StopCoroutine(timerRoutine);

        // Base 500 pts + time bonus + accuracy bonus - crash penalty
        int finalScore = 500
                       + Mathf.RoundToInt(currentTime * 10)
                       + Mathf.RoundToInt(accuracy * 200)   // 0–200 from precision
                       - (crashCount * 50);
        finalScore = Mathf.Max(0, finalScore);

        UIManager.Instance.ShowLevelComplete(
            FormatTime(timeLimit - currentTime),
            crashCount,
            finalScore);

        Time.timeScale = 0f;
    }

    // ─────────────────────────────────────────────────────────────────────────
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
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(next);
    }

    // Exposed for ParkingZoneEvaluator to read time spent
    public float ElapsedSeconds => timeLimit - currentTime;
}