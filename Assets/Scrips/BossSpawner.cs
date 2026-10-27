using UnityEngine;
using UnityEngine.Events;

public class BossSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Prefab ยุงธรรมดา (ใช้เป็นลูกน้อง และเป็นตัวบอสถ้าไม่ใส่ Boss Prefab)")]
    public GameObject minionPrefab;

    [Tooltip("ไม่ใส่ก็ได้ ถ้าไม่ใส่จะใช้ Minion Prefab มาขยายเป็นบอส")]
    public GameObject bossPrefab;

    [Header("Spawn Condition")]
    [Min(1)] public int killsToSpawnBoss = 20;
    [Tooltip("ตายแล้วนับใหม่ เพื่อเรียกบอสรอบต่อไป")]
    public bool repeatBoss = true;

    [Tooltip("ตำแหน่งที่บอสโผล่ (Viewport 0-1)")]
    public Vector2 spawnViewportPosition = new Vector2(0.5f, 0.75f);

    [Header("Boss Settings")]
    public BossSettings boss = new BossSettings();

    [Header("Boss Fight")]
    [Tooltip("สปาวเนอร์ยุงธรรมดาที่จะปิดตอนบอสเกิด แล้วเปิดกลับตอนบอสตาย")]
    public Behaviour[] spawnersToPause;

    [Tooltip("ลบยุงธรรมดาที่เหลือบนจอทิ้งตอนบอสเกิด")]
    public bool clearMosquitoesOnBossSpawn = true;

    [Header("Events")]
    public UnityEvent onBossSpawned;
    public UnityEvent onBossDefeated;

    private int killCount;
    private BossMosquito currentBoss;
    private bool bossActive;
    private bool finished;

    public int KillCount => killCount;
    public bool BossActive => bossActive;

    // สปาวเนอร์ที่เขียนเองเช็คค่านี้ได้ (วิธีที่ชัวร์ที่สุด)
    // เช่น if (BossSpawner.SpawningPaused) return;
    public static bool SpawningPaused { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => SpawningPaused = false;

    private void OnEnable()
    {
        Mosquito.OnMosquitoKilled += HandleMosquitoKilled;
    }

    private void OnDisable()
    {
        Mosquito.OnMosquitoKilled -= HandleMosquitoKilled;

        // กันสปาวเนอร์ค้างปิดเวลาเปลี่ยน Scene
        SetSpawnersPaused(false);
    }

    private void Update()
    {
        // กันกรณีบอสถูกลบด้วยวิธีอื่น (เช่น เปลี่ยน Scene / ResetGame)
        if (bossActive && currentBoss == null)
        {
            bossActive = false;
            SetSpawnersPaused(false);
        }
    }

    private void HandleMosquitoKilled()
    {
        // ระหว่างมีบอส ไม่นับ เพื่อไม่ให้ลูกน้องทำให้บอสซ้อนกัน
        if (bossActive || finished)
            return;

        killCount++;

        if (killCount >= killsToSpawnBoss)
            SpawnBoss();
    }

    public void SpawnBoss()
    {
        GameObject prefab = bossPrefab != null ? bossPrefab : minionPrefab;

        if (prefab == null)
        {
            Debug.LogError("BossSpawner: ยังไม่ได้ใส่ Prefab");
            return;
        }

        Camera cam = Camera.main;
        Vector3 pos = prefab.transform.position;

        if (cam != null)
        {
            float distance = Mathf.Abs(
                cam.transform.position.z - prefab.transform.position.z
            );

            pos = cam.ViewportToWorldPoint(new Vector3(
                spawnViewportPosition.x,
                spawnViewportPosition.y,
                distance
            ));

            pos.z = prefab.transform.position.z;
        }

        // ลบยุงธรรมดาที่เหลือ (ทำก่อนสร้างบอส บอสและลูกน้องจะไม่โดนลบ)
        if (clearMosquitoesOnBossSpawn)
        {
            foreach (Mosquito m in FindObjectsByType<Mosquito>(FindObjectsSortMode.None))
                Destroy(m.gameObject);
        }

        // ปิดระบบเสกยุงทั้งหมดระหว่างสู้บอส
        SetSpawnersPaused(true);

        GameObject go = Instantiate(prefab, pos, Quaternion.identity);

        // ถอดสคริปต์ยุงธรรมดาออก เพราะตายด้วยการคลิกครั้งเดียว
        Mosquito normal = go.GetComponent<Mosquito>();
        if (normal != null)
            DestroyImmediate(normal);

        BossMosquito b = go.GetComponent<BossMosquito>();
        if (b == null)
            b = go.AddComponent<BossMosquito>();

        b.Init(boss, minionPrefab);
        b.Defeated += HandleBossDefeated;

        currentBoss = b;
        bossActive = true;
        killCount = 0;

        onBossSpawned?.Invoke();
    }

    private void HandleBossDefeated(BossMosquito b)
    {
        b.Defeated -= HandleBossDefeated;

        bossActive = false;
        currentBoss = null;

        // เปิดระบบเสกยุงกลับ
        SetSpawnersPaused(false);

        if (!repeatBoss)
            finished = true;

        onBossDefeated?.Invoke();
    }

    private void SetSpawnersPaused(bool paused)
    {
        SpawningPaused = paused;

        if (spawnersToPause == null)
            return;

        foreach (Behaviour b in spawnersToPause)
        {
            if (b != null)
                b.enabled = !paused;
        }
    }
}