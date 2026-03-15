using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

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


    public enum GameState
    {
        Playing,
        GameOver,
        LevelComplete
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        currentTime = timeLimit;
        crashCount = 0;

        currentState = GameState.Playing;



        UpdateHUD();
        timerRoutine = StartCoroutine(TimerRoutine());
    }



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


    public void AddCrash()
    {
        if (currentState != GameState.Playing) return;

        crashCount++;
        UpdateHUD();

        if (crashCount >= maxCrashes)
        {
            TriggerGameOver("TOO MANY CRASHES");
        }
    }

    void UpdateHUD()
    {
        UIManager.Instance.UpdateCrash(crashCount);
    }

    string FormatTime(float time)
    {
        float displayTime = Mathf.Max(0, time);
        int minutes = Mathf.FloorToInt(displayTime / 60);
        int seconds = Mathf.FloorToInt(displayTime % 60);
        return string.Format("Time: {0:00}:{1:00}", minutes, seconds);
    }



    public void TriggerGameOver(string reason)
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.GameOver;

        if (timerRoutine != null)
        {
            StopCoroutine(timerRoutine);
        }

        UIManager.Instance.ShowGameOver(
            FormatTime(timeLimit - currentTime),
            crashCount,
            reason
        );

        Time.timeScale = 0f;
    }

    public void TriggerLevelComplete(float accuracy = 1.0f)
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.LevelComplete;

        if (timerRoutine != null)
            StopCoroutine(timerRoutine);

        int finalScore = 500 + Mathf.RoundToInt(currentTime * 10) - (crashCount * 50);
        finalScore = Mathf.Max(0, finalScore);

        UIManager.Instance.ShowLevelComplete(
            FormatTime(timeLimit - currentTime),
            crashCount,
            finalScore
        );

        Time.timeScale = 0f;
    }



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