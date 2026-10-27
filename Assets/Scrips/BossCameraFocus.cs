using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class BossCameraFocus : MonoBehaviour
{
    [Header("Zoom Settings")]
    public float bossZoomSize = 3f;
    public float zoomSpeed = 3f;

    [Header("Follow Settings")]
    public float followSmoothness = 5f;
    public Vector3 followOffset = new Vector3(0f, 0f, -10f);

    private Camera targetCamera;
    private Transform bossTarget;

    private Vector3 originalPosition;
    private float originalOrthographicSize;

    private bool isFollowingBoss;
    private Coroutine restoreCoroutine;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();

        originalPosition = transform.position;
        originalOrthographicSize = targetCamera.orthographicSize;
    }
    private void LateUpdate()
    {
        if (targetCamera == null)
            return;

        if (isFollowingBoss && bossTarget != null)
        {
            Vector3 targetPosition =
                bossTarget.position + followOffset;

            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                followSmoothness * Time.deltaTime
            );

            targetCamera.orthographicSize = Mathf.Lerp(
                targetCamera.orthographicSize,
                bossZoomSize,
                zoomSpeed * Time.deltaTime
            );
        }
        else if (bossTarget == null && isFollowingBoss)
        {
            RestoreView();
        }
    }

    public void FocusOn(Transform target)
    {
        if (target == null)
            return;

        if (restoreCoroutine != null)
        {
            StopCoroutine(restoreCoroutine);
            restoreCoroutine = null;
        }

        bossTarget = target;
        isFollowingBoss = true;
    }

    public void RestoreView()
    {
        isFollowingBoss = false;
        bossTarget = null;

        if (restoreCoroutine != null)
            StopCoroutine(restoreCoroutine);

        restoreCoroutine = StartCoroutine(RestoreRoutine());
    }
    private IEnumerator RestoreRoutine()
    {
        while (Vector3.Distance(
                   transform.position,
                   originalPosition
               ) > 0.02f ||
               Mathf.Abs(
                   targetCamera.orthographicSize -
                   originalOrthographicSize
               ) > 0.02f)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                originalPosition,
                zoomSpeed * Time.deltaTime
            );

            targetCamera.orthographicSize = Mathf.Lerp(
                targetCamera.orthographicSize,
                originalOrthographicSize,
                zoomSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = originalPosition;
        targetCamera.orthographicSize = originalOrthographicSize;

        restoreCoroutine = null;
    }
}