using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform slotRect;
    [SerializeField] private Image icon;

    [Header("Slot Height")]
    [SerializeField] private float selectedHeight = 150f;
    [SerializeField] private float unselectedHeight = 70f;

    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.25f;

    private Coroutine resizeCoroutine;

    private void Awake()
    {
        if (slotRect == null)
            slotRect = GetComponent<RectTransform>();

        // ยึดขอบล่างไว้ แล้วขยายขึ้นด้านบน
        if (slotRect != null)
        {
            Vector2 pivot = slotRect.pivot;
            pivot.y = 0f;
            slotRect.pivot = pivot;
        }
    }

    public void SetSelected(bool selected)
    {
        if (slotRect == null)
            slotRect = GetComponent<RectTransform>();

        if (slotRect == null)
        {
            Debug.LogError("EquipmentSlotUI: ไม่พบ RectTransform", this);
            return;
        }

        float targetHeight = selected
            ? selectedHeight
            : unselectedHeight;

        if (resizeCoroutine != null)
            StopCoroutine(resizeCoroutine);

        resizeCoroutine = StartCoroutine(AnimateHeight(targetHeight));
    }

    private IEnumerator AnimateHeight(float targetHeight)
    {
        float startHeight = slotRect.rect.height;
        float elapsed = 0f;

        if (animationDuration <= 0f)
        {
            SetHeight(targetHeight);
            resizeCoroutine = null;
            yield break;
        }

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / animationDuration);
            t = t * t * (3f - 2f * t);

            SetHeight(Mathf.Lerp(startHeight, targetHeight, t));

            yield return null;
        }

        SetHeight(targetHeight);
        resizeCoroutine = null;
    }

    private void SetHeight(float height)
    {
        Vector2 size = slotRect.sizeDelta;
        size.y = height;
        slotRect.sizeDelta = size;
    }

    public void SetIcon(Sprite sprite)
    {
        if (icon != null)
            icon.sprite = sprite;
    }
}