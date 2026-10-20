
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

    [Header("Special Mosquito")]
    public bool isSpecial = false;
    public Color specialColor = Color.yellow;
    public float specialScale = 1.3f;

    private Vector3 targetPosition;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private float randomOffset;

    private bool alreadyHit;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
        randomOffset = Random.Range(0f, 100f);
    }

    private void Start()
    {
        Camera cam = Camera.main;

        if (cam != null)
        {
            float distance = Mathf.Abs(
                cam.transform.position.z - transform.position.z
            );

            Vector3 bottomLeft =
                cam.ViewportToWorldPoint(
                    new Vector3(0f, 0f, distance)
                );

            Vector3 topRight =
                cam.ViewportToWorldPoint(
                    new Vector3(1f, 1f, distance)
                );

            targetPosition = new Vector3(
                Random.Range(bottomLeft.x, topRight.x),
                Random.Range(bottomLeft.y, topRight.y),
                transform.position.z
            );
        }
        else
        {
            targetPosition = transform.position;
        }
    }

    private void Update()
    {
        if (state == MosquitoState.Dead)
            return;

        if (state == MosquitoState.Flying)
        {
            FlyAround();
        }
    }

    private void FlyAround()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            flySpeed * Time.deltaTime
        );

        float wobble = Mathf.Sin(
            (Time.time + randomOffset) * wobbleSpeed
        ) * wobbleAmount;

        Vector3 pos = transform.position;
        pos.y += wobble * Time.deltaTime;
        transform.position = pos;

        if (Vector3.Distance(transform.position, targetPosition) < 0.2f)
        {
            ChooseNewTarget();
        }
    }

    private void ChooseNewTarget()
    {
        Camera cam = Camera.main;

        if (cam == null)
            return;

        float distance = Mathf.Abs(
            cam.transform.position.z - transform.position.z
        );

        Vector3 bottomLeft =
            cam.ViewportToWorldPoint(
                new Vector3(0f, 0f, distance)
            );

        Vector3 topRight =
            cam.ViewportToWorldPoint(
                new Vector3(1f, 1f, distance)
            );

        targetPosition = new Vector3(
            Random.Range(bottomLeft.x, topRight.x),
            Random.Range(bottomLeft.y, topRight.y),
            transform.position.z
        );
    }

    public void Setup(bool special)
    {
        isSpecial = special;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (isSpecial)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = specialColor;
            }

            transform.localScale = originalScale * specialScale;
        }
        else
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }

            transform.localScale = originalScale;
        }
    }

    private void OnMouseDown()
    {
        if (alreadyHit || state == MosquitoState.Dead)
            return;

        alreadyHit = true;
        state = MosquitoState.Dead;

        // เพิ่มคะแนนและ Combo เมื่อคลิกโดนยุง
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

        // เปิด Slot Event เมื่อเป็นยุงพิเศษ
        if (isSpecial &&
            SlotEventManager.Instance != null)
        {
            SlotEventManager.Instance.OpenEvent();
        }

        Destroy(gameObject);
    }
}
