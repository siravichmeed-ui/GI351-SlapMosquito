using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class BossSettings
{
    [Header("Boss")]
    [Min(1)] public int maxHP = 50;

    [Tooltip("ขนาดเทียบกับยุงธรรมดา (ขนาดของ Prefab)")]
    public float scaleMultiplier = 15f;

    public float flySpeed = 2f;

    [Tooltip("ห่างจากขอบจอ (Viewport 0-0.5) ที่บอสจะไม่บินเกินเข้าไป")]
    [Range(0f, 0.5f)]
    public float screenMargin = 0.1f;

    [Header("Summon (เรียกลูกน้อง)")]
    [Min(1)]
    public int minionsPerWave = 5;

    [Tooltip("ลูกน้องบินเร็วกว่ายุงธรรมดากี่เท่า")]
    public float minionSpeedMultiplier = 1.8f;

    [Tooltip("หน่วงหลังลูกน้องชุดเก่าหมด ก่อนเรียกชุดใหม่ (วินาที)")]
    public float summonDelay = 1.0f;

    public float firstSummonDelay = 1.5f;

    [Tooltip("ระยะที่ลูกน้องโผล่ ห่างจากกลางตัวบอส (เทียบกับขนาดตัวบอส)")]
    public float spawnRadiusFactor = 0.5f;

    [Header("Hit Flash (โดนตีแล้วแดงกระพริบ)")]
    public Color flashColor = Color.red;

    [Min(1)]
    public int flashCount = 3;

    public float flashInterval = 0.06f;

    [Header("Defeat")]
    public float deadDuration = 1f;

    public bool killMinionsOnDeath = true;
}

public class BossMosquito : MonoBehaviour
{
    public event Action<BossMosquito> Defeated;

    private BossSettings s;
    private GameObject minionPrefab;

    private SpriteRenderer sr;
    private Animator animator;
    private Collider2D col;

    private int currentHP;
    private bool isDead;

    private Vector3 targetPosition;
    private Color originalColor;
    private Coroutine flashRoutine;

    [Header("Fly Area")]
    public BoxCollider2D flyArea;

    [Header("Flight Animation")]
    public float wobbleAmount = 0.15f;
    public float wobbleSpeed = 3f;

   
    private float randomOffset;
    private readonly List<Mosquito> minions = new List<Mosquito>();

    private bool hadMinions;
    private float nextSummonTime;

    public int CurrentHP => currentHP;
    public int MaxHP => s != null ? s.maxHP : 0;
    public bool IsDead => isDead;
    private bool isEscaping;

    public void Init(BossSettings settings, GameObject minionPrefab)
    {
        s = settings;
        this.minionPrefab = minionPrefab;

        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        if (col == null)
            Debug.LogWarning("Boss ไม่มี Collider2D จึงคลิกโดนไม่ได้");

        currentHP = s.maxHP;
        transform.localScale *= s.scaleMultiplier;

        randomOffset = Random.Range(0f, 100f);

        FindFlyArea();

        if (sr != null)
            originalColor = sr.color;

        nextSummonTime = Time.time + s.firstSummonDelay;
        PickNewTarget();
    }
    private void FindFlyArea()
    {
        if (flyArea != null)
            return;

        GameObject area = GameObject.Find("MosquitoFlyArea");

        if (area != null)
            flyArea = area.GetComponent<BoxCollider2D>();
    }
    private void Update()
    {
        if (isDead || s == null)
            return;

        // ถ้ากำลังหนี ให้ออกจากระบบบินปกติ
        if (isEscaping)
            return;

        Fly();
        UpdateSummon();
    }

    // ---------------- บิน ----------------

    private void Fly()
    {
        // หันหน้าตามทิศทางบิน
        // สมมติว่าภาพต้นฉบับหันซ้าย และ Scale X เป็นบวก
        // ถ้าบินขวา ให้กลับภาพด้วย Scale X ติดลบ

        if (targetPosition.x < transform.position.x)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
        else if (targetPosition.x > transform.position.x)
        {
            transform.localScale = new Vector3(
                -Mathf.Abs(transform.localScale.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }

        // บินไปยังจุดหมาย
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            s.flySpeed * Time.deltaTime
        );

        // ขยับขึ้นลงเล็กน้อยขณะบิน
        float wobble = Mathf.Sin(
            (Time.time + randomOffset) * wobbleSpeed
        ) * wobbleAmount;

        Vector3 pos = transform.position;
        pos.y += wobble * Time.deltaTime;
        transform.position = pos;

        // จำกัดพื้นที่บิน
        if (flyArea != null)
        {
            Bounds bounds = GetFlyBounds();

            pos = transform.position;

            pos.x = Mathf.Clamp(
                pos.x,
                bounds.min.x,
                bounds.max.x
            );

            pos.y = Mathf.Clamp(
                pos.y,
                bounds.min.y,
                bounds.max.y
            );

            transform.position = pos;
        }

        // เมื่อถึงจุดหมาย ให้สุ่มจุดใหม่
        if (Vector3.Distance(
            transform.position,
            targetPosition
        ) < 0.2f)
        {
            PickNewTarget();
        }
    }

    private Bounds GetFlyBounds()
    {
        Bounds bounds = flyArea.bounds;

        if (sr != null)
        {
            Vector3 extents = sr.bounds.extents;

            float minX = bounds.min.x + extents.x;
            float maxX = bounds.max.x - extents.x;

            float minY = bounds.min.y + extents.y;
            float maxY = bounds.max.y - extents.y;

            // ป้องกันกรณีบอสตัวใหญ่กว่าพื้นที่บิน
            if (minX > maxX)
            {
                minX = bounds.center.x;
                maxX = bounds.center.x;
            }

            if (minY > maxY)
            {
                minY = bounds.center.y;
                maxY = bounds.center.y;
            }

            bounds.SetMinMax(
                new Vector3(minX, minY, bounds.min.z),
                new Vector3(maxX, maxY, bounds.max.z)
            );
        }

        return bounds;
    }

    private void PickNewTarget()
    {
        // ถ้ามี Fly Area ให้สุ่มเป้าหมายภายในกรอบ
        if (flyArea != null)
        {
            Bounds bounds = GetFlyBounds();

            targetPosition = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                transform.position.z
            );

            return;
        }

        // หากหา Fly Area ไม่พบ ให้ใช้พื้นที่กล้องแทน
        Camera cam = Camera.main;

        if (cam == null)
        {
            targetPosition = transform.position;
            return;
        }

        float distance = Mathf.Abs(
            cam.transform.position.z - transform.position.z
        );

        Vector3 min = cam.ViewportToWorldPoint(
            new Vector3(
                s.screenMargin,
                s.screenMargin,
                distance
            )
        );

        Vector3 max = cam.ViewportToWorldPoint(
            new Vector3(
                1f - s.screenMargin,
                1f - s.screenMargin,
                distance
            )
        );

        targetPosition = new Vector3(
            Random.Range(min.x, max.x),
            Random.Range(min.y, max.y),
            transform.position.z
        );
    }

    // ---------------- เรียกลูกน้อง ----------------

    private void UpdateSummon()
    {
        // ล้างยุงที่ตายแล้วออกจากลิสต์
        minions.RemoveAll(m => m == null);

        if (minions.Count > 0)
            return;

        // ชุดเก่าเพิ่งหมด -> เริ่มนับหน่วงก่อนเรียกชุดใหม่
        if (hadMinions)
        {
            hadMinions = false;
            nextSummonTime = Time.time + s.summonDelay;
        }

        if (Time.time >= nextSummonTime)
            SummonWave();
    }

    private void SummonWave()
    {
        if (minionPrefab == null)
            return;

        float radius = 1f;

        if (sr != null)
            radius = Mathf.Max(sr.bounds.extents.x, sr.bounds.extents.y)
                     * s.spawnRadiusFactor;

        for (int i = 0; i < s.minionsPerWave; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;

            Vector3 pos = transform.position + (Vector3)offset;

            GameObject go = Instantiate(
                minionPrefab,
                pos,
                Quaternion.identity
            );

            Mosquito m = go.GetComponent<Mosquito>();

            if (m == null)
            {
                Destroy(go);
                continue;
            }

            m.Setup(false);
            m.flySpeed *= s.minionSpeedMultiplier;

            minions.Add(m);
        }

        hadMinions = minions.Count > 0;
    }

    // ---------------- โดนตี ----------------

    private void OnMouseDown()
    {
        if (isDead || s == null)
            return;

        if (BreakableGlass.InputBlocked)
            return;

        if (EquipmentManager.SelectedTool !=
            EquipmentManager.ToolType.FlySwatter)
            return;

        TakeHit();
    }

    public void TakeHit()
    {
        if (isDead)
            return;

        // เพิ่ม Combo และคะแนนเมื่อโจมตีบอสโดน
        if (ComboManager.Instance != null)
        {
            ComboManager.Instance.RegisterBossHit(this);
        }

        // ลด HP บอสตามระบบเดิม
        currentHP--;

        // เอฟเฟกต์กระพริบเมื่อถูกตี
        PlayFlash();

        // ตรวจสอบว่าบอสตายหรือยัง
        if (currentHP <= 0)
            Die();
    }

    private void PlayFlash()
    {
        if (sr == null)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < s.flashCount; i++)
        {
            sr.color = s.flashColor;
            yield return new WaitForSeconds(s.flashInterval);

            sr.color = originalColor;
            yield return new WaitForSeconds(s.flashInterval);
        }

        sr.color = originalColor;
        flashRoutine = null;
    }

    // ---------------- ตาย ----------------

    private void Die()
    {
        isDead = true;

        if (col != null)
            col.enabled = false;

        if (s.killMinionsOnDeath)
        {
            foreach (Mosquito m in minions)
            {
                if (m != null)
                    Destroy(m.gameObject);
            }
        }

        minions.Clear();

        TrySetTrigger("Dead");

        Defeated?.Invoke(this);

        Destroy(gameObject, s.deadDuration);
    }

    private void TrySetTrigger(string triggerName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger &&
                p.name == triggerName)
            {
                animator.SetTrigger(triggerName);
                return;
            }
        }
    }
    // ---------------- Escape ----------------

    public void EscapeAndGameOver(GameOverManager gameOverManager)
    {
        if (isEscaping || isDead)
            return;

        isEscaping = true;

        // ป้องกันไม่ให้ผู้เล่นคลิกโจมตีบอสได้อีก
        if (col != null)
            col.enabled = false;

        // หยุดการเรียกลูกน้อง
        StopAllCoroutines();

        // หยุดการทำงานของลูกน้องที่ยังเหลืออยู่
        foreach (Mosquito m in minions)
        {
            if (m != null)
                Destroy(m.gameObject);
        }

        minions.Clear();

        StartCoroutine(EscapeRoutine(gameOverManager));
    }

    private IEnumerator EscapeRoutine(GameOverManager gameOverManager)
    {
        Camera cam = Camera.main;

        Vector3 escapeTarget;

        if (cam != null)
        {
            float distance = Mathf.Abs(
                cam.transform.position.z - transform.position.z
            );

            // จุดหมายอยู่นอกขอบขวาของกล้อง
            escapeTarget = cam.ViewportToWorldPoint(
                new Vector3(1.3f, 0.8f, distance)
            );

            escapeTarget.z = transform.position.z;
        }
        else
        {
            escapeTarget = transform.position + Vector3.right * 30f;
        }

        // หันหน้าไปทางที่กำลังหนี
        transform.localScale = new Vector3(
            -Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            transform.localScale.z
        );

        // บินออกนอกจอ
        while (Vector3.Distance(transform.position, escapeTarget) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                escapeTarget,
                s.flySpeed * 4f * Time.deltaTime
            );

            yield return null;
        }

        // ยุงหนีออกไปสำเร็จแล้ว
        if (gameOverManager != null)
        {
            gameOverManager.TriggerGameOver();
        }
        else
        {
            Debug.LogError(
                "BossMosquito: ไม่ได้กำหนด GameOverManager"
            );
        }

        Destroy(gameObject);
    }
}