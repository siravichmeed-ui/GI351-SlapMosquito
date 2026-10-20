using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[System.Serializable]
public class CrackStage
{
    [Tooltip("ภาพกระจกตอนร้าวระดับนี้ (ภาพกระจกทั้งบาน)")]
    public Sprite sprite;

    [Min(0)]
    [Tooltip("แสดงภาพนี้เมื่อ HP เหลือ 'เท่ากับหรือน้อยกว่า' ค่านี้")]
    public int showWhenHPAtOrBelow = 1;
}

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class BreakableGlass : MonoBehaviour
{
    [Header("HP")]
    [Min(1)] public int maxHP = 3;
    [Min(1)] public int damagePerClick = 1;

    [Header("Crack Stages (ภาพเริ่มต้นใช้จาก Sprite Renderer)")]
    public CrackStage[] crackStages;

    [Header("Click Crack (รอยร้าวขึ้นตรงจุดที่กด)")]
    public bool spawnCrackOnHit = true;

    [Tooltip("ภาพรอยร้าว PNG โปร่งใส (ใส่กี่ภาพก็ได้ สุ่มใช้) ถ้าเว้นว่าง โค้ดจะวาดรอยร้าวให้เอง")]
    public Sprite[] crackDecals;

    [Tooltip("ขนาดรอยร้าวในโลกเกม (สุ่มระหว่างสองค่านี้)")]
    public Vector2 decalSizeRange = new Vector2(1f, 1.8f);

    public bool randomRotation = true;

    [Tooltip("Order in Layer ของรอยร้าว = ของกระจก + ค่านี้ (ต้องต่ำกว่ายุง)")]
    public int decalSortingOffset = 1;

    [Header("Shake")]
    public float shakeDuration = 0.12f;
    public float shakeMagnitude = 0.08f;

    [Header("Break")]
    [Tooltip("ติ๊ก = ลบกระจกทิ้งตอนแตก / ไม่ติ๊ก = ซ่อนไว้เฉยๆ")]
    public bool destroyOnBreak = true;

    [Header("Depth (ให้ยุงอยู่หน้ากระจกเสมอ)")]
    [Tooltip("ติ๊ก = บังคับให้กระจกอยู่ไกลกล้องกว่า เพื่อให้คลิกที่ยุงไม่ทะลุมาโดนกระจก")]
    public bool setGlassDepth = true;

    [Tooltip("ค่า Z ของกระจก ต้องมากกว่า Z ของยุง (ยุงควรอยู่ที่ Z = 0)")]
    public float glassZ = 1f;

    [Header("Events")]
    public UnityEvent onHit;
    public UnityEvent onBreak;

    private int currentHP;
    private bool isBroken;

    private SpriteRenderer glassRenderer;
    private Collider2D glassCollider;
    private Sprite originalSprite;

    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;

    private readonly List<GameObject> decals = new List<GameObject>();
    private Camera mainCamera;

    // เปิดเป็น true ตอนมี event/popup เพื่อไม่ให้กระจกรับคลิกเลย
    // เช่น SlotEvent เริ่ม -> BreakableGlass.InputBlocked = true; จบ -> false
    public static bool InputBlocked = false;

    public int CurrentHP => currentHP;
    public bool IsBroken => isBroken;

    private void Awake()
    {
        glassRenderer = GetComponent<SpriteRenderer>();
        glassCollider = GetComponent<Collider2D>();
        originalSprite = glassRenderer.sprite;
        originalPosition = transform.localPosition;

        currentHP = maxHP;

        if (setGlassDepth)
        {
            Vector3 pos = transform.position;
            pos.z = glassZ;
            transform.position = pos;
            originalPosition = transform.localPosition;
        }
    }

    // Unity ส่งคลิกให้ collider ที่อยู่หน้าสุดเพียงตัวเดียว
    // ถ้ายุงอยู่หน้ากระจก (Z น้อยกว่า) คลิกจะไปที่ยุงอย่างเดียว กระจกไม่โดน
    private void OnMouseDown()
    {
        if (InputBlocked)
            return;

        // คลิกโดน UI (เช่น popup Slot Event, ปุ่ม, คันโยก) ไม่นับเป็นการตีกระจก
        if (IsPointerOverUI())
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        Vector3 clickPoint = mainCamera != null
            ? mainCamera.ScreenToWorldPoint(Input.mousePosition)
            : transform.position;

        TakeDamage(damagePerClick, clickPoint);
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        // รองรับการแตะบนมือถือ
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;
        }

        return false;
    }

    // ดาเมจแบบไม่ระบุจุด (เช่น ยุงเจาะ) รอยร้าวจะขึ้นจุดสุ่มบนกระจก
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, RandomPointOnGlass());
    }

    // ดาเมจแบบระบุจุด รอยร้าวจะขึ้นตรงจุดนั้น
    public void TakeDamage(int damage, Vector3 worldPoint)
    {
        if (isBroken || damage <= 0)
            return;

        currentHP = Mathf.Max(currentHP - damage, 0);

        if (spawnCrackOnHit)
            SpawnCrackDecal(worldPoint);

        UpdateCrack();
        onHit?.Invoke();

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            transform.localPosition = originalPosition;
        }
        shakeCoroutine = StartCoroutine(Shake());

        Debug.Log("Glass HP: " + currentHP + "/" + maxHP);

        if (currentHP <= 0)
            BreakGlass();
    }

    // ตั้ง HP ตรงๆ (เรียกจากสคริปต์อื่นได้)
    public void SetHP(int hp)
    {
        if (isBroken)
            return;

        currentHP = Mathf.Clamp(hp, 1, maxHP);
        UpdateCrack();
    }

    public void Heal(int amount)
    {
        SetHP(currentHP + amount);
    }

    // รีเซ็ตกระจกกลับเป็นสภาพเต็ม
    public void ResetGlass()
    {
        StopAllCoroutines();
        shakeCoroutine = null;

        isBroken = false;
        currentHP = maxHP;
        ClearDecals();

        transform.localPosition = originalPosition;
        glassRenderer.enabled = true;
        glassCollider.enabled = true;
        glassRenderer.sprite = originalSprite;
    }

    // เลือกภาพที่ตรงกับ HP ปัจจุบัน:
    // ในบรรดา stage ที่ HP ต่ำกว่าหรือเท่ากับเงื่อนไข ใช้ตัวที่เงื่อนไขต่ำสุด
    private void UpdateCrack()
    {
        Sprite chosen = originalSprite;
        int bestThreshold = int.MaxValue;

        if (crackStages != null)
        {
            foreach (CrackStage stage in crackStages)
            {
                if (stage == null || stage.sprite == null)
                    continue;

                if (currentHP <= stage.showWhenHPAtOrBelow
                    && stage.showWhenHPAtOrBelow < bestThreshold)
                {
                    bestThreshold = stage.showWhenHPAtOrBelow;
                    chosen = stage.sprite;
                }
            }
        }

        glassRenderer.sprite = chosen;
    }

    // คลิกขวาที่หัวคอมโพเนนต์ -> Auto Fill Thresholds
    // กระจายค่า HP ของแต่ละ stage ให้เท่ากันตามจำนวนภาพ
    [ContextMenu("Auto Fill Thresholds")]
    private void AutoFillThresholds()
    {
        if (crackStages == null || crackStages.Length == 0)
            return;

        int n = crackStages.Length;

        for (int i = 0; i < n; i++)
        {
            int t = Mathf.RoundToInt(maxHP * (n - i) / (n + 1f));
            crackStages[i].showWhenHPAtOrBelow = Mathf.Clamp(t, 1, Mathf.Max(1, maxHP - 1));
        }
    }

    // ---------------- รอยร้าวตรงจุดที่กด ----------------

    private Vector3 RandomPointOnGlass()
    {
        Bounds b = glassCollider != null ? glassCollider.bounds : new Bounds(transform.position, Vector3.zero);

        return new Vector3(
            Random.Range(b.min.x, b.max.x),
            Random.Range(b.min.y, b.max.y),
            transform.position.z
        );
    }

    private void SpawnCrackDecal(Vector3 worldPoint)
    {
        Sprite sprite = PickDecalSprite();

        if (sprite == null)
            return;

        GameObject go = new GameObject("CrackDecal"); // ไม่มี Collider ไม่รบกวนการคลิก
        go.transform.SetParent(transform, true);
        go.transform.position = new Vector3(worldPoint.x, worldPoint.y, transform.position.z);

        if (randomRotation)
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        // คำนวณขนาดให้ได้ตามที่ต้องการในโลกเกม แม้กระจกจะถูกย่อ/ขยายอยู่
        float worldSize = Random.Range(decalSizeRange.x, decalSizeRange.y);
        float spriteWidth = Mathf.Max(0.0001f, sprite.bounds.size.x);
        float parentScale = Mathf.Max(0.0001f, transform.lossyScale.x);
        go.transform.localScale = Vector3.one * (worldSize / spriteWidth / parentScale);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerID = glassRenderer.sortingLayerID;
        sr.sortingOrder = glassRenderer.sortingOrder + decalSortingOffset;

        decals.Add(go);
    }

    private Sprite PickDecalSprite()
    {
        if (crackDecals != null && crackDecals.Length > 0)
        {
            // สุ่มจากภาพที่ใส่ไว้ (ข้ามช่องว่าง)
            for (int tries = 0; tries < 5; tries++)
            {
                Sprite s = crackDecals[Random.Range(0, crackDecals.Length)];
                if (s != null)
                    return s;
            }
            return null;
        }

        Sprite[] defaults = GetDefaultCracks();
        return defaults[Random.Range(0, defaults.Length)];
    }

    private void ClearDecals()
    {
        foreach (GameObject d in decals)
        {
            if (d != null)
                Destroy(d);
        }

        decals.Clear();
    }

    // วาดรอยร้าวด้วยโค้ด ใช้เมื่อไม่ได้ใส่ภาพเอง
    private static Sprite[] defaultCracks;

    private static Sprite[] GetDefaultCracks()
    {
        if (defaultCracks != null && defaultCracks[0] != null)
            return defaultCracks;

        defaultCracks = new Sprite[3];

        for (int i = 0; i < defaultCracks.Length; i++)
            defaultCracks[i] = CreateProceduralCrack(i);

        return defaultCracks;
    }

    private static Sprite CreateProceduralCrack(int seed)
    {
        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        tex.SetPixels32(pixels); // โปร่งใสทั้งหมด

        System.Random rng = new System.Random(seed * 7919 + 13);
        int rays = 6 + rng.Next(0, 4);

        for (int r = 0; r < rays; r++)
        {
            float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            int length = 30 + rng.Next(0, 30);
            DrawCrackLine(tex, size / 2f, size / 2f, angle, length, rng, true);
        }

        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    private static void DrawCrackLine(Texture2D tex, float x, float y, float angle,
        int length, System.Random rng, bool allowBranch)
    {
        for (int step = 0; step < length; step++)
        {
            x += Mathf.Cos(angle);
            y += Mathf.Sin(angle);
            angle += (float)(rng.NextDouble() - 0.5) * 0.5f;

            float fade = 1f - (step / (float)(length + 10)) * 0.6f;
            bool thick = step < length * 0.5f;

            PlotCrackPixel(tex, Mathf.RoundToInt(x), Mathf.RoundToInt(y), fade);

            if (thick)
            {
                PlotCrackPixel(tex, Mathf.RoundToInt(x) + 1, Mathf.RoundToInt(y), fade * 0.6f);
                PlotCrackPixel(tex, Mathf.RoundToInt(x), Mathf.RoundToInt(y) + 1, fade * 0.6f);
            }

            // แตกกิ่งสั้นๆ กลางเส้น
            if (allowBranch && step == length / 2 && rng.NextDouble() < 0.6)
            {
                float branchAngle = angle + (rng.NextDouble() < 0.5 ? 0.8f : -0.8f);
                DrawCrackLine(tex, x, y, branchAngle, length / 2, rng, false);
            }
        }
    }

    private static void PlotCrackPixel(Texture2D tex, int px, int py, float alpha)
    {
        if (px < 0 || py < 0 || px >= tex.width || py >= tex.height)
            return;

        Color existing = tex.GetPixel(px, py);
        float a = Mathf.Max(existing.a, Mathf.Clamp01(alpha));

        tex.SetPixel(px, py, new Color(0.92f, 0.96f, 1f, a));
    }

    private IEnumerator Shake()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            transform.localPosition = originalPosition + new Vector3(x, y, 0f);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
        shakeCoroutine = null;
    }

    private void BreakGlass()
    {
        isBroken = true;
        glassCollider.enabled = false;

        Debug.Log("Glass Broken!");
        onBreak?.Invoke();

        if (destroyOnBreak)
            Destroy(gameObject, shakeDuration);
        else
            StartCoroutine(HideAfterShake());
    }

    private IEnumerator HideAfterShake()
    {
        yield return new WaitForSecondsRealtime(shakeDuration);
        glassRenderer.enabled = false;
    }
}