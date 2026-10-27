using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score")]
    public int totalScore = 0;
    public int baseScore = 100;

    [Header("UI")]
    public TMP_Text scoreText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        UpdateScoreUI();
    }

    public int AddScore(int multiplier)
    {
        int gainedScore = baseScore * multiplier;

        totalScore += gainedScore;

        UpdateScoreUI();

        return gainedScore;
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "SCORE: " + totalScore;
        }
    }

    public void ResetScore()
    {
        totalScore = 0;
        UpdateScoreUI();
    }
}