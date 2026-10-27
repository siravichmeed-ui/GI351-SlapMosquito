using System.Collections;
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

    [Header("Animation")]
    public float deadAnimationDuration = 0.5f;

    private Animator animator;

    [Header("Special Mosquito (ยุงทอง)")]
    public bool isSpecial = false;
    public Color specialColor = Color.yellow;
    public float specialScale = 1.3f;

    [Header("Glass Pierce")]
    public bool canPierce = true;

    [Range(0f, 1f)]
    public float pierceChance = 0.08f;

    public float minFlyTimeBeforePierce = 2f;
    public float pierceCooldown = 6f;
    public float pierceDuration = 3f;

    [Min(1)]
    public int pierceDamage = 1;

    public int maxPiercingAtOnce = 1;
    public bool dieAfterPierce = false;

    [Header("Warning Overlay")]
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

    private static int activePiercers;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        activePiercers = 0;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

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

        float wobble =
            Mathf.Sin((Time.time + randomOffset) * wobbleSpeed)
            * wobbleAmount;

        Vector3 pos = transform.position;
        pos.y += wobble * Time.deltaTime;
        transform.position = pos;

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

        float distance = Mathf.Abs(
            cam.transform.position.z - transform.position.z
        );

        Vector3 bottomLeft = cam.ViewportToWorldPoint(
            new Vector3(0f, 0f, distance)
        );

        Vector3 topRight = cam.ViewportToWorldPoint(
            new Vector3(1f, 1f, distance)
        );

        targetPosition = new Vector3(
            Random.Range(bottomLeft.x, topRight.x),
            Random.Range(bottomLeft.y, topRight.y),
            transform.position.z
        );
    }

    // ---------------- เจาะกระจก ----------------

    private bool TryStartPierce()
    {
        if (!canPierce || isSpecial)
            return false;

        if (flyTimer < minFlyTimeBeforePierce ||
            Time.time < nextPierceTime)
        {
            return false;
        }

        if (activePiercers >= maxPiercingAtOnce)
            return false;

        if (Random.value > pierceChance)
            return false;

        BreakableGlass glass = FindGlassUnderMosquito();

        if (glass == null)
            return false;

        StartPierce(glass);
        return true;
    }

    private void StartPierce(BreakableGlass target)
    {
        pierceTarget = target;
        state = MosquitoState.Attacking;
        attackTimer = 0f;
        pulsePhase = 0f;

        if (animator != null)
            animator.SetTrigger("Attack");

        activePiercers++;
        countedAsPiercer = true;

        ShowWarning();
    }

    private void UpdateAttack()
    {
        if (pierceTarget == null || pierceTarget.IsBroken)
        {
            CancelPierce();
            return;
        }

        attackTimer += Time.deltaTime;

        float progress = Mathf.Clamp01(
            attackTimer / pierceDuration
        );

        pulsePhase += Time.deltaTime
            * pulseSpeed
            * (1f + progress * 2f);

        if (warningObject != null)
        {
            float scale = warningScale *
                (1f + Mathf.Sin(pulsePhase) * pulseAmount);

            warningObject.transform.localScale =
                Vector3.one * scale;
        }

        if (attackTimer >= pierceDuration)
            FinishPierce();
    }

    private void FinishPierce()
    {
        ReleasePiercer();
        HideWarning();

        if (pierceTarget != null && !pierceTarget.IsBroken)
        {
            pierceTarget.TakeDamage(
                pierceDamage,
                transform.position
            );
        }

        pierceTarget = null;

        if (dieAfterPierce)
        {
            PlayDeadAnimation();
            return;
        }

        ReturnToFlying();
    }

    private void CancelPierce()
    {
        ReleasePiercer();
        HideWarning();

        pierceTarget = null;

        ReturnToFlying();
    }

    private void ReturnToFlying()
    {
        state = MosquitoState.Flying;

        if (animator != null)
            animator.Play("Fly", 0, 0f);

        nextPierceTime = Time.time + pierceCooldown;

        PickNewTarget();
    }

    private BreakableGlass FindGlassUnderMosquito()
    {
        Collider2D[] hits =
            Physics2D.OverlapPointAll(transform.position);

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            BreakableGlass glass =
                hit.GetComponent<BreakableGlass>();

            if (glass != null && !glass.IsBroken)
                return glass;
        }

        return null;
    }

    private void ReleasePiercer()
    {
        if (!countedAsPiercer)
            return;

        activePiercers = Mathf.Max(0, activePiercers - 1);
        countedAsPiercer = false;
    }

    // ---------------- Animation ตาย ----------------

    private void PlayDeadAnimation()
    {
        if (state == MosquitoState.Dead)
            return;

        state = MosquitoState.Dead;

        ReleasePiercer();
        HideWarning();

        if (animator != null)
            animator.SetTrigger("Dead");

        if (animator != null && deadAnimationDuration > 0f)
        {
            StartCoroutine(DestroyAfterDeadAnimation());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator DestroyAfterDeadAnimation()
    {
        yield return new WaitForSeconds(deadAnimationDuration);

        Destroy(gameObject);
    }

    // ---------------- Warning ----------------

    private void ShowWarning()
    {
        if (warningObject != null)
            return;

        if (warningPrefab != null)
        {
            warningObject = Instantiate(
                warningPrefab,
                transform
            );

            warningObject.transform.localPosition = warningOffset;
        }
        else
        {
            warningObject = new GameObject("PierceWarning");

            warningObject.transform.SetParent(transform, false);
            warningObject.transform.localPosition = warningOffset;

            SpriteRenderer renderer =
                warningObject.AddComponent<SpriteRenderer>();

            renderer.sprite = GetDefaultWarningSprite();

            if (spriteRenderer != null)
            {
                renderer.sortingLayerID =
                    spriteRenderer.sortingLayerID;

                renderer.sortingOrder =
                    spriteRenderer.sortingOrder + 10;
            }
        }

        warningObject.transform.localScale =
            Vector3.one * warningScale;
    }

    private void HideWarning()
    {
        if (warningObject != null)
        {
            Destroy(warningObject);
            warningObject = null;
        }
    }

    // ---------------- สร้างเครื่องหมาย ! ----------------

    private static Sprite defaultWarningSprite;

    private static Sprite GetDefaultWarningSprite()
    {
        if (defaultWarningSprite != null)
            return defaultWarningSprite;

        const int size = 32;

        Texture2D texture = new Texture2D(
            size,
            size,
            TextureFormat.RGBA32,
            false
        );

        texture.filterMode = FilterMode.Point;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 red = new Color32(220, 30, 30, 255);
        Color32 white = new Color32(255, 255, 255, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - 15.5f;
                float dy = y - 15.5f;

                float distance = Mathf.Sqrt(
                    dx * dx + dy * dy
                );

                Color32 color = clear;

                if (distance <= 15.5f)
                    color = distance >= 13.5f ? white : red;

                bool bar =
                    x >= 14 && x <= 17 &&
                    y >= 11 && y <= 24;

                bool dot =
                    x >= 14 && x <= 17 &&
                    y >= 6 && y <= 9;

                if (bar || dot)
                    color = white;

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();

        defaultWarningSprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            32f
        );

        return defaultWarningSprite;
    }

    // ---------------- ตั้งค่ายุง ----------------

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

    // ---------------- คลิกตีตัวยุง ----------------

    private void OnMouseDown()
    {
        if (alreadyHit || state == MosquitoState.Dead)
            return;

        if (BreakableGlass.InputBlocked)
            return;

        if (EquipmentManager.SelectedTool !=
            EquipmentManager.ToolType.FlySwatter)
        {
            return;
        }

        alreadyHit = true;

        // เล่น Animation ตายก่อนลบ GameObject
        PlayDeadAnimation();

        // เพิ่มคะแนนและ Combo
        if (ComboScoreManager.Instance != null)
        {
            ComboScoreManager.Instance.RegisterMosquitoHit();
        }
        else
        {
            Debug.LogError(
                "ไม่พบ ComboScoreManager ใน Scene!"
            );
        }

        // เปิด Slot Event เมื่อกำจัดยุงพิเศษ
        if (isSpecial &&
            SlotEventManager.Instance != null)
        {
            SlotEventManager.Instance.OpenEvent();
        }
    }

    private void OnDestroy()
    {
        ReleasePiercer();
    }
}