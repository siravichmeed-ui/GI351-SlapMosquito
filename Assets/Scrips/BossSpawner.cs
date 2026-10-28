using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class BossSpawner : MonoBehaviour
{
    [Header("Mosquito Prefab")]
    public GameObject mosquitoPrefab;

    [Header("Boss Spawn Condition")]
    [Min(1)] public int minKillsToSpawnBoss = 20;
    [Min(1)] public int maxKillsToSpawnBoss = 30;
    public bool repeatBoss = true;

    [Header("Boss Spawn Position")]
    [Range(0f, 1f)] public float spawnViewportX = 0.5f;
    [Range(0f, 1f)] public float spawnViewportY = 0.65f;

    [Header("Random Boss HP")]
    [Min(1)] public int minBossHP = 30;
    [Min(1)] public int maxBossHP = 70;

    [Header("Boss Scale and Speed")]
    [Min(0.1f)] public float bossScaleMultiplier = 3f;
    [Min(0f)] public float bossFlySpeed = 1f;

    [Header("Boss Camera")]
    public BossCameraFocus bossCamera;

    [Header("Game Over")]
    public GameOverManager gameOverManager;

    [Header("Spawner Control")]
    public Behaviour[] spawnersToPause;
    public bool clearMosquitoesOnBossSpawn = true;

    [Header("Events")]
    public UnityEvent onBossSpawned;
    public UnityEvent onBossDefeated;

    private int killCount;
    private int currentKillTarget;
    private BossMosquito currentBoss;

    private bool bossActive;
    private bool finished;
    private bool escapeStarted;

    public int KillCount => killCount;
    public int CurrentKillTarget => currentKillTarget;
    public bool BossActive => bossActive;

    public static bool SpawningPaused { get; private set; }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        SpawningPaused = false;
    }

    private void Awake()
    {
        minKillsToSpawnBoss = Mathf.Max(1, minKillsToSpawnBoss);

        maxKillsToSpawnBoss = Mathf.Max(
            minKillsToSpawnBoss,
            maxKillsToSpawnBoss
        );

        minBossHP = Mathf.Max(1, minBossHP);

        maxBossHP = Mathf.Max(
            minBossHP,
            maxBossHP
        );

        if (bossCamera == null && Camera.main != null)
        {
            bossCamera = Camera.main.GetComponent<BossCameraFocus>();
        }

        ChooseNextKillTarget();
    }

    private void OnEnable()
    {
        Mosquito.OnMosquitoKilled += HandleMosquitoKilled;
    }

    private void OnDisable()
    {
        Mosquito.OnMosquitoKilled -= HandleMosquitoKilled;

        SetSpawnersPaused(false);

        if (bossCamera != null)
            bossCamera.RestoreView();

        if (currentBoss != null)
            currentBoss.Defeated -= HandleBossDefeated;
    }

    private void Update()
    {
        if (bossActive && currentBoss == null)
        {
            bossActive = false;

            SetSpawnersPaused(false);

            if (bossCamera != null)
                bossCamera.RestoreView();

            if (repeatBoss && !finished)
                ChooseNextKillTarget();
            else
                finished = true;
        }
    }

    private void ChooseNextKillTarget()
    {
        currentKillTarget = Random.Range(
            minKillsToSpawnBoss,
            maxKillsToSpawnBoss + 1
        );
    }

    private void HandleMosquitoKilled(Mosquito mosquito)
    {
        if (bossActive || finished || escapeStarted)
            return;

        killCount++;

        Debug.Log(
            $"กำจัดยุง: {killCount}/{currentKillTarget}"
        );

        if (killCount >= currentKillTarget)
            SpawnBoss();
    }

    public void SpawnBoss()
    {
        if (bossActive || finished || escapeStarted)
            return;

        if (mosquitoPrefab == null)
        {
            Debug.LogError(
                "BossSpawner: กรุณาใส่ Mosquito Prefab",
                this
            );

            return;
        }

        int randomBossHP = Random.Range(
            minBossHP,
            maxBossHP + 1
        );

        Vector3 spawnPosition = mosquitoPrefab.transform.position;

        Camera cam = Camera.main;

        if (cam != null)
        {
            float distance = Mathf.Abs(
                cam.transform.position.z -
                mosquitoPrefab.transform.position.z
            );

            spawnPosition = cam.ViewportToWorldPoint(
                new Vector3(
                    spawnViewportX,
                    spawnViewportY,
                    distance
                )
            );

            spawnPosition.z = mosquitoPrefab.transform.position.z;
        }

        SetSpawnersPaused(true);

        if (clearMosquitoesOnBossSpawn)
        {
            Mosquito[] mosquitoes =
                FindObjectsByType<Mosquito>(
                    FindObjectsSortMode.None
                );

            foreach (Mosquito mosquito in mosquitoes)
            {
                if (mosquito != null)
                    Destroy(mosquito.gameObject);
            }
        }

        GameObject bossObject = Instantiate(
            mosquitoPrefab,
            spawnPosition,
            Quaternion.identity
        );

        Mosquito normalMosquito =
            bossObject.GetComponent<Mosquito>();

        if (normalMosquito != null)
            normalMosquito.enabled = false;

        BossMosquito boss =
            bossObject.GetComponent<BossMosquito>();

        if (boss == null)
            boss = bossObject.AddComponent<BossMosquito>();

        BossSettings settings = new BossSettings();

        settings.maxHP = randomBossHP;
        settings.scaleMultiplier = bossScaleMultiplier;
        settings.flySpeed = bossFlySpeed;

        boss.Init(settings, null);

        boss.Defeated += HandleBossDefeated;

        currentBoss = boss;
        bossActive = true;
        killCount = 0;

        if (bossCamera != null)
            bossCamera.FocusOn(boss.transform);

        onBossSpawned?.Invoke();

        Debug.Log($"บอสเกิดแล้ว! สุ่ม HP ได้ {randomBossHP}");
    }

    private void HandleBossDefeated(BossMosquito boss)
    {
        if (currentBoss != boss)
            return;

        boss.Defeated -= HandleBossDefeated;

        currentBoss = null;
        bossActive = false;

        if (bossCamera != null)
            bossCamera.RestoreView();

        SetSpawnersPaused(false);

        if (repeatBoss)
            ChooseNextKillTarget();
        else
            finished = true;

        onBossDefeated?.Invoke();

        Debug.Log("บอสถูกกำจัดแล้ว!");
    }

    // เรียกจาก BreakableGlass เมื่อกระจกแตก
    public void HandleGlassBroken()
    {
        if (escapeStarted)
            return;

        if (currentBoss == null)
        {
            Debug.LogWarning(
                "BossSpawner: กระจกแตก แต่ไม่พบบอสยุงที่กำลังทำงานอยู่"
            );

            return;
        }

        escapeStarted = true;
        finished = true;
        MakeNormalMosquitoesEscape();
        // หยุดระบบสร้างยุงทันที
        SetSpawnersPaused(true);

        // ยกเลิกการโฟกัสกล้องที่อาจขัดขวางการหนี
        if (bossCamera != null)
            bossCamera.RestoreView();

        // สั่งให้บอสบินหนี แล้วให้ GameOverManager
        // แสดงหน้า Game Over หลังจากบินออกนอกจอสำเร็จ
        if (gameOverManager == null)
        {
            Debug.LogError(
                "BossSpawner: กรุณากำหนด GameOverManager ใน Inspector",
                this
            );

            return;
        }

        currentBoss.EscapeAndGameOver(gameOverManager);

        Debug.Log("กระจกแตก! บอสยุงกำลังหนีออกจากฉาก");
    }

    private void SetSpawnersPaused(bool paused)
    {
        SpawningPaused = paused;

        if (spawnersToPause == null)
            return;

        foreach (Behaviour spawner in spawnersToPause)
        {
            if (spawner != null)
                spawner.enabled = !paused;
        }
    }
    // ให้ยุงธรรมดาทุกตัวบินออกนอกจอ
    private void MakeNormalMosquitoesEscape()
    {
        Mosquito[] mosquitoes = FindObjectsByType<Mosquito>(
            FindObjectsSortMode.None
        );

        Camera cam = Camera.main;

        if (cam == null)
            return;

        foreach (Mosquito mosquito in mosquitoes)
        {
            if (mosquito == null)
                continue;

            // ไม่ให้ระบบนี้ควบคุมบอส
            if (currentBoss != null &&
                mosquito.gameObject == currentBoss.gameObject)
            {
                continue;
            }

            StartCoroutine(EscapeNormalMosquito(mosquito, cam));
        }
    }

    private IEnumerator EscapeNormalMosquito(
        Mosquito mosquito,
        Camera cam)
    {
        if (mosquito == null)
            yield break;

        Transform mosquitoTransform = mosquito.transform;

        // หยุดระบบของยุงและปิด Collider
        MonoBehaviour[] scripts =
            mosquito.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            if (script != null)
                script.enabled = false;
        }

        Collider2D[] colliders =
            mosquito.GetComponentsInChildren<Collider2D>();

        foreach (Collider2D col in colliders)
        {
            if (col != null)
                col.enabled = false;
        }

        // กำหนดเป้าหมายให้อยู่นอกขอบจอด้านขวา
        float distance = Mathf.Abs(
            cam.transform.position.z - mosquitoTransform.position.z
        );

        Vector3 target = cam.ViewportToWorldPoint(
            new Vector3(1.3f, Random.Range(0.2f, 0.9f), distance)
        );

        target.z = mosquitoTransform.position.z;

        // บินออกนอกจอ
        float escapeSpeed = 8f;

        while (mosquitoTransform != null &&
               Vector3.Distance(mosquitoTransform.position, target) > 0.1f)
        {
            mosquitoTransform.position = Vector3.MoveTowards(
                mosquitoTransform.position,
                target,
                escapeSpeed * Time.deltaTime
            );

            yield return null;
        }

        if (mosquitoTransform != null)
            Destroy(mosquitoTransform.gameObject);
    }
}