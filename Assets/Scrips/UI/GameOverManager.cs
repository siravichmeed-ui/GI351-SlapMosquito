using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverManager : MonoBehaviour
{
    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public TMP_Text finalScoreText;

    [Header("Settings")]
    public float gameOverDelay = 2f;

    [Header("Stop Spawners")]
    public Behaviour[] spawnersToStop;

    [Header("Objects to Clear")]
    public string gameplayObjectTag = "GameplayObject";

    private bool isGameOver = false;

    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        Time.timeScale = 1f;
        isGameOver = false;
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
        // 1. รอให้เอฟเฟกต์กระจกแตกแสดงผล
        yield return new WaitForSecondsRealtime(gameOverDelay);

        // 2. หยุดระบบ Spawn
        StopAllSpawners();

        // 3. ลบยุงทั้งหมด
        ClearAllMosquitoes();

        // 4. ลบบอสยุงทั้งหมด
        ClearAllBosses();

        // 5. ลบวัตถุเกมเพลย์ตาม Tag
        ClearGameplayObjects();

        // 6. อัปเดตคะแนนบนหน้า Game Over
        UpdateFinalScore();

        // 7. แสดงหน้า Game Over
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // 8. หยุดเวลาในเกม
        Time.timeScale = 0f;

        Debug.Log("GAME OVER: ลบวัตถุเกมเพลย์ทั้งหมดแล้ว");
    }

    private void UpdateFinalScore()
    {
        if (finalScoreText == null)
        {
            Debug.LogWarning("ยังไม่ได้กำหนด Final Score Text ใน Inspector");
            return;
        }

        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("ไม่พบ ScoreManager.Instance");
            finalScoreText.text = "SCORE: 0";
            return;
        }

        finalScoreText.text = "SCORE: " + ScoreManager.Instance.totalScore;
    }

    private void StopAllSpawners()
    {
        if (spawnersToStop == null)
            return;

        foreach (Behaviour spawner in spawnersToStop)
        {
            if (spawner != null)
            {
                spawner.enabled = false;
            }
        }
    }

    private void ClearAllMosquitoes()
    {
        Mosquito[] mosquitoes =
            FindObjectsByType<Mosquito>(FindObjectsSortMode.None);

        foreach (Mosquito mosquito in mosquitoes)
        {
            if (mosquito != null)
            {
                Destroy(mosquito.gameObject);
            }
        }

        Debug.Log("ลบยุงธรรมดาและยุงพิเศษแล้ว");
    }

    private void ClearAllBosses()
    {
        BossMosquito[] bosses =
            FindObjectsByType<BossMosquito>(FindObjectsSortMode.None);

        foreach (BossMosquito boss in bosses)
        {
            if (boss != null)
            {
                Destroy(boss.gameObject);
            }
        }

        Debug.Log("ลบบอสยุงแล้ว");
    }

    private void ClearGameplayObjects()
    {
        if (string.IsNullOrEmpty(gameplayObjectTag))
            return;

        GameObject[] gameplayObjects;

        try
        {
            gameplayObjects = GameObject.FindGameObjectsWithTag(gameplayObjectTag);
        }
        catch (UnityException)
        {
            Debug.LogWarning(
                "ไม่พบ Tag: " + gameplayObjectTag +
                " กรุณาสร้าง Tag นี้ใน Unity หรือเปลี่ยนชื่อ Tag ใน Inspector"
            );

            return;
        }

        foreach (GameObject obj in gameplayObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}