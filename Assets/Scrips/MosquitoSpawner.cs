using UnityEngine;

public class MosquitoSpawner : MonoBehaviour
{
    [Header("Mosquito")]
    public GameObject mosquitoPrefab;

    [Header("Spawn Settings")]
    public float spawnInterval = 2f;
    public int maxMosquitoes = 10;

    [Range(0f, 1f)]
    public float specialChance = 0.1f;

    private float spawnTimer;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("MosquitoSpawner: ไม่พบ Main Camera!");
            enabled = false;
            return;
        }

        // ถ้าเริ่มเกมมาแล้วบอสอยู่ ก็ไม่ต้องเสก
        if (!BossSpawner.SpawningPaused)
            SpawnMosquito();

        spawnTimer = spawnInterval;
    }

    void Update()
    {
        if (mosquitoPrefab == null)
            return;

        // ระหว่างสู้บอส หยุดเสกยุงทั้งหมด
        // รีเซ็ตเวลาไว้ด้วย เพื่อให้มีช่วงพักหลังบอสตายก่อนยุงเริ่มเกิดใหม่
        if (BossSpawner.SpawningPaused)
        {
            spawnTimer = spawnInterval;
            return;
        }

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            int count = FindObjectsByType<Mosquito>(
                FindObjectsSortMode.None
            ).Length;

            if (count < maxMosquitoes)
                SpawnMosquito();

            spawnTimer = spawnInterval;
        }
    }

    void SpawnMosquito()
    {
        float zDistance = Mathf.Abs(
            mainCamera.transform.position.z
        );

        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(
            new Vector3(0f, 0f, zDistance)
        );

        Vector3 topRight = mainCamera.ViewportToWorldPoint(
            new Vector3(1f, 1f, zDistance)
        );

        Vector3 spawnPosition;

        int side = Random.Range(0, 4);

        switch (side)
        {
            case 0:
                spawnPosition = new Vector3(
                    bottomLeft.x - 0.5f,
                    Random.Range(bottomLeft.y, topRight.y),
                    0f
                );
                break;

            case 1:
                spawnPosition = new Vector3(
                    topRight.x + 0.5f,
                    Random.Range(bottomLeft.y, topRight.y),
                    0f
                );
                break;

            case 2:
                spawnPosition = new Vector3(
                    Random.Range(bottomLeft.x, topRight.x),
                    bottomLeft.y - 0.5f,
                    0f
                );
                break;

            default:
                spawnPosition = new Vector3(
                    Random.Range(bottomLeft.x, topRight.x),
                    topRight.y + 0.5f,
                    0f
                );
                break;
        }

        GameObject mosquitoObject = Instantiate(
            mosquitoPrefab,
            spawnPosition,
            Quaternion.identity
        );

        Mosquito mosquito = mosquitoObject.GetComponent<Mosquito>();

        if (mosquito != null)
        {
            bool special = Random.value < specialChance;
            mosquito.Setup(special);
        }
        else
        {
            Debug.LogError(
                "Mosquito Prefab ต้องมี Mosquito.cs!"
            );
        }
    }
}