using System.Collections;
using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    [Header("Game Over UI")]
    public GameObject gameOverPanel;

    [Header("Settings")]
    public float gameOverDelay = 2f;

    private bool isGameOver = false;

    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void TriggerGameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        // รอให้เห็นเอฟเฟกต์กระจกแตกก่อน
        yield return new WaitForSecondsRealtime(gameOverDelay);

        // แสดงหน้า Game Over
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // หยุดเกม
        Time.timeScale = 0f;

        Debug.Log("GAME OVER");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        UnityEngine.SceneManagement.SceneManager
            .LoadScene(
                UnityEngine.SceneManagement.SceneManager
                    .GetActiveScene().buildIndex
            );
    }
}