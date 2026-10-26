using UnityEngine;
using UnityEngine.UI;

public class RepairProgressUI : MonoBehaviour
{
    public static RepairProgressUI Instance { get; private set; }

    [Header("Prefab")]
    [SerializeField] private GameObject repairProgressPrefab;

    [Header("Position")]
    [SerializeField] private Vector2 mouseOffset = Vector2.zero;

    private GameObject progressObject;
    private Image progressImage;
    private RectTransform progressRect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CreateProgressUI();
    }

    private void CreateProgressUI()
    {
        if (repairProgressPrefab == null)
        {
            Debug.LogError("กรุณาใส่ RepairProgress Prefab ใน Inspector");
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("ไม่พบ Canvas ใน Scene");
            return;
        }

        progressObject = Instantiate(repairProgressPrefab, canvas.transform);
        progressObject.name = "RepairProgress";

        Transform ring = progressObject.transform.Find("ProgressRing");

        if (ring == null)
        {
            Debug.LogError("ไม่พบ ProgressRing ใน Prefab");
            return;
        }

        progressImage = ring.GetComponent<Image>();
        progressRect = ring.GetComponent<RectTransform>();

        if (progressImage == null)
        {
            Debug.LogError("ProgressRing ต้องมี Image Component");
            return;
        }

        progressImage.type = Image.Type.Filled;
        progressImage.fillMethod = Image.FillMethod.Radial360;
        progressImage.fillOrigin = (int)Image.Origin360.Top;
        progressImage.fillClockwise = true;
        progressImage.fillAmount = 0f;
        progressImage.raycastTarget = false;

        progressObject.SetActive(false);
    }

    private void Update()
    {
        if (progressObject == null || !progressObject.activeSelf)
            return;

        RectTransform rootRect =
            progressObject.GetComponent<RectTransform>();

        if (rootRect != null)
        {
            rootRect.position =
                (Vector2)Input.mousePosition + mouseOffset;
        }
    }

    public void Show()
    {
        if (progressObject == null || progressImage == null)
            return;

        progressImage.fillAmount = 0f;
        progressObject.SetActive(true);
    }

    public void SetProgress(float value)
    {
        if (progressImage == null)
            return;

        progressImage.fillAmount = Mathf.Clamp01(value);
    }

    public void Hide()
    {
        if (progressObject == null || progressImage == null)
            return;

        progressImage.fillAmount = 0f;
        progressObject.SetActive(false);
    }
}
