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

    private bool eventRunning;
    private bool spinning;
    private float previousTimeScale = 1f;

    private Sprite result1;
    private Sprite result2;
    private Sprite result3;

    private Coroutine spinCoroutine;

    public bool IsEventRunning => eventRunning;
    public bool IsSpinning => spinning;

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

        // เริ่มเกมโดยอนุญาตให้คลิกยุงและกระจก
        SetGameInputBlocked(false);

        if (slotPanel != null)
            slotPanel.SetActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        SetLeverEnabled(false);
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

    // ล็อกหรือปลดล็อกการคลิกภายในเกม
    private void SetGameInputBlocked(bool blocked)
    {
        BreakableGlass.InputBlocked = blocked;
    }

    public void OpenEvent()
    {
        if (eventRunning)
            return;

        FindLeverController();

        eventRunning = true;
        spinning = false;

        // ล็อกการตีและคลิกวัตถุในเกมทันทีที่เปิด Slot
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

        Debug.Log("Slot Event เปิดแล้ว: ล็อกการตีและคลิกในเกม");
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

        // สุ่มผลลัพธ์ของแต่ละช่องล่วงหน้า
        result1 = symbols[Random.Range(0, symbols.Length)];
        result2 = symbols[Random.Range(0, symbols.Length)];
        result3 = symbols[Random.Range(0, symbols.Length)];

        // เริ่มหมุนทั้ง 3 ช่องพร้อมกัน
        Coroutine reel1Routine = StartCoroutine(
            SpinReel(reel1, result1, spinDuration)
        );

        Coroutine reel2Routine = StartCoroutine(
            SpinReel(reel2, result2, spinDuration + stopDelay)
        );

        Coroutine reel3Routine = StartCoroutine(
            SpinReel(reel3, result3, spinDuration + stopDelay * 2f)
        );

        // รอจนกว่าทั้ง 3 ช่องจะหมุนเสร็จ
        yield return reel1Routine;
        yield return reel2Routine;
        yield return reel3Routine;

        // แสดงผลเมื่อทุกช่องหยุดแล้วเท่านั้น
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

        // กำหนดภาพผลลัพธ์สุดท้าย
        reel.sprite = finalSprite;
    }

    private void ShowResult()
    {
        if (resultText == null)
            return;

        if (result1 == result2 && result2 == result3)
        {
            resultText.text = "JACKPOT!";
        }
        else if (
            result1 == result2 ||
            result2 == result3 ||
            result1 == result3)
        {
            resultText.text = "SMALL WIN!";
        }
        else
        {
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

        // ปลดล็อกให้กลับมาตียุงและกระจกได้
        SetGameInputBlocked(false);

        Debug.Log("Slot Event จบแล้ว: ปลดล็อกการตีและคลิกในเกม");
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
        if (Instance == this)
        {
            // ป้องกันสถานะล็อกค้างหาก Manager ถูกทำลาย
            SetGameInputBlocked(false);

            Time.timeScale = previousTimeScale;
            Instance = null;
        }
    }
}