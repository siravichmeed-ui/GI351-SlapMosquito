using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SlotEventManager : MonoBehaviour
{
    public static SlotEventManager Instance { get; private set; }

    [Header("Event UI")]
    public GameObject slotPanel;
    public Button continueButton;
    public TMP_Text resultText;

    [Header("Lever Slider")]
    public Slider leverSlider;
    public SlotLeverSlider leverController;

    [Header("Slot Images")]
    public Image reel1;
    public Image reel2;
    public Image reel3;

    [Header("Slot Symbols")]
    public Sprite[] symbols;

    [Header("Spin Settings")]
    public float spinDuration = 2f;
    public float spinInterval = 0.08f;
    public float stopDelay = 0.5f;

    [Header("Win Settings")]
    [Range(0f, 100f)]
    [Tooltip("โอกาสชนะเป็นเปอร์เซ็นต์ เช่น 10 = 10%")]
    public float winRate = 10f;

    [Header("Reward Settings")]
    [Min(0f)]
    [Tooltip("ระยะเวลาที่กระจกไม่แตกหลังชนะ")]
    public float rewardDuration = 10f;

    [Header("Reward UI")]
    public TMP_Text rewardTimerText;

    [Header("Combo Popup")]
    [SerializeField] private GameObject comboPopupRoot;

    private bool eventRunning;
    private bool spinning;
    private bool pendingReward;
    private bool rewardActive;

    private float previousTimeScale = 1f;
    private float rewardEndTime;

    private Sprite result1;
    private Sprite result2;
    private Sprite result3;

    private Coroutine spinCoroutine;
    private Coroutine rewardCoroutine;

    public bool IsEventRunning => eventRunning;
    public bool IsSpinning => spinning;
    public bool IsRewardActive => rewardActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError(
                "พบ SlotEventManager มากกว่าหนึ่งตัวใน Scene!"
            );

            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        FindLeverController();

        SetGameInputBlocked(false);

        if (slotPanel != null)
            slotPanel.SetActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        if (rewardTimerText != null)
            rewardTimerText.gameObject.SetActive(false);

        SetLeverEnabled(false);
    }

    private void Update()
    {
        if (!rewardActive || rewardTimerText == null)
            return;

        float remaining = Mathf.Max(
            0f,
            rewardEndTime - Time.unscaledTime
        );

        rewardTimerText.text =
            "GLASS PROTECTION: " + remaining.ToString("0.0") + "s";
    }

    private void FindLeverController()
    {
        if (leverController != null)
            return;

        if (leverSlider != null)
        {
            leverController =
                leverSlider.GetComponent<SlotLeverSlider>();

            if (leverController == null)
            {
                leverController =
                    leverSlider.GetComponentInParent<SlotLeverSlider>();
            }
        }
    }

    private void SetGameInputBlocked(bool blocked)
    {
        BreakableGlass.InputBlocked = blocked;
    }

    public void OpenEvent()
    {
        if (eventRunning || rewardActive)
            return;

        FindLeverController();

        eventRunning = true;
        spinning = false;
        pendingReward = false;

        // ซ่อน Combo Popup เมื่อเข้า Slot Event
        if (comboPopupRoot != null)
        {
            comboPopupRoot.SetActive(false);
        }

        SetGameInputBlocked(true);

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (slotPanel != null)
            slotPanel.SetActive(true);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        if (resultText != null)
            resultText.text = "PULL THE LEVER!";

        SetLeverEnabled(true);

        Debug.Log("Slot Event เปิดแล้ว");
    }

    public void PullLever()
    {
        if (!eventRunning || spinning)
            return;

        if (symbols == null || symbols.Length == 0)
        {
            Debug.LogError(
                "SlotEventManager: กรุณาใส่ Sprite ใน Symbols"
            );

            if (resultText != null)
                resultText.text = "NO SLOT SYMBOLS!";

            SetLeverEnabled(true);
            return;
        }

        if (symbols.Length < 2)
        {
            Debug.LogError(
                "SlotEventManager: ต้องมี Sprite อย่างน้อย 2 รูป"
            );

            if (resultText != null)
                resultText.text = "NEED AT LEAST 2 SYMBOLS!";

            SetLeverEnabled(true);
            return;
        }

        spinning = true;

        SetLeverEnabled(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        if (spinCoroutine != null)
            StopCoroutine(spinCoroutine);

        spinCoroutine = StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        if (resultText != null)
            resultText.text = "SPINNING...";

        // สุ่มว่ารอบนี้จะชนะหรือไม่
        bool isWin = Random.value * 100f < winRate;

        if (isWin)
        {
            // ชนะ: ทั้งสามช่องต้องเป็นรูปเดียวกัน
            Sprite winningSymbol =
                symbols[Random.Range(0, symbols.Length)];

            result1 = winningSymbol;
            result2 = winningSymbol;
            result3 = winningSymbol;
        }
        else
        {
            // แพ้: สุ่มใหม่จนกว่าจะไม่ใช่สามรูปเดียวกัน
            do
            {
                result1 = symbols[
                    Random.Range(0, symbols.Length)
                ];

                result2 = symbols[
                    Random.Range(0, symbols.Length)
                ];

                result3 = symbols[
                    Random.Range(0, symbols.Length)
                ];
            }
            while (
                result1 == result2 &&
                result2 == result3
            );
        }

        Coroutine reel1Routine = StartCoroutine(
            SpinReel(reel1, result1, spinDuration)
        );

        Coroutine reel2Routine = StartCoroutine(
            SpinReel(reel2, result2, spinDuration + stopDelay)
        );

        Coroutine reel3Routine = StartCoroutine(
            SpinReel(
                reel3,
                result3,
                spinDuration + stopDelay * 2f
            )
        );

        yield return reel1Routine;
        yield return reel2Routine;
        yield return reel3Routine;

        ShowResult();

        spinning = false;
        spinCoroutine = null;

        if (continueButton != null)
            continueButton.gameObject.SetActive(true);
    }

    private IEnumerator SpinReel(
        Image reel,
        Sprite finalSprite,
        float duration)
    {
        if (reel == null)
            yield break;

        float elapsed = 0f;
        float timer = 0f;

        while (elapsed < duration)
        {
            float delta = Time.unscaledDeltaTime;

            elapsed += delta;
            timer += delta;

            if (timer >= spinInterval)
            {
                timer = 0f;

                reel.sprite = symbols[
                    Random.Range(0, symbols.Length)
                ];
            }

            yield return null;
        }

        reel.sprite = finalSprite;
    }

    private void ShowResult()
    {
        bool isWin =
            result1 != null &&
            result1 == result2 &&
            result2 == result3;

        if (isWin)
        {
            pendingReward = true;

            if (resultText != null)
            {
                resultText.text =
                    "JACKPOT!\nGLASS PROTECTED FOR " +
                    rewardDuration.ToString("0") + " SECONDS!";
            }

            Debug.Log("Slot ชนะ! ได้รับสิทธิ์ป้องกันกระจก");
        }
        else
        {
            pendingReward = false;

            if (resultText != null)
                resultText.text = "TRY AGAIN!";
        }
    }

    public void ContinueGame()
    {
        if (!eventRunning)
            return;

        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
            spinCoroutine = null;
        }

        eventRunning = false;
        spinning = false;

        SetLeverEnabled(false);

        if (slotPanel != null)
            slotPanel.SetActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        // คืนความเร็วเกม
        Time.timeScale = previousTimeScale;

        if (pendingReward)
        {
            pendingReward = false;
            ActivateReward();
        }
        else
        {
            SetGameInputBlocked(false);
        }

        Debug.Log("Slot Event จบแล้ว");
    }

    private void ActivateReward()
    {
        rewardActive = true;

        // เปิดให้คลิกวัตถุในเกมได้
        SetGameInputBlocked(false);

        // เปิดสิทธิ์ป้องกันกระจกแตก
        BreakableGlass.GlassProtected = true;

        rewardEndTime =
            Time.unscaledTime + rewardDuration;

        if (rewardTimerText != null)
        {
            rewardTimerText.gameObject.SetActive(true);
        }

        if (rewardCoroutine != null)
            StopCoroutine(rewardCoroutine);

        rewardCoroutine = StartCoroutine(RewardRoutine());

        Debug.Log(
            "ได้รับรางวัล: กระจกไม่แตกเป็นเวลา " +
            rewardDuration + " วินาที"
        );
    }

    private IEnumerator RewardRoutine()
    {
        // ใช้เวลาจริง แม้ Time.timeScale จะเป็น 0
        yield return new WaitForSecondsRealtime(rewardDuration);

        BreakableGlass.GlassProtected = false;

        rewardActive = false;
        rewardCoroutine = null;

        if (rewardTimerText != null)
        {
            rewardTimerText.gameObject.SetActive(false);
        }

        Debug.Log("หมดเวลาป้องกันกระจกแล้ว");
    }

    private void SetLeverEnabled(bool isEnabled)
    {
        FindLeverController();

        if (leverController != null)
        {
            leverController.SetLeverEnabled(isEnabled);
        }
        else if (leverSlider != null)
        {
            leverSlider.SetValueWithoutNotify(0f);
            leverSlider.interactable = isEnabled;
        }
        else
        {
            Debug.LogWarning(
                "SlotEventManager: ยังไม่ได้กำหนด Lever Slider"
            );
        }
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        // ป้องกันสถานะป้องกันกระจกค้าง
        BreakableGlass.GlassProtected = false;
        BreakableGlass.InputBlocked = false;

        Time.timeScale = previousTimeScale;

        Instance = null;
    }
}
