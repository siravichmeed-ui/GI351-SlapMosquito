
using UnityEngine;
using UnityEngine.UI;

public class SlotLeverSlider : MonoBehaviour
{
    [Header("References")]
    public Slider leverSlider;
    public SlotEventManager slotManager;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float pullThreshold = 0.9f;

    private bool activated;

    private void Awake()
    {
        if (leverSlider == null)
            leverSlider = GetComponent<Slider>();
    }

    private void Start()
    {
        if (leverSlider == null)
        {
            Debug.LogError(
                "SlotLeverSlider: ไม่พบ Slider!"
            );

            enabled = false;
            return;
        }

        if (slotManager == null)
            slotManager = SlotEventManager.Instance;

        if (slotManager == null)
        {
            Debug.LogError(
                "SlotLeverSlider: ไม่พบ SlotEventManager!"
            );

            enabled = false;
            return;
        }

        leverSlider.minValue = 0f;
        leverSlider.maxValue = 1f;

        leverSlider.onValueChanged.AddListener(OnSliderChanged);

        // สำคัญ: ตรวจสอบสถานะ Event ปัจจุบัน
        // ไม่ปิด Slider ทิ้งโดยไม่ตรวจสอบ
        SetLeverEnabled(slotManager.IsEventRunning);
    }

    public void SetLeverEnabled(bool isEnabled)
    {
        activated = false;

        if (leverSlider == null)
            return;

        leverSlider.SetValueWithoutNotify(0f);
        leverSlider.interactable = isEnabled;
    }

    private void OnSliderChanged(float value)
    {
        if (activated)
            return;

        if (value < pullThreshold)
            return;

        if (slotManager == null || !slotManager.IsEventRunning)
            return;

        activated = true;
        leverSlider.interactable = false;

        slotManager.PullLever();
    }

    private void OnDestroy()
    {
        if (leverSlider != null)
        {
            leverSlider.onValueChanged.RemoveListener(
                OnSliderChanged
            );
        }
    }
}
