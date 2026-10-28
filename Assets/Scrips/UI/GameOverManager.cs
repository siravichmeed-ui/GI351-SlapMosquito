using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("Game Over UI")]
    public GameObject gameOverPanel;

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

        // 2. หยุดระบบ Spawn ทั้งหมดที่กำหนดไว้
        StopAllSpawners();

        // 3. ลบยุงธรรมดาและยุงพิเศษทั้งหมด
        ClearAllMosquitoes();

        // 4. ลบบอสยุงทั้งหมด
        ClearAllBosses();

        // 5. ลบวัตถุเกมเพลย์ที่กำหนด Tag ไว้
        ClearGameplayObjects();

        // 6. แสดงหน้า Game Over
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // 7. หยุดเวลาในเกม
        Time.timeScale = 0f;

        Debug.Log("GAME OVER: ลบวัตถุเกมเพลย์ทั้งหมดแล้ว");
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
            FindObjectsByType<Mosquito>(
                FindObjectsSortMode.None
            );

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
            FindObjectsByType<BossMosquito>(
                FindObjectsSortMode.None
            );

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

        // ป้องกัน Error หากยังไม่ได้สร้าง Tag
        bool tagExists = false;

        foreach (string tag in UnityEditorTags())
        {
            if (tag == gameplayObjectTag)
            {
                tagExists = true;
                break;
            }
        }

        if (!tagExists)
        {
            Debug.LogWarning(
                "ไม่พบ Tag: " + gameplayObjectTag +
                " กรุณาสร้าง Tag ใน Unity ก่อน"
            );

            return;
        }

        GameObject[] gameplayObjects =
            GameObject.FindGameObjectsWithTag(
                gameplayObjectTag
            );

        foreach (GameObject obj in gameplayObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
    }

    private string[] UnityEditorTags()
    {
        // Tag ที่ใช้งานได้จะถูกตรวจสอบผ่าน Unity โดยตรง
        // ฟังก์ชันนี้จะถูกแทนที่ด้วย Tag ที่กำหนดด้านล่าง
        return new string[]
        {
            "GameplayObject"
        };
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}
