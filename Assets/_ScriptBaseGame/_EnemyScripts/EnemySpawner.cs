using UnityEngine;

public class EnemySpawner : MonoBehaviour
{

    private DifficultyManager difficultyManager;

    public enum SpawnAreaMode { Perimeter, Inside }
    [Header("Spawn Around Collider")]
    public bool spawnAroundCollider = true;                 // toggle collider-based spawning
    public SpawnAreaMode spawnMode = SpawnAreaMode.Perimeter;
    public BoxCollider2D boundsCollider;                    // optional: assign a BoxCollider2D
    public CircleCollider2D circleCollider;                 // optional: assign a CircleCollider2D
    [Tooltip("Distance outward from the collider when using Perimeter mode")]
    public float perimeterOffset = 0.5f;
    [Tooltip("Padding inside the collider when using Inside mode")]
    public float insidePadding = 0.1f;
    [Tooltip("Fallback radius around this transform when no collider is assigned")]
    public float fallbackRadius = 3f;

    [SerializeField] private GameObject[] enemyPrefabs; // assign multiple prefabs in Inspector

    // last spawn position for debug visualization
    private Vector3 lastSpawnPos = Vector3.zero;

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

        // If user assigned only one collider type, prefer that
        if (boundsCollider == null && circleCollider != null)
        {
            // nothing to do, circle assigned
        }
        else if (circleCollider == null && boundsCollider != null)
        {
            // nothing to do, box assigned
        }
    }

    // Called by WaveManager
    public GameObject SpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("[Spawner] No enemyPrefabs assigned!");
            return null;
        }

        // pick a random prefab
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        Vector3 spawnPos = GetSpawnPosition();
        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        lastSpawnPos = spawnPos;
        Debug.Log($"[Spawner] Enemy spawned at {spawnPos} using prefab {prefab.name}");
        return enemy;
    }

    // Compute spawn position according to collider settings or fallback
    private Vector3 GetSpawnPosition()
    {
        // Use BoxCollider2D if assigned and spawnAroundCollider is true
        if (spawnAroundCollider && boundsCollider != null)
        {
            Bounds b = boundsCollider.bounds;
            if (spawnMode == SpawnAreaMode.Perimeter)
            {
                // choose a random edge and a point along it, then offset outward
                int side = Random.Range(0, 4); // 0=bottom,1=right,2=top,3=left
                float t = Random.value;
                Vector3 point = Vector3.zero;
                Vector3 outward = Vector3.zero;

                switch (side)
                {
                    case 0: // bottom
                        point = new Vector3(Mathf.Lerp(b.min.x, b.max.x, t), b.min.y, transform.position.z);
                        outward = Vector3.down;
                        break;
                    case 1: // right
                        point = new Vector3(b.max.x, Mathf.Lerp(b.min.y, b.max.y, t), transform.position.z);
                        outward = Vector3.right;
                        break;
                    case 2: // top
                        point = new Vector3(Mathf.Lerp(b.min.x, b.max.x, t), b.max.y, transform.position.z);
                        outward = Vector3.up;
                        break;
                    case 3: // left
                        point = new Vector3(b.min.x, Mathf.Lerp(b.min.y, b.max.y, t), transform.position.z);
                        outward = Vector3.left;
                        break;
                }

                // apply offset in world space
                Vector3 spawn = point + outward.normalized * perimeterOffset;
                spawn.z = transform.position.z;
                return spawn;
            }
            else // Inside
            {
                float minX = b.min.x + insidePadding;
                float maxX = b.max.x - insidePadding;
                float minY = b.min.y + insidePadding;
                float maxY = b.max.y - insidePadding;

                // clamp in case padding is too large
                if (minX > maxX) { float mid = (b.min.x + b.max.x) * 0.5f; minX = maxX = mid; }
                if (minY > maxY) { float mid = (b.min.y + b.max.y) * 0.5f; minY = maxY = mid; }

                Vector3 spawn = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), transform.position.z);
                return spawn;
            }
        }

        // Use CircleCollider2D if assigned and spawnAroundCollider is true
        if (spawnAroundCollider && circleCollider != null)
        {
            Vector3 center = circleCollider.bounds.center;
            // compute world radius considering transform scale
            float radius = circleCollider.radius * Mathf.Max(circleCollider.transform.lossyScale.x, circleCollider.transform.lossyScale.y);

            if (spawnMode == SpawnAreaMode.Perimeter)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                Vector3 spawn = center + dir * (radius + perimeterOffset);
                spawn.z = transform.position.z;
                return spawn;
            }
            else // Inside
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                // uniform distribution inside circle
                float r = radius * Mathf.Sqrt(Random.value);
                Vector3 dir = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                Vector3 spawn = center + dir * r;
                spawn.z = transform.position.z;
                return spawn;
            }
        }

        // Fallback: random point around transform within fallbackRadius
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float r = Random.Range(0f, fallbackRadius);
            Vector3 spawn = transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * r;
            spawn.z = transform.position.z;
            return spawn;
        }
    }

    // Debug visualization in editor and play mode
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        if (spawnAroundCollider && boundsCollider != null)
        {
            Gizmos.DrawWireCube(boundsCollider.bounds.center, boundsCollider.bounds.size);

            // draw padded inset
            Vector3 paddedSize = new Vector3(
                Mathf.Max(0f, boundsCollider.bounds.size.x - insidePadding * 2f),
                Mathf.Max(0f, boundsCollider.bounds.size.y - insidePadding * 2f),
                boundsCollider.bounds.size.z
            );
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.5f);
            Gizmos.DrawWireCube(boundsCollider.bounds.center, paddedSize);
        }
        else if (spawnAroundCollider && circleCollider != null)
        {
            Gizmos.DrawWireSphere(circleCollider.bounds.center, circleCollider.radius * Mathf.Max(circleCollider.transform.lossyScale.x, circleCollider.transform.lossyScale.y));
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, fallbackRadius);
        }

        // last spawn position
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(lastSpawnPos, 0.15f);
    }
}
