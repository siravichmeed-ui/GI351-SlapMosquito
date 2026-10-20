
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ComboScoreManager : MonoBehaviour
{
    public static ComboScoreManager Instance { get; private set; }

    [Header("Score Settings")]
    public int baseScore = 10;

    [Header("Combo Settings")]
    public float comboTimeLimit = 2f;

    [Header("Miss Click Settings")]
    public bool resetComboOnMissClick = true;

    [Header("Score UI")]
    public TMP_Text scoreText;

    [Header("Combo UI")]
    public TMP_Text comboText;
    public Slider comboTimerSlider;

    [Header("Combo Timer Colors")]
    public Image comboTimerFill;
    public Color safeColor = Color.green;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;

    [Range(0f, 1f)]
    public float warningThreshold = 0.5f;

    [Range(0f, 1f)]
    public float dangerThreshold = 0.25f;

    private int score;
    private int combo;
    private float comboTimer;

    // เก็บเฟรมล่าสุดที่คลิกโดนยุง
    private int lastMosquitoHitFrame = -1;

    public int Score => score;
    public int Combo => combo;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError(
                "พบ ComboScoreManager มากกว่าหนึ่งตัวใน Scene!"
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (comboTimerSlider != null)
        {
            comboTimerSlider.minValue = 0f;
            comboTimerSlider.maxValue = comboTimeLimit;
            comboTimerSlider.SetValueWithoutNotify(0f);
        }

        UpdateScoreUI();
        UpdateComboUI();
        UpdateTimerUI();
    }

    private void Update()
    {
        // ไม่ตรวจคลิกระหว่าง Slot Event
        if (SlotEventManager.Instance != null &&
            SlotEventManager.Instance.IsEventRunning)
        {
            return;
        }

        // ตรวจการคลิกเมาส์ซ้าย
        if (resetComboOnMissClick &&
            Input.GetMouseButtonDown(0))
        {
            bool pointerOverUI =
                EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject();

            if (!pointerOverUI)
            {
                int clickedFrame = Time.frameCount;

                StartCoroutine(
                    CheckMissClickAtEndOfFrame(clickedFrame)
                );
            }
        }

        // นับเวลาของ Combo
        if (combo > 0)
        {
            comboTimer -= Time.deltaTime;

            if (comboTimer <= 0f)
            {
                ResetCombo();
            }
            else
            {
                UpdateTimerUI();
            }
        }
    }

    private IEnumerator CheckMissClickAtEndOfFrame(int clickedFrame)
    {
        // รอให้ OnMouseDown ของ Mosquito ทำงานก่อน
        yield return new WaitForEndOfFrame();

        // ถ้าเฟรมเดียวกันมีการตบยุงสำเร็จ ถือว่าไม่พลาด
        if (lastMosquitoHitFrame == clickedFrame)
        {
            yield break;
        }

        // ถ้าเป็นคลิกพลาดจริง ให้รีเซ็ต Combo
        ResetCombo();

        Debug.Log("คลิกพลาด: Combo ถูกรีเซ็ต");
    }

    public void RegisterMosquitoHit()
    {
        // บันทึกเฟรมที่คลิกโดนยุง
        lastMosquitoHitFrame = Time.frameCount;

        combo++;
        comboTimer = comboTimeLimit;

        int earnedScore = baseScore * combo;
        score += earnedScore;

        UpdateScoreUI();
        UpdateComboUI();
        UpdateTimerUI();

        Debug.Log(
            "COMBO x" + combo +
            " | ได้ " + earnedScore +
            " คะแนน | คะแนนรวม " + score
        );
    }

    private void ResetCombo()
    {
        combo = 0;
        comboTimer = 0f;

        UpdateComboUI();
        UpdateTimerUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "SCORE: " + score;
        }
    }

    private void UpdateComboUI()
    {
        if (comboText == null)
            return;

        if (combo > 0)
        {
            comboText.gameObject.SetActive(true);
            comboText.text = "COMBO x" + combo;
        }
        else
        {
            comboText.text = "";
            comboText.gameObject.SetActive(false);
        }
    }

    private void UpdateTimerUI()
    {
        if (comboTimerSlider != null)
        {
            comboTimerSlider.maxValue = comboTimeLimit;

            comboTimerSlider.SetValueWithoutNotify(
                Mathf.Clamp(comboTimer, 0f, comboTimeLimit)
            );
        }

        if (comboTimerFill != null)
        {
            float normalizedTime = comboTimeLimit > 0f
                ? comboTimer / comboTimeLimit
                : 0f;

            if (normalizedTime <= dangerThreshold)
            {
                comboTimerFill.color = dangerColor;
            }
            else if (normalizedTime <= warningThreshold)
            {
                comboTimerFill.color = warningColor;
            }
            else
            {
                comboTimerFill.color = safeColor;
            }
        }
    }

    public void ResetAllScore()
    {
        score = 0;
        combo = 0;
        comboTimer = 0f;
        lastMosquitoHitFrame = -1;

        UpdateScoreUI();
        UpdateComboUI();
        UpdateTimerUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
