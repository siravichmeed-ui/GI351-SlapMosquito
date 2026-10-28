using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance { get; private set; }

    [Header("Combo Settings")]
    [Min(0.1f)]
    public float comboTimeLimit = 2f;

    [Min(0f)]
    public float popupStayDuration = 0.5f;

    public int currentCombo = 0;
    [Header("Max Combo")]
    public int maxCombo = 0;

    private float comboTimer;
    private int lastMosquitoKilledFrame = -1;
    private Coroutine popupCoroutine;
    private Coroutine fadeCoroutine;

    [Header("Score Multiplier")]
    public int multiplierAt5 = 2;
    public int multiplierAt10 = 3;
    public int multiplierAt20 = 4;
    public int multiplierAt50 = 5;

    [Header("Popup UI")]
    public Canvas popupCanvas;
    public RectTransform popupRoot;
    public TMP_Text comboText;
    public TMP_Text scoreText;
    public Image popupBackground;

    [Header("Popup Animation")]
    public float popupScale = 1.25f;
    public float animationDuration = 0.15f;
    public float floatHeight = 60f;
    public float fadeDuration = 0.25f;

    [Header("Combo Impact")]
    public float normalImpact = 1f;
    public float greatImpact = 1.2f;
    public float amazingImpact = 1.4f;
    public float unstoppableImpact = 1.7f;
    public float maxComboImpact = 2f;

    [Header("Special Combo Scale")]
    public float normalScale = 1f;
    public float greatScale = 1.2f;
    public float amazingScale = 1.4f;
    public float unstoppableScale = 1.6f;
    public float maxComboScale = 1.8f;

    [Header("Special Combo Colors")]
    public Color normalColor = Color.white;
    public Color greatColor = Color.yellow;
    public Color amazingColor = new Color(1f, 0.5f, 0f);
    public Color unstoppableColor = Color.red;
    public Color maxComboColor = new Color(1f, 0.2f, 0.8f);

    [Header("Miss Detection")]
    public LayerMask mosquitoLayer;

    private Vector2 originalPosition;
    private Vector3 originalScale;
    private Vector3 currentStyleScale;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (popupRoot != null)
        {
            originalPosition = popupRoot.anchoredPosition;
            originalScale = popupRoot.localScale;
            currentStyleScale = originalScale;
        }

        HidePopup();
    }

    private void OnEnable()
    {
        Mosquito.OnMosquitoKilled += HandleMosquitoKilled;
    }

    private void OnDisable()
    {
        Mosquito.OnMosquitoKilled -= HandleMosquitoKilled;

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        CheckMissClick();

        if (currentCombo <= 0)
            return;

        comboTimer -= Time.deltaTime;

        if (comboTimer <= 0f)
            EndCombo();
    }

    // ตรวจจับการคลิกพลาด
    private void CheckMissClick()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        // ถ้าเพิ่งตีโดนยุงในเฟรมนี้ ห้ามรีเซ็ต Combo
        if (lastMosquitoKilledFrame == Time.frameCount)
            return;

        // ไม่ตรวจจับระหว่าง Slot Event
        if (SlotEventManager.Instance != null &&
            SlotEventManager.Instance.IsEventRunning)
        {
            return;
        }

        // ไม่ตรวจจับระหว่างที่ระบบบล็อก Input
        if (BreakableGlass.InputBlocked)
            return;

        // ต้องเลือกไม้ตียุงอยู่
        if (EquipmentManager.SelectedTool !=
            EquipmentManager.ToolType.FlySwatter)
        {
            return;
        }

        // คลิกโดน UI ไม่ถือว่าพลาด
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Camera cam = Camera.main;

        if (cam == null)
            return;

        Vector2 worldPoint = cam.ScreenToWorldPoint(
            Input.mousePosition
        );

        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);

       
bool hitMosquito = false;

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            // ตรวจสอบ Object หลักก่อน โดยไม่กรอง Layer ของ Collider ลูก
            Mosquito mosquito = hit.GetComponentInParent<Mosquito>();
            BossMosquito boss = hit.GetComponentInParent<BossMosquito>();

            if (mosquito != null || boss != null)
            {
                hitMosquito = true;
                break;
            }
        }

        if (!hitMosquito && currentCombo > 0)
        {
            ResetCombo();
        }

    }

    // รับ Event เมื่อยุงถูกตีสำเร็จ
    private void HandleMosquitoKilled(Mosquito mosquito)
    {
        if (mosquito == null)
            return;
        lastMosquitoKilledFrame = Time.frameCount;
        if (SlotEventManager.Instance != null &&
            SlotEventManager.Instance.IsEventRunning)
        {
            return;
        }

        currentCombo++;
        comboTimer = comboTimeLimit;

        int multiplier = GetMultiplier(currentCombo);
        int gainedScore = 0;

        if (ScoreManager.Instance != null)
        {
            gainedScore = ScoreManager.Instance.AddScore(multiplier);
        }

        ShowPopup(mosquito.transform.position, gainedScore);
    }

    public int GetMultiplier(int combo)
    {
        if (combo >= 50)
            return multiplierAt50;

        if (combo >= 20)
            return multiplierAt20;

        if (combo >= 10)
            return multiplierAt10;

        if (combo >= 5)
            return multiplierAt5;

        return 1;
    }

    public void ShowPopup(Vector3 worldPosition, int gainedScore)
    {
        if (popupRoot == null || popupCanvas == null)
            return;

        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        popupRoot.gameObject.SetActive(true);

        RestorePopupAlpha();
        UpdatePopupText(gainedScore);
        UpdatePopupStyle();

        Camera cam = Camera.main;

        if (cam != null)
        {
            Vector2 screenPosition =
                cam.WorldToScreenPoint(worldPosition);

            RectTransform canvasRect =
                popupCanvas.transform as RectTransform;

            Camera uiCamera =
                popupCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : popupCanvas.worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                uiCamera,
                out Vector2 localPosition))
            {
                popupRoot.anchoredPosition = localPosition;
            }
        }

        popupCoroutine = StartCoroutine(PlayPopupAnimation());
    }

    private void UpdatePopupText(int gainedScore)
    {
        if (comboText != null)
            comboText.text = GetComboLabel(currentCombo);

        if (scoreText != null)
            scoreText.text = "+" + gainedScore;
    }

    private string GetComboLabel(int combo)
    {
        if (combo >= 50)
            return "MOSQUITO ANNIHILATOR! x" + combo;

        if (combo >= 20)
            return "UNSTOPPABLE! x" + combo;

        if (combo >= 10)
            return "AMAZING! x" + combo;

        if (combo >= 5)
            return "GREAT! x" + combo;

        return "COMBO x" + combo;
    }

    private void UpdatePopupStyle()
    {
        Color color = normalColor;
        float scale = normalScale;

        if (currentCombo >= 50)
        {
            color = maxComboColor;
            scale = maxComboScale;
        }
        else if (currentCombo >= 20)
        {
            color = unstoppableColor;
            scale = unstoppableScale;
        }
        else if (currentCombo >= 10)
        {
            color = amazingColor;
            scale = amazingScale;
        }
        else if (currentCombo >= 5)
        {
            color = greatColor;
            scale = greatScale;
        }

        if (comboText != null)
            comboText.color = color;

        if (scoreText != null)
            scoreText.color = color;

        if (popupBackground != null)
            popupBackground.color = color;

        currentStyleScale = originalScale * scale;
        popupRoot.localScale = currentStyleScale;
    }

    private IEnumerator PlayPopupAnimation()
    {
        Vector2 startPosition = popupRoot.anchoredPosition;

        float impact = normalImpact;

        if (currentCombo >= 50)
            impact = maxComboImpact;
        else if (currentCombo >= 20)
            impact = unstoppableImpact;
        else if (currentCombo >= 10)
            impact = amazingImpact;
        else if (currentCombo >= 5)
            impact = greatImpact;

        Vector3 startScale = currentStyleScale * 0.65f;
        Vector3 targetScale = currentStyleScale * popupScale * impact;

        float elapsed = 0f;

        popupRoot.localScale = startScale;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / animationDuration);

            // เด้งออกแรงตอนเริ่ม แล้วค่อย ๆ ช้าลง
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            popupRoot.localScale = Vector3.Lerp(
                startScale,
                targetScale,
                eased
            );

            popupRoot.anchoredPosition =
                startPosition +
                Vector2.up * (floatHeight * 0.25f * eased);

            yield return null;
        }

        popupRoot.localScale = targetScale;
        popupCoroutine = null;
    }

    private void EndCombo()
    {
        currentCombo = 0;
        comboTimer = 0f;

        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (popupRoot != null && popupRoot.gameObject.activeSelf)
            popupCoroutine = StartCoroutine(WaitBeforeHide());
    }

    private IEnumerator WaitBeforeHide()
    {
        yield return new WaitForSeconds(popupStayDuration);

        popupCoroutine = null;
        fadeCoroutine = StartCoroutine(FadePopup());
    }

    private IEnumerator FadePopup()
    {
        float elapsed = 0f;

        Color comboStart = comboText != null
            ? comboText.color
            : Color.white;

        Color scoreStart = scoreText != null
            ? scoreText.color
            : Color.white;

        Color backgroundStart = popupBackground != null
            ? popupBackground.color
            : Color.white;

        Vector2 startPosition = popupRoot.anchoredPosition;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = fadeDuration > 0f
                ? Mathf.Clamp01(elapsed / fadeDuration)
                : 1f;

            float alpha = 1f - t;

            if (comboText != null)
            {
                comboText.color = new Color(
                    comboStart.r,
                    comboStart.g,
                    comboStart.b,
                    comboStart.a * alpha
                );
            }

            if (scoreText != null)
            {
                scoreText.color = new Color(
                    scoreStart.r,
                    scoreStart.g,
                    scoreStart.b,
                    scoreStart.a * alpha
                );
            }

            if (popupBackground != null)
            {
                popupBackground.color = new Color(
                    backgroundStart.r,
                    backgroundStart.g,
                    backgroundStart.b,
                    backgroundStart.a * alpha
                );
            }

            popupRoot.anchoredPosition =
                startPosition + Vector2.up * (floatHeight * t);

            yield return null;
        }

        fadeCoroutine = null;
        HidePopup();
    }

    public void ResetCombo()
    {
        if (currentCombo <= 0)
            return;

        EndCombo();
    }

    private void RestorePopupAlpha()
    {
        if (comboText != null)
        {
            Color c = comboText.color;
            comboText.color = new Color(c.r, c.g, c.b, 1f);
        }

        if (scoreText != null)
        {
            Color c = scoreText.color;
            scoreText.color = new Color(c.r, c.g, c.b, 1f);
        }

        if (popupBackground != null)
        {
            Color c = popupBackground.color;
            popupBackground.color = new Color(c.r, c.g, c.b, 1f);
        }
    }

    private void HidePopup()
    {
        if (popupRoot == null)
            return;

        popupRoot.gameObject.SetActive(false);
        popupRoot.anchoredPosition = originalPosition;
        popupRoot.localScale = originalScale;

        RestorePopupAlpha();
    }
    // เรียกเมื่อผู้เล่นตีบอสโดน
    public void RegisterBossHit(BossMosquito boss)
    {
        if (boss == null)
            return;

        // ไม่เพิ่ม Combo ระหว่าง Slot Event
        if (SlotEventManager.Instance != null &&
            SlotEventManager.Instance.IsEventRunning)
        {
            return;
        }

        // เพิ่ม Combo
        currentCombo++;

        // รีเซ็ตเวลานับถอยหลัง Combo
        comboTimer = comboTimeLimit;

        // คำนวณตัวคูณคะแนน
        int multiplier = GetMultiplier(currentCombo);

        int gainedScore = 0;

        // เพิ่มคะแนน
        if (ScoreManager.Instance != null)
        {
            gainedScore = ScoreManager.Instance.AddScore(multiplier);
        }

        // แสดง Popup ที่ตำแหน่งบอส
        ShowPopup(boss.transform.position, gainedScore);
    }
}
