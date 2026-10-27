using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[System.Serializable]
public class CrackStage
{
    [Tooltip("ภาพกระจกตอนร้าว")]
    public Sprite sprite;

    [Min(0)]
    [Tooltip("แสดงภาพนี้เมื่อ HP เหลือเท่ากับหรือน้อยกว่าค่านี้")]
    public int showWhenHPAtOrBelow = 1;
}

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class BreakableGlass : MonoBehaviour
{
    [Header("HP")]
    [Min(1)] public int maxHP = 3;
    [Min(1)] public int damagePerClick = 1;

    [Header("Crack Stages")]
    public CrackStage[] crackStages;

    [Header("Click Crack")]
    public bool spawnCrackOnHit = true;
    public Sprite[] crackDecals;
    public Vector2 decalSizeRange = new Vector2(1f, 1.8f);
    public bool randomRotation = true;
    public int decalSortingOffset = 1;

    [Header("Shake")]
    public float shakeDuration = 0.12f;
    public float shakeMagnitude = 0.08f;

    [Header("Break")]
    public bool destroyOnBreak = true;

    [Header("Shards - Break (กระจกแตก)")]
    public bool spawnShardsOnBreak = true;
    [Min(1)] public int breakShardCount = 40;
    public Vector2 breakShardSize = new Vector2(0.12f, 0.35f);
    public Vector2 breakShardSpeed = new Vector2(0.5f, 3.5f);
    public Vector2 breakShardLifetime = new Vector2(1.0f, 1.8f);
    public float breakShardGravity = 14f;

    [Header("Shards - Hit (ตอนคลิกเป็นรอย)")]
    public bool spawnShardsOnHit = true;
    public Vector2Int hitShardCount = new Vector2Int(3, 6);
    public Vector2 hitShardSize = new Vector2(0.04f, 0.1f);
    public Vector2 hitShardSpeed = new Vector2(0.3f, 1.5f);
    public Vector2 hitShardLifetime = new Vector2(0.6f, 1.1f);
    public float hitShardGravity = 10f;

    [Header("Shards - Common")]
    public Color shardColor = new Color(0.85f, 0.95f, 1f, 0.85f);

    [Header("Depth")]
    public bool setGlassDepth = true;
    public float glassZ = 1f;

    [Header("Repair Progress")]
    [Min(0.1f)] public float repairDuration = 1.5f;

    [Header("Sound")]
    [Tooltip("เสียงตอนกระจกโดนตี/โดนเจาะ (ทั้งผู้เล่นคลิกและยุงเจาะ) แต่ยังไม่แตก")]
    public AudioClip hitSound;

    [Tooltip("เสียงตอนกระจกแตก")]
    public AudioClip breakSound;

    [Tooltip("เสียงตอนซ่อมรอยแตกสำเร็จ")]
    public AudioClip repairSound;

    [Range(0f, 1f)]
    public float soundVolume = 1f;

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
    private Coroutine repairCoroutine;

    private readonly List<GameObject> decals = new List<GameObject>();

    private Camera mainCamera;

    public static bool InputBlocked = false;

    public int CurrentHP => currentHP;
    public bool IsBroken => isBroken;

    private static Sprite[] defaultCracks;
    private static Sprite shardSprite;

    private void Awake()
    {
        glassRenderer = GetComponent<SpriteRenderer>();
        glassCollider = GetComponent<Collider2D>();

        originalSprite = glassRenderer.sprite;
        originalPosition = transform.localPosition;

        currentHP = maxHP;
        mainCamera = Camera.main;

        if (setGlassDepth)
        {
            Vector3 pos = transform.position;
            pos.z = glassZ;
            transform.position = pos;

            originalPosition = transform.localPosition;
        }
    }

    private void OnMouseDown()
    {
        if (InputBlocked || IsPointerOverUI())
            return;

        if (isBroken)
            return;

        // ปุ่ม 2 = เทปซ่อมกระจก
        if (EquipmentManager.SelectedTool ==
            EquipmentManager.ToolType.RepairTape)
        {
            StartRepairAtMousePosition();
            return;
        }

        // ปุ่ม 1 = ไม้ตียุง
        // คลิกกระจกจะทำดาเมจตามระบบเดิม
        if (mainCamera == null)
            mainCamera = Camera.main;

        Vector3 clickPoint = transform.position;

        if (mainCamera != null)
        {
            Vector3 screenPoint = Input.mousePosition;

            screenPoint.z = Mathf.Abs(
                mainCamera.transform.position.z -
                transform.position.z
            );

            clickPoint = mainCamera.ScreenToWorldPoint(screenPoint);
        }

        TakeDamage(damagePerClick, clickPoint);
    }

    private void StartRepairAtMousePosition()
    {
        if (isBroken || currentHP >= maxHP)
            return;

        if (repairCoroutine != null)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        Vector3 screenPoint = Input.mousePosition;

        screenPoint.z = Mathf.Abs(
            mainCamera.transform.position.z -
            transform.position.z
        );

        Vector3 worldPoint =
            mainCamera.ScreenToWorldPoint(screenPoint);

        GameObject targetDecal = FindDecalAtPoint(worldPoint);

        // ต้องคลิกตรงรอยแตกเท่านั้น
        if (targetDecal == null)
        {
            Debug.Log("ไม่พบรอยแตกตรงจุดที่คลิก");
            return;
        }

        repairCoroutine = StartCoroutine(
            RepairRoutine(targetDecal)
        );
    }

    private GameObject FindDecalAtPoint(Vector3 worldPoint)
    {
        for (int i = decals.Count - 1; i >= 0; i--)
        {
            GameObject decal = decals[i];

            if (decal == null)
            {
                decals.RemoveAt(i);
                continue;
            }

            SpriteRenderer decalRenderer =
                decal.GetComponent<SpriteRenderer>();

            if (decalRenderer == null)
                continue;

            if (decalRenderer.bounds.Contains(worldPoint))
                return decal;
        }

        return null;
    }

    private IEnumerator RepairRoutine(GameObject targetDecal)
    {
        RepairProgressUI progressUI = RepairProgressUI.Instance;

        if (progressUI != null)
            progressUI.Show();

        float elapsed = 0f;

        while (elapsed < repairDuration)
        {
            // ยกเลิกถ้ากระจกแตก รอยแตกหาย หรือมี Event บังอยู่
            if (isBroken || targetDecal == null || InputBlocked)
                break;

            // ต้องกดเมาส์ค้าง
            if (!Input.GetMouseButton(0))
                break;

            // ต้องถือเทปอยู่
            if (EquipmentManager.SelectedTool !=
                EquipmentManager.ToolType.RepairTape)
                break;

            elapsed += Time.deltaTime;

            float progress = repairDuration > 0f
                ? elapsed / repairDuration
                : 1f;

            if (progressUI != null)
                progressUI.SetProgress(progress);

            yield return null;
        }

        bool completed =
            !isBroken &&
            targetDecal != null &&
            elapsed >= repairDuration &&
            !InputBlocked &&
            EquipmentManager.SelectedTool ==
            EquipmentManager.ToolType.RepairTape;

        if (completed)
        {
            int decalIndex = decals.IndexOf(targetDecal);

            if (decalIndex >= 0)
            {
                // ลบรอยแตกที่ซ่อมสำเร็จ
                decals.RemoveAt(decalIndex);
                Destroy(targetDecal);

                // เพิ่ม HP
                Heal(EquipmentManager.GetRepairAmount());

                // เสียงซ่อมสำเร็จ
                PlaySound(repairSound);

                Debug.Log(
                    "ซ่อมกระจกสำเร็จ: " +
                    currentHP + "/" + maxHP
                );
            }
        }

        if (progressUI != null)
            progressUI.Hide();

        repairCoroutine = null;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        for (int i = 0; i < Input.touchCount; i++)
        {
            if (EventSystem.current.IsPointerOverGameObject(
                Input.GetTouch(i).fingerId))
            {
                return true;
            }
        }

        return false;
    }

    // ดาเมจโดยไม่ระบุจุด เช่น ยุงเจาะกระจก
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, RandomPointOnGlass());
    }

    // ดาเมจโดยระบุจุดที่โดน
    public void TakeDamage(int damage, Vector3 worldPoint)
    {
        if (isBroken || damage <= 0)
            return;

        currentHP = Mathf.Max(currentHP - damage, 0);

        if (spawnCrackOnHit)
            SpawnCrackDecal(worldPoint);

        // เศษเล็กๆ ร่วงตอนโดนตี
        if (spawnShardsOnHit)
            SpawnHitShards(worldPoint);

        UpdateCrack();

        // ถ้าไม่แตก เล่นเสียงโดนตี (ถ้าแตกจะเล่นเสียงแตกใน BreakGlass แทน)
        if (currentHP > 0)
            PlaySound(hitSound);

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

    public void SetHP(int hp)
    {
        if (isBroken)
            return;

        currentHP = Mathf.Clamp(hp, 1, maxHP);
        UpdateCrack();
    }

    public void Heal(int amount)
    {
        if (isBroken || amount <= 0)
            return;

        int previousHP = currentHP;

        currentHP = Mathf.Clamp(
            currentHP + amount,
            0,
            maxHP
        );

        UpdateCrack();

        // HP เต็มแล้ว ลบรอยแตกทั้งหมด
        if (currentHP >= maxHP)
            ClearDecals();

        if (currentHP > previousHP)
        {
            Debug.Log(
                "ซ่อมกระจกสำเร็จ: " +
                currentHP + "/" + maxHP
            );
        }
    }

    public void ResetGlass()
    {
        StopAllCoroutines();

        shakeCoroutine = null;
        repairCoroutine = null;

        if (RepairProgressUI.Instance != null)
            RepairProgressUI.Instance.Hide();

        isBroken = false;
        currentHP = maxHP;

        ClearDecals();

        transform.localPosition = originalPosition;

        glassRenderer.enabled = true;
        glassCollider.enabled = true;
        glassRenderer.sprite = originalSprite;
    }

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

                if (currentHP <= stage.showWhenHPAtOrBelow &&
                    stage.showWhenHPAtOrBelow < bestThreshold)
                {
                    bestThreshold = stage.showWhenHPAtOrBelow;
                    chosen = stage.sprite;
                }
            }
        }

        glassRenderer.sprite = chosen;
    }

    [ContextMenu("Auto Fill Thresholds")]
    private void AutoFillThresholds()
    {
        if (crackStages == null || crackStages.Length == 0)
            return;

        int n = crackStages.Length;

        for (int i = 0; i < n; i++)
        {
            int threshold = Mathf.RoundToInt(
                maxHP * (n - i) / (n + 1f)
            );

            crackStages[i].showWhenHPAtOrBelow =
                Mathf.Clamp(
                    threshold,
                    1,
                    Mathf.Max(1, maxHP - 1)
                );
        }
    }

    private Vector3 RandomPointOnGlass()
    {
        Bounds bounds = glassCollider != null
            ? glassCollider.bounds
            : new Bounds(transform.position, Vector3.zero);

        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            transform.position.z
        );
    }

    private void SpawnCrackDecal(Vector3 worldPoint)
    {
        Sprite sprite = PickDecalSprite();

        if (sprite == null)
            return;

        GameObject go = new GameObject("CrackDecal");

        go.transform.SetParent(transform, true);

        go.transform.position = new Vector3(
            worldPoint.x,
            worldPoint.y,
            transform.position.z
        );

        if (randomRotation)
        {
            go.transform.rotation = Quaternion.Euler(
                0f,
                0f,
                Random.Range(0f, 360f)
            );
        }

        float worldSize = Random.Range(
            decalSizeRange.x,
            decalSizeRange.y
        );

        float spriteWidth = Mathf.Max(
            0.0001f,
            sprite.bounds.size.x
        );

        float parentScale = Mathf.Max(
            0.0001f,
            transform.lossyScale.x
        );

        go.transform.localScale = Vector3.one *
            (worldSize / spriteWidth / parentScale);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();

        sr.sprite = sprite;
        sr.sortingLayerID = glassRenderer.sortingLayerID;
        sr.sortingOrder =
            glassRenderer.sortingOrder + decalSortingOffset;

        decals.Add(go);
    }

    private Sprite PickDecalSprite()
    {
        if (crackDecals != null && crackDecals.Length > 0)
        {
            for (int tries = 0; tries < 5; tries++)
            {
                Sprite sprite = crackDecals[
                    Random.Range(0, crackDecals.Length)
                ];

                if (sprite != null)
                    return sprite;
            }

            return null;
        }

        Sprite[] defaults = GetDefaultCracks();

        return defaults[Random.Range(0, defaults.Length)];
    }

    private void ClearDecals()
    {
        foreach (GameObject decal in decals)
        {
            if (decal != null)
                Destroy(decal);
        }

        decals.Clear();
    }

    // ---------------------------------------------------------
    // Sound
    // ---------------------------------------------------------

    // สร้าง AudioSource ชั่วคราวเล่นเสียง
    // เสียงไม่ขาดแม้กระจกถูก Destroy และเป็นเสียง 2D ไม่เบาตามระยะกล้อง
    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
            return;

        GameObject go = new GameObject("GlassSFX");

        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = soundVolume;
        src.spatialBlend = 0f;
        src.Play();

        Destroy(go, clip.length + 0.1f);
    }

    // ---------------------------------------------------------
    // Glass Shards (เศษกระจก)
    // ---------------------------------------------------------

    private static Sprite GetShardSprite()
    {
        if (shardSprite != null)
            return shardSprite;

        const int size = 32;

        Texture2D tex = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false
        );

        tex.filterMode = FilterMode.Bilinear;

        Color32[] px = new Color32[size * size];
        Color32 white = new Color32(255, 255, 255, 255);

        // สามเหลี่ยมปลายแหลมชี้ขึ้น
        for (int y = 0; y < size; y++)
        {
            float halfWidth = (1f - y / (size - 1f)) * size * 0.5f;

            for (int x = 0; x < size; x++)
            {
                if (Mathf.Abs(x - (size - 1) * 0.5f) <= halfWidth)
                    px[y * size + x] = white;
            }
        }

        tex.SetPixels32(px);
        tex.Apply();

        shardSprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size
        );

        return shardSprite;
    }

    private void SpawnShard(
        Vector3 pos,
        Vector2 velocity,
        Vector2 sizeRange,
        Vector2 lifeRange,
        float gravity)
    {
        GameObject go = new GameObject("GlassShard");

        // ไม่ผูกเป็นลูกของกระจก เพราะกระจกอาจถูก Destroy
        go.transform.position = new Vector3(
            pos.x,
            pos.y,
            transform.position.z
        );

        go.transform.rotation = Quaternion.Euler(
            0f,
            0f,
            Random.Range(0f, 360f)
        );

        float s = Random.Range(sizeRange.x, sizeRange.y);

        go.transform.localScale = new Vector3(
            s * Random.Range(0.6f, 1.2f),
            s * Random.Range(0.8f, 1.6f),
            1f
        );

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetShardSprite();

        Color c = shardColor;
        float tint = Random.Range(0.85f, 1f); // สีแต่ละชิ้นไม่เหมือนกันเป๊ะ
        c.r *= tint;
        c.g *= tint;
        c.b *= tint;
        sr.color = c;

        sr.sortingLayerID = glassRenderer.sortingLayerID;
        sr.sortingOrder =
            glassRenderer.sortingOrder + decalSortingOffset + 1;

        GlassShard shard = go.AddComponent<GlassShard>();
        shard.Init(
            velocity,
            Random.Range(-540f, 540f),
            gravity,
            Random.Range(lifeRange.x, lifeRange.y)
        );
    }

    // เศษเล็กๆ ร่วงตอนคลิกโดน
    private void SpawnHitShards(Vector3 worldPoint)
    {
        int count = Random.Range(
            hitShardCount.x,
            hitShardCount.y + 1
        );

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * 0.15f;

            // กระเด็นออกมานิดหน่อย แล้วร่วงลง
            Vector2 vel = new Vector2(
                Random.Range(-1f, 1f),
                Random.Range(0f, 1f)
            ).normalized * Random.Range(
                hitShardSpeed.x,
                hitShardSpeed.y
            );

            SpawnShard(
                worldPoint + (Vector3)offset,
                vel,
                hitShardSize,
                hitShardLifetime,
                hitShardGravity
            );
        }
    }

    // กระจกแตกเป็นเศษทั้งบาน
    private void SpawnBreakShards()
    {
        Bounds b = glassRenderer.bounds;

        for (int i = 0; i < breakShardCount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(b.min.x, b.max.x),
                Random.Range(b.min.y, b.max.y),
                transform.position.z
            );

            // กระเด็นออกจากกลางบานเล็กน้อย แล้วร่วงลงตามแรงโน้มถ่วง
            Vector2 dir = ((Vector2)(pos - b.center)).normalized;

            if (dir == Vector2.zero)
                dir = Random.insideUnitCircle.normalized;

            Vector2 vel = dir * Random.Range(
                breakShardSpeed.x,
                breakShardSpeed.y
            );

            vel.y += Random.Range(0f, 1.5f);

            SpawnShard(
                pos,
                vel,
                breakShardSize,
                breakShardLifetime,
                breakShardGravity
            );
        }
    }

    // ---------------------------------------------------------
    // Procedural crack decal
    // ---------------------------------------------------------

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

        Texture2D tex = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false
        );

        tex.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];

        tex.SetPixels32(pixels);

        System.Random rng = new System.Random(
            seed * 7919 + 13
        );

        int rays = 6 + rng.Next(0, 4);

        for (int r = 0; r < rays; r++)
        {
            float angle = (float)(
                rng.NextDouble() * Mathf.PI * 2.0
            );

            int length = 30 + rng.Next(0, 30);

            DrawCrackLine(
                tex,
                size / 2f,
                size / 2f,
                angle,
                length,
                rng,
                true
            );
        }

        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            64f
        );
    }

    private static void DrawCrackLine(
        Texture2D tex,
        float x,
        float y,
        float angle,
        int length,
        System.Random rng,
        bool allowBranch)
    {
        for (int step = 0; step < length; step++)
        {
            x += Mathf.Cos(angle);
            y += Mathf.Sin(angle);

            angle += (float)(
                rng.NextDouble() - 0.5
            ) * 0.5f;

            float fade = 1f -
                (step / (float)(length + 10)) * 0.6f;

            bool thick = step < length * 0.5f;

            PlotCrackPixel(
                tex,
                Mathf.RoundToInt(x),
                Mathf.RoundToInt(y),
                fade
            );

            if (thick)
            {
                PlotCrackPixel(
                    tex,
                    Mathf.RoundToInt(x) + 1,
                    Mathf.RoundToInt(y),
                    fade * 0.6f
                );

                PlotCrackPixel(
                    tex,
                    Mathf.RoundToInt(x),
                    Mathf.RoundToInt(y) + 1,
                    fade * 0.6f
                );
            }

            if (allowBranch &&
                step == length / 2 &&
                rng.NextDouble() < 0.6)
            {
                float branchAngle = angle +
                    (rng.NextDouble() < 0.5 ? 0.8f : -0.8f);

                DrawCrackLine(
                    tex,
                    x,
                    y,
                    branchAngle,
                    length / 2,
                    rng,
                    false
                );
            }
        }
    }

    private static void PlotCrackPixel(
        Texture2D tex,
        int px,
        int py,
        float alpha)
    {
        if (px < 0 || py < 0 ||
            px >= tex.width || py >= tex.height)
            return;

        Color existing = tex.GetPixel(px, py);

        float a = Mathf.Max(
            existing.a,
            Mathf.Clamp01(alpha)
        );

        tex.SetPixel(
            px,
            py,
            new Color(0.92f, 0.96f, 1f, a)
        );
    }

    private IEnumerator Shake()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;

            transform.localPosition =
                originalPosition + new Vector3(x, y, 0f);

            elapsed += Time.unscaledDeltaTime;

            yield return null;
        }

        transform.localPosition = originalPosition;
        shakeCoroutine = null;
    }

    private void BreakGlass()
    {
        isBroken = true;

        if (repairCoroutine != null)
        {
            StopCoroutine(repairCoroutine);
            repairCoroutine = null;
        }

        if (RepairProgressUI.Instance != null)
            RepairProgressUI.Instance.Hide();

        glassCollider.enabled = false;

        // เสียงกระจกแตก
        PlaySound(breakSound);

        // แตกเป็นเศษ: ซ่อนบานเดิมแล้วปล่อยเศษร่วงลงมา
        if (spawnShardsOnBreak)
        {
            SpawnBreakShards();
            ClearDecals();
            glassRenderer.enabled = false;
        }

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

    private void OnDisable()
    {
        if (repairCoroutine != null)
        {
            StopCoroutine(repairCoroutine);
            repairCoroutine = null;
        }

        if (RepairProgressUI.Instance != null)
            RepairProgressUI.Instance.Hide();
    }
}