using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("HUD")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI crashText;

    [Header("Panels")]
    public GameObject gameOverPanel;
    public GameObject levelCompletePanel;

    [Header("Game Over Text")]
    public TextMeshProUGUI gameOverTimeText;
    public TextMeshProUGUI gameOverCrashText;
    public TextMeshProUGUI gameOverReasonText;

    [Header("Level Complete Text")]
    public TextMeshProUGUI levelTimeText;
    public TextMeshProUGUI levelCrashText;
    public TextMeshProUGUI levelScoreText;

    void Awake()
    {
        Instance = this;

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (levelCompletePanel) levelCompletePanel.SetActive(false);
    }

    public void UpdateTimer(string time)
    {
        if (timerText)
            timerText.text = time;
    }

    public void UpdateCrash(int crashCount)
    {
        if (crashText)
            crashText.text = "Crashes: " + crashCount;
    }

    public void ShowGameOver(string time, int crashes, string reason)
    {
        gameOverPanel.SetActive(true);

        gameOverTimeText.text = time;
        gameOverCrashText.text = "CRASHES: " + crashes;
        gameOverReasonText.text = "(" + reason + ")";
    }

    public void ShowLevelComplete(string time, int crashes, int score)
    {
        levelCompletePanel.SetActive(true);

        levelTimeText.text = time;
        levelCrashText.text = "CRASHES: " + crashes;
        levelScoreText.text = "SCORE: " + score;
    }
}