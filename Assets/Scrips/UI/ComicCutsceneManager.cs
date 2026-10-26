using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ComicCutsceneManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject comicCutscene;
    [SerializeField] private Image[] comicPanels;

    [Header("Animation Settings")]
    [SerializeField] private float delayBetweenPanels = 0.5f;

    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "GameScene";

    private bool isPlaying = false;

    private void Start()
    {
        // เริ่มต้นด้วยหน้า Main Menu
        if (mainMenu != null)
            mainMenu.SetActive(true);

        if (comicCutscene != null)
            comicCutscene.SetActive(false);

        // ซ่อนภาพมังงะทุกช่อง
        foreach (Image panel in comicPanels)
        {
            if (panel != null)
                panel.gameObject.SetActive(false);
        }
    }

    public void PlayCutscene()
    {
        // ป้องกันการกด PLAY ซ้ำ
        if (isPlaying)
            return;

        isPlaying = true;

        StartCoroutine(PlayComicSequence());
    }

    private IEnumerator PlayComicSequence()
    {
        // ซ่อนหน้า Main Menu
        if (mainMenu != null)
            mainMenu.SetActive(false);

        // เปิดหน้า Cutscene
        comicCutscene.SetActive(true);

        // แสดงภาพทีละช่อง
        foreach (Image panel in comicPanels)
        {
            if (panel == null)
                continue;

            panel.gameObject.SetActive(true);

            // รอตามเวลาที่กำหนด
            yield return new WaitForSeconds(delayBetweenPanels);
        }

        // รอให้ผู้เล่นเห็นภาพสุดท้ายก่อนเปลี่ยนฉาก
        yield return new WaitForSeconds(1f);

        // โหลดฉากเกม
        SceneManager.LoadScene(gameSceneName);
    }
}