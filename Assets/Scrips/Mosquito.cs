using UnityEngine;

public class Mosquito : MonoBehaviour
{
    public enum MosquitoState
    {
        Flying,
        Attacking,
        Dead
    }

    [Header("State")]
    public MosquitoState state = MosquitoState.Flying;

    [Header("Movement")]
    public float flySpeed = 3f;
    public float wobbleAmount = 0.5f;
    public float wobbleSpeed = 3f;

    [Header("Special Mosquito (ยุงทอง)")]
    public bool isSpecial = false;
    public Color specialColor = Color.yellow;
    public float specialScale = 1.3f;

    [Header("Glass Pierce (นานๆ ทีถึงจะเจาะ / เฉพาะยุงธรรมดา)")]
    public bool canPierce = true;

    [Range(0f, 1f)]
    [Tooltip("โอกาสที่จะหยุดเจาะกระจก ตอนบินถึงจุดหมายแต่ละครั้ง (0.08 = 8%)")]
    public float pierceChance = 0.08f;

    [Tooltip("ต้องบินมาแล้วอย่างน้อยกี่วินาทีถึงจะเริ่มเจาะได้")]
    public float minFlyTimeBeforePierce = 2f;

    [Tooltip("เว้นช่วงหลังเจาะเสร็จ ก่อนจะเจาะอีกได้")]
    public float pierceCooldown = 6f;

    [Tooltip("เวลาที่ยุงอยู่นิ่งก่อนเจาะ (วินาที) ตีให้ตายก่อนหมดเวลาจะไม่โดนเจาะ")]
    public float pierceDuration = 3f;

    [Min(1)] public int pierceDamage = 1;

    [Tooltip("จำนวนยุงที่เจาะพร้อมกันได้สูงสุดทั้งฉาก")]
    public int maxPiercingAtOnce = 1;

    [Tooltip("ติ๊ก = ยุงหายไปหลังเจาะเสร็จ / ไม่ติ๊ก = บินต่อ")]
    public bool dieAfterPierce = false;

    [Header("Warning Overlay")]
    [Tooltip("ปล่อยว่างได้ โค้ดจะสร้างไอคอน ! สีแดงให้อัตโนมัติ")]
    public GameObject warningPrefab;
    public Vector3 warningOffset = new Vector3(0f, 0.8f, 0f);
    public float warningScale = 0.6f;
    public float pulseSpeed = 8f;
    public float pulseAmount = 0.15f;

    private Vector3 targetPosition;
    private SpriteRenderer spriteRenderer;
    private float randomOffset;
    private Vector3 originalScale;

    private bool alreadyHit;

    private float flyTimer;
    private float nextPierceTime;
    private float attackTimer;
    private float pulsePhase;
    private bool countedAsPiercer;

    private GameObject warningObject;
    private BreakableGlass pierceTarget;

    // จำนวนยุงที่กำลังเจาะอยู่ทั้งฉาก
    private static int activePiercers;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        activePiercers = 0;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
        randomOffset = Random.Range(0f, 100f);
    }

    private void Start()
    {
        PickNewTarget();
    }

    private void Update()
    {
        if (state == MosquitoState.Dead)
            return;

        if (state == MosquitoState.Flying)
        {
            flyTimer += Time.deltaTime;
            FlyAround();
        }
        else if (state == MosquitoState.Attacking)
        {
            UpdateAttack();
        }
    }

    // ---------------- บิน ----------------

    private void FlyAround()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            flySpeed * Time.deltaTime
        );

        float wobble = Mathf.Sin((Time.time + randomOffset) * wobbleSpeed) * wobbleAmount;

        Vector3 pos = transform.position;
        pos.y += wobble * Time.deltaTime;
        transform.position = pos;

        // ถึงจุดหมาย: มีโอกาสหยุดเจาะกระจก ไม่งั้นสุ่มจุดใหม่
        if (Vector3.Distance(transform.position, targetPosition) < 0.2f)
        {
            if (!TryStartPierce())
                PickNewTarget();
        }
    }

    private void PickNewTarget()
    {
        Camera cam = Camera.main;

        if (cam == null)
        {
            targetPosition = transform.position;
            return;
        }

        float distance = Mathf.Abs(cam.transform.position.z - transform.position.z);

        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, distance));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, distance));

        targetPosition = new Vector3(
            Random.Range(bottomLeft.x, topRight.x),
            Random.Range(bottomLeft.y, topRight.y),
            transform.position.z
        );
    }

    // ---------------- เจาะกระจก ----------------

    private bool TryStartPierce()
    {
        // ยุงทองไม่เจาะกระจก
        if (!canPierce || isSpecial)
            return false;

        if (flyTimer < minFlyTimeBeforePierce || Time.time < nextPierceTime)
            return false;

        if (activePiercers >= maxPiercingAtOnce)
            return false;

        if (Random.value > pierceChance)
            return false;

        // เจาะได้เฉพาะตอนที่ยุงอยู่บน collider ของกระจก
        BreakableGlass g = FindGlassUnderMosquito();

        if (g == null)
            return false;

        StartPierce(g);
        return true;
    }

    private void StartPierce(BreakableGlass target)
    {
        pierceTarget = target;
        state = MosquitoState.Attacking;
        attackTimer = 0f;
        pulsePhase = 0f;

        activePiercers++;
        countedAsPiercer = true;

        ShowWarning();
    }

    private void UpdateAttack()
    {
        // กระจกที่กำลังเจาะแตก/หายไปแล้ว (เช่น ผู้เล่นเผลอตีแตกเอง) ยกเลิกการเจาะ
        if (pierceTarget == null || pierceTarget.IsBroken)
        {
            CancelPierce();
            return;
        }

        attackTimer += Time.deltaTime;

        // ไอคอนกะพริบเร็วขึ้นเรื่อยๆ ยิ่งใกล้เจาะ
        float progress = Mathf.Clamp01(attackTimer / pierceDuration);
        pulsePhase += Time.deltaTime * pulseSpeed * (1f + progress * 2f);

        if (warningObject != null)
        {
            float s = warningScale * (1f + Mathf.Sin(pulsePhase) * pulseAmount);
            warningObject.transform.localScale = Vector3.one * s;
        }

        if (attackTimer >= pierceDuration)
            FinishPierce();
    }

    private void FinishPierce()
    {
        ReleasePiercer();
        HideWarning();

        if (pierceTarget != null && !pierceTarget.IsBroken)
            pierceTarget.TakeDamage(pierceDamage, transform.position);

        pierceTarget = null;

        if (dieAfterPierce)
        {
            state = MosquitoState.Dead;
            Destroy(gameObject);
            return;
        }

        state = MosquitoState.Flying;
        nextPierceTime = Time.time + pierceCooldown;
        PickNewTarget();
    }

    private void ReleasePiercer()
    {
        if (!countedAsPiercer)
            return;

        activePiercers = Mathf.Max(0, activePiercers - 1);
        countedAsPiercer = false;
    }

    // หากระจกที่ collider ครอบตำแหน่งของยุงอยู่ตอนนี้
    private BreakableGlass FindGlassUnderMosquito()
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            BreakableGlass g = hit.GetComponent<BreakableGlass>();

            if (g != null && !g.IsBroken)
                return g;
        }

        return null;
    }

    private void CancelPierce()
    {
        ReleasePiercer();
        HideWarning();
        pierceTarget = null;

        state = MosquitoState.Flying;
        nextPierceTime = Time.time + pierceCooldown;
        PickNewTarget();
    }

    // ---------------- Overlay แจ้งเตือน ----------------

    private void ShowWarning()
    {
        if (warningObject != null)
            return;

        if (warningPrefab != null)
        {
            warningObject = Instantiate(warningPrefab, transform);
            warningObject.transform.localPosition = warningOffset;
        }
        else
        {
            warningObject = new GameObject("PierceWarning");
            warningObject.transform.SetParent(transform, false);
            warningObject.transform.localPosition = warningOffset;

            SpriteRenderer sr = warningObject.AddComponent<SpriteRenderer>();
            sr.sprite = GetDefaultWarningSprite();

            if (spriteRenderer != null)
            {
                sr.sortingLayerID = spriteRenderer.sortingLayerID;
                sr.sortingOrder = spriteRenderer.sortingOrder + 10;
            }
        }

        warningObject.transform.localScale = Vector3.one * warningScale;
    }

    private void HideWarning()
    {
        if (warningObject != null)
        {
            Destroy(warningObject);
            warningObject = null;
        }
    }

    // วาดไอคอน ! สีแดงด้วยโค้ด ไม่ต้องใช้รูปภาพ
    private static Sprite defaultWarningSprite;

    private static Sprite GetDefaultWarningSprite()
    {
        if (defaultWarningSprite != null)
            return defaultWarningSprite;

        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 red = new Color32(220, 30, 30, 255);
        Color32 white = new Color32(255, 255, 255, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - 15.5f;
                float dy = y - 15.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                Color32 c = clear;

                if (dist <= 15.5f)
                    c = dist >= 13.5f ? white : red;

                // เครื่องหมาย !
                bool bar = x >= 14 && x <= 17 && y >= 11 && y <= 24;
                bool dot = x >= 14 && x <= 17 && y >= 6 && y <= 9;

                if (bar || dot)
                    c = white;

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();

        defaultWarningSprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            32f
        );

        return defaultWarningSprite;
    }

    // ---------------- อื่นๆ ----------------

    // Spawner เรียกฟังก์ชันนี้ตอนเสกยุง
    public void Setup(bool special)
    {
        isSpecial = special;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (isSpecial)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = specialColor;

            transform.localScale = originalScale * specialScale;
        }
        else
        {
            if (spriteRenderer != null)
                spriteRenderer.color = Color.white;

            transform.localScale = originalScale;
        }
    }

    private void OnMouseDown()
    {
        if (alreadyHit || state == MosquitoState.Dead)
            return;

        alreadyHit = true;
        state = MosquitoState.Dead;

        ReleasePiercer();
        HideWarning();

        // เพิ่มคะแนนและ Combo เมื่อคลิกโดนยุง
        if (ComboScoreManager.Instance != null)
        {
            ComboScoreManager.Instance.RegisterMosquitoHit();
        }
        else
        {
            Debug.LogError("ไม่พบ ComboScoreManager ใน Scene!");
        }

        // เปิด Slot Event เมื่อเป็นยุงพิเศษ
        if (isSpecial && SlotEventManager.Instance != null)
        {
            SlotEventManager.Instance.OpenEvent();
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        ReleasePiercer();
    }
}