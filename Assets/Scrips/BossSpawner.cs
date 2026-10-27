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
        maxBossHP = Mathf.Max(minBossHP, maxBossHP);

        if (bossCamera == null && Camera.main != null)
            bossCamera = Camera.main.GetComponent<BossCameraFocus>();

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
        if (bossActive || finished)
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
        if (bossActive || finished)
            return;

        if (mosquitoPrefab == null)
        {
            Debug.LogError("BossSpawner: กรุณาใส่ Mosquito Prefab", this);
            return;
        }

        // สุ่ม HP ใหม่ทุกครั้งที่เรียกบอส
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

        // หยุดระบบสร้างยุงธรรมดา
        SetSpawnersPaused(true);

        // ลบยุงธรรมดาที่เหลืออยู่ก่อนสร้างบอส
        if (clearMosquitoesOnBossSpawn)
        {
            Mosquito[] mosquitoes = FindObjectsByType<Mosquito>(
                FindObjectsSortMode.None
            );

            foreach (Mosquito mosquito in mosquitoes)
            {
                if (mosquito != null)
                    Destroy(mosquito.gameObject);
            }
        }

        // ใช้ Prefab ยุงตัวเดิม
        GameObject bossObject = Instantiate(
            mosquitoPrefab,
            spawnPosition,
            Quaternion.identity
        );

        // ปิดระบบยุงธรรมดาบนตัวบอส
        Mosquito normalMosquito = bossObject.GetComponent<Mosquito>();

        if (normalMosquito != null)
            normalMosquito.enabled = false;

        // เพิ่มหรือดึงระบบบอส
        BossMosquito boss = bossObject.GetComponent<BossMosquito>();

        if (boss == null)
            boss = bossObject.AddComponent<BossMosquito>();

        // ส่ง HP ที่สุ่มได้เข้าไป
        // ตั้งค่าบอส
        BossSettings settings = new BossSettings();

        settings.maxHP = randomBossHP;
        settings.scaleMultiplier = bossScaleMultiplier;
        settings.flySpeed = bossFlySpeed;

        // ส่งค่าตั้งค่าเข้าไปในบอส
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
}