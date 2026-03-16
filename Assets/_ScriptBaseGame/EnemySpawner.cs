using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float baseSpawnInterval = 5f;

    private DifficultyManager difficultyManager;
    private float timer;

    public void Initialize(DifficultyManager dm)
    {
        difficultyManager = dm;
        Debug.Log($"[Spawner] Initialized with DifficultyManager: {(dm != null)}");
    }

    void Awake()
    {
        // Fallback if Initialize() wasn’t called
        if (difficultyManager == null)
        {
            difficultyManager = UnityEngine.Object.FindFirstObjectByType<DifficultyManager>();
            Debug.Log("[Spawner] Auto-found DifficultyManager in Awake.");
        }
    }

    void Update()
    {
        if (difficultyManager == null)
        {
            Debug.LogWarning("[Spawner] No DifficultyManager found, cannot spawn.");
            return;
        }

        timer += Time.deltaTime;
        float currentInterval = baseSpawnInterval / difficultyManager.DifficultyMultiplier;

        if (timer >= currentInterval)
        {
            Debug.Log($"[Spawner] Timer hit {currentInterval:F2}s, spawning enemy.");
            SpawnEnemy();
            timer = 0f;
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("[Spawner] No enemyPrefab assigned!");
            return;
        }

        Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        Debug.Log($"[Spawner] Enemy spawned at {transform.position}");
    }
}
