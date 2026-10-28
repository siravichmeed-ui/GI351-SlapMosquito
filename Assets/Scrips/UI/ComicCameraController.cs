using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ComicCameraController : MonoBehaviour
{
    [System.Serializable]
    public class PanelEffect
    {
        [Range(0.1f, 3f)]
        public float zoom = 1f;

        [Min(0f)]
        public float pause = 0.5f;
    }
    

    [Header("UI")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject comicCutscene;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform comicPage;
    [SerializeField] private RectTransform[] panels;

    [Header("Camera Animation")]
    [SerializeField] private float moveDuration = 1f;
    [SerializeField] private float pauseDuration = 0.5f;
    [SerializeField] private float panelFill = 0.85f;

    [Header("Panel Appear Effect")]
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float popScale = 0.92f;

    [Header("Ending")]
    [SerializeField] private float endingDuration = 1.5f;
    [SerializeField] private string gameSceneName = "InGame";

    private bool isPlaying;

    private Vector2 originalPosition;
    private Vector3 originalScale;

    private CanvasGroup[] panelGroups;
    private Vector3[] panelOriginalScales;

    private void Awake()
    {
        originalPosition = comicPage.anchoredPosition;
        originalScale = comicPage.localScale;

        panelGroups = new CanvasGroup[panels.Length];
        panelOriginalScales = new Vector3[panels.Length];

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] == null)
                continue;

            // เพิ่ม CanvasGroup อัตโนมัติถ้ายังไม่มี
            CanvasGroup group =
                panels[i].GetComponent<CanvasGroup>();

            if (group == null)
                group = panels[i].gameObject.AddComponent<CanvasGroup>();

            panelGroups[i] = group;
            panelOriginalScales[i] = panels[i].localScale;
        }
    }

    private void Start()
    {
        mainMenu.SetActive(true);
        comicCutscene.SetActive(false);


    }
    public void PlayCutscene()
    {
        if (isPlaying)
            return;

        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        isPlaying = true;

        mainMenu.SetActive(false);
        comicCutscene.SetActive(true);

        comicPage.anchoredPosition = originalPosition;
        comicPage.localScale = originalScale;

        // ซ่อนภาพทั้งหมดก่อนเริ่ม Cutscene
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] == null || panelGroups[i] == null)
                continue;

            panelGroups[i].alpha = 0f;

            panels[i].localScale =
                panelOriginalScales[i] * popScale;
        }

        // แสดงภาพทีละช่องตามลำดับ
        for (int i = 0; i < panels.Length; i++)
        {
            RectTransform panel = panels[i];

            if (panel == null || panelGroups[i] == null)
                continue;

            // คำนวณจุดกึ่งกลางของช่อง
            Vector3 centerWorld =
                panel.TransformPoint(panel.rect.center);

            Vector3 centerLocal =
                comicPage.InverseTransformPoint(centerWorld);

            // คำนวณการซูมให้ช่องอยู่กลางหน้าจอ
            float zoomX =
                viewport.rect.width / panel.rect.width;

            float zoomY =
                viewport.rect.height / panel.rect.height;

            float zoom =
                Mathf.Min(zoomX, zoomY) * panelFill;

            Vector3 targetScale =
                originalScale * zoom;

            Vector2 targetPosition =
                -new Vector2(centerLocal.x, centerLocal.y) * zoom;

            // ให้กล้องเคลื่อนที่พร้อมกับการปรากฏของภาพ
            Coroutine cameraMove = StartCoroutine(
                MoveCamera(targetPosition, targetScale, moveDuration)
            );

            Coroutine panelAppear = StartCoroutine(
                FadeAndPopPanel(i)
            );

            yield return cameraMove;
            yield return panelAppear;

            // หยุดให้ผู้เล่นอ่านภาพ
            yield return new WaitForSeconds(pauseDuration);
        }

        // ซูมออกกลับไปเห็นหน้ามังงะทั้งหมด
        yield return StartCoroutine(
            MoveCamera(originalPosition, originalScale, endingDuration)
        );

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator FadeAndPopPanel(int index)
    {
        CanvasGroup group = panelGroups[index];
        RectTransform panel = panels[index];

        float elapsed = 0f;

        Vector3 startScale =
            panelOriginalScales[index] * popScale;

        Vector3 targetScale =
            panelOriginalScales[index];

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = fadeDuration > 0f
                ? Mathf.Clamp01(elapsed / fadeDuration)
                : 1f;

            // Fade แบบค่อย ๆ ปรากฏ
            group.alpha = t;

            // Pop แบบนุ่มนวล
            float smoothT = t * t * (3f - 2f * t);

            panel.localScale = Vector3.Lerp(
                startScale,
                targetScale,
                smoothT
            );

            yield return null;
        }

        group.alpha = 1f;
        panel.localScale = targetScale;
    }
    public void SkipCutscene()
    {
        if (!isPlaying)
            return;

        // หยุด Animation และ Coroutine ทั้งหมดของ Cutscene
        StopAllCoroutines();

        // โหลดฉากเกมทันที
        SceneManager.LoadScene(gameSceneName);
    }

    private IEnumerator MoveCamera(
        Vector2 targetPosition,
        Vector3 targetScale,
        float duration)
    {
        Vector2 startPosition = comicPage.anchoredPosition;
        Vector3 startScale = comicPage.localScale;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = duration > 0f
                ? Mathf.Clamp01(elapsed / duration)
                : 1f;

            // SmoothStep
            t = t * t * (3f - 2f * t);

            comicPage.anchoredPosition = Vector2.Lerp(
                startPosition,
                targetPosition,
                t
            );

            comicPage.localScale = Vector3.Lerp(
                startScale,
                targetScale,
                t
            );

            yield return null;
        }

        comicPage.anchoredPosition = targetPosition;
        comicPage.localScale = targetScale;
    }
}