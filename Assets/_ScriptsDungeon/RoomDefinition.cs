using UnityEngine;

public enum RoomType { PlayerSpawn, Enemy, Treasure, Boss }

[CreateAssetMenu(fileName = "RoomDefinition", menuName = "Dungeon/Room Definition")]
public class RoomDefinition : ScriptableObject
{
    public RoomType roomType;

    [Header("Prefabs")]
    public GameObject[] itemPrefabs;
    public GameObject[] enemyPrefabs;

    [Header("Spawn Settings")]
    public int minItems = 0;
    public int maxItems = 3;
    public int minEnemies = 0;
    public int maxEnemies = 5;

    [Header("Placement Rules")]
    public bool spawnNearWalls = false;
    public bool spawnInOpenSpace = true;
}
