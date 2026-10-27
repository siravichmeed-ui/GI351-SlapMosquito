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
    [Range(0f, 0.5f)] public float screenMargin = 0.1f;

    [Header("Summon (เรียกลูกน้อง)")]
    [Min(1)] public int minionsPerWave = 5;
    [Tooltip("ลูกน้องบินเร็วกว่ายุงธรรมดากี่เท่า")]
    public float minionSpeedMultiplier = 1.8f;
    [Tooltip("หน่วงหลังลูกน้องชุดเก่าหมด ก่อนเรียกชุดใหม่ (วินาที)")]
    public float summonDelay = 1.0f;
    public float firstSummonDelay = 1.5f;
    [Tooltip("ระยะที่ลูกน้องโผล่ ห่างจากกลางตัวบอส (เทียบกับขนาดตัวบอส)")]
    public float spawnRadiusFactor = 0.5f;

    [Header("Hit Flash (โดนตีแล้วแดงกระพริบ)")]
    public Color flashColor = Color.red;
    [Min(1)] public int flashCount = 3;
    public float flashInterval = 0.06f;

    [Header("Defeat")]
    public float deadDuration = 1f;
    public bool killMinionsOnDeath = true;
    [Tooltip("เรียก RegisterMosquitoHit กี่ครั้งตอนบอสตาย (ให้คะแนน/คอมโบ)")]
    [Min(0)] public int scoreHitsOnDefeat = 5;
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

    private readonly List<Mosquito> minions = new List<Mosquito>();
    private bool hadMinions;
    private float nextSummonTime;

    public int CurrentHP => currentHP;
    public int MaxHP => s != null ? s.maxHP : 0;
    public bool IsDead => isDead;

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

        if (sr != null)
            originalColor = sr.color;

        nextSummonTime = Time.time + s.firstSummonDelay;
        PickNewTarget();
    }

    private void Update()
    {
        if (isDead || s == null)
            return;

        Fly();
        UpdateSummon();
    }

    // ---------------- บิน ----------------

    private void Fly()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            s.flySpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) < 0.3f)
            PickNewTarget();
    }

    private void PickNewTarget()
    {
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
            new Vector3(s.screenMargin, s.screenMargin, distance)
        );

        Vector3 max = cam.ViewportToWorldPoint(
            new Vector3(1f - s.screenMargin, 1f - s.screenMargin, distance)
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

        currentHP--;

        PlayFlash();

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

        if (ComboScoreManager.Instance != null)
        {
            for (int i = 0; i < s.scoreHitsOnDefeat; i++)
                ComboScoreManager.Instance.RegisterMosquitoHit();
        }

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
}
