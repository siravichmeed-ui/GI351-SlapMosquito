
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class MosquitoBurstLauncher : MonoBehaviour
{
    [Header("Mosquito")]
    [SerializeField] private GameObject mosquitoNormal;
    [SerializeField] private GameObject mosquitoBurst;
    [SerializeField] private Animator burstAnimator;

    [Header("Animation")]
    [SerializeField] private string animationStateName = "M04";
    [SerializeField] private float animationDuration = 1f;

    [Header("Button Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonClickSound;
    [Range(0f, 1f)]
    [SerializeField] private float buttonVolume = 1f;

    [Header("Comic")]
    [SerializeField] private ComicCameraController comicController;

    private bool isPlaying;

    public void PlayGame()
    {
        if (isPlaying)
            return;
        // เล่นเสียงทันทีที่กดปุ่ม Play
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound, buttonVolume);
        }

        StartCoroutine(PlayBurstSequence());
    }

    private IEnumerator PlayBurstSequence()
    {
        isPlaying = true;

        // ซ่อนเมนู
        if (mosquitoNormal != null)
            mosquitoNormal.SetActive(false);

        // แสดงภาพยุงแตก
        if (mosquitoBurst != null)
            mosquitoBurst.SetActive(true);

        if (burstAnimator == null)
        {
            Debug.LogError("กรุณากำหนด Burst Animator ใน Inspector");
            yield break;
        }

        burstAnimator.enabled = true;
        burstAnimator.Play(animationStateName, 0, 0f);
        burstAnimator.Update(0f);

        // รอให้ Animation เล่นจนจบ
        yield return new WaitForSeconds(animationDuration);

        // ซ่อนภาพยุงแตก
        if (mosquitoBurst != null)
            mosquitoBurst.SetActive(false);

        // เริ่ม Comic Cutscene ต่อ
        if (comicController != null)
            comicController.PlayCutscene();

        isPlaying = false;
    }
}
