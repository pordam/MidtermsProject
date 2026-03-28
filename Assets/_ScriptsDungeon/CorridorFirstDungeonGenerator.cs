using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Pathfinding;

[Serializable]
public class RoomData
{
    public RoomDefinition definition;
    public HashSet<Vector2Int> floorTiles;
}

public class CorridorFirstDungeonGenerator : SimpleRandomWalkDungeonGenerator
{
    [SerializeField] private int corridorLength = 14, corridorCount = 5;
    [SerializeField][Range(0.1f, 1)] private float roomPercent = 0.8f;
    [SerializeField] private GameObject player;

    [SerializeField] private RoomDefinition playerSpawnDefinition;
    [SerializeField] private RoomDefinition enemyRoomDefinition;
    [SerializeField] private RoomDefinition treasureRoomDefinition;
    [SerializeField] private RoomDefinition bossRoomDefinition;
    [SerializeField] private int corridorWidth = 1; // tweakable in Inspector

    [SerializeField] private GameObject enemySpawnerPrefab;


    private Dictionary<Vector2Int, HashSet<Vector2Int>> roomsDictionary = new Dictionary<Vector2Int, HashSet<Vector2Int>>();
    private HashSet<Vector2Int> currentFloorPositions;

    protected override void RunProceduralGeneration()
    {
        Debug.Log("[DungeonGen] Starting procedural generation...");

        CorridorFirstGeneration();

        if (currentFloorPositions == null || currentFloorPositions.Count == 0)
        {
            Debug.LogError("[DungeonGen] No floor positions generated!");
        }
        else
        {
            Debug.Log($"[DungeonGen] Generated {currentFloorPositions.Count} floor tiles.");
        }

        StartCoroutine(DelayedGraphUpdate());
    }


    private IEnumerator DelayedGraphUpdate()
    {
        // Wait more than one frame if colliders need time to bake
        yield return new WaitForSeconds(0.5f);
        UpdateGraphBounds(currentFloorPositions);
    }

    private void CorridorFirstGeneration()
    {
        Debug.Log("[DungeonGen] CorridorFirstGeneration called.");

        HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
        HashSet<Vector2Int> potentialRoomPositions = new HashSet<Vector2Int>();

        CreateCorridors(floorPositions, potentialRoomPositions);
        Debug.Log($"[DungeonGen] Corridors created. Potential room positions: {potentialRoomPositions.Count}");

        HashSet<Vector2Int> roomPositions = CreateRooms(potentialRoomPositions);
        Debug.Log($"[DungeonGen] Rooms created. Room count: {roomsDictionary.Count}");

        List<Vector2Int> deadEnds = FindAllDeadEnds(floorPositions);
        Debug.Log($"[DungeonGen] Found {deadEnds.Count} dead ends.");

        CreateRoomsAtDeadEnd(deadEnds, roomPositions);
        Debug.Log($"[DungeonGen] After dead ends, total rooms: {roomsDictionary.Count}");

        floorPositions.UnionWith(roomPositions);

        tilemapVisualizer.PaintFloorTiles(floorPositions);
        WallGenerator.CreateWalls(floorPositions, tilemapVisualizer);

        currentFloorPositions = floorPositions;

        var rooms = AssignRoomTypes(roomsDictionary);
        Debug.Log($"[DungeonGen] Assigned {rooms.Count} room types.");

        PopulateRooms(rooms);
    }

    private void CreateRoomsAtDeadEnd(List<Vector2Int> deadEnds, HashSet<Vector2Int> roomFloors)
    {
        foreach (var position in deadEnds)
        {
            if (!roomFloors.Contains(position))
            {
                var room = RunRandomWalk(randomWalkParameters, position);
                roomFloors.UnionWith(room);
                roomsDictionary[position] = room;
            }
        }
    }

    private List<Vector2Int> FindAllDeadEnds(HashSet<Vector2Int> floorPositions)
    {
        List<Vector2Int> deadEnds = new List<Vector2Int>();
        foreach (var position in floorPositions)
        {
            int neighboursCount = 0;
            foreach (var direction in Direction2D.cardinalDirectionsList)
            {
                if (floorPositions.Contains(position + direction))
                    neighboursCount++;
            }
            if (neighboursCount == 1)
                deadEnds.Add(position);
        }
        return deadEnds;
    }

    private HashSet<Vector2Int> CreateRooms(HashSet<Vector2Int> potentialRoomPositions)
    {
        HashSet<Vector2Int> roomPositions = new HashSet<Vector2Int>();
        int roomToCreateCount = Mathf.RoundToInt(potentialRoomPositions.Count * roomPercent);

        List<Vector2Int> roomsToCreate = potentialRoomPositions
            .OrderBy(x => Guid.NewGuid())
            .Take(roomToCreateCount)
            .ToList();

        foreach (var roomPosition in roomsToCreate)
        {
            var roomFloor = RunRandomWalk(randomWalkParameters, roomPosition);
            roomPositions.UnionWith(roomFloor);
            roomsDictionary[roomPosition] = roomFloor;
        }

        // Only force extra rooms if we ended up with fewer than 5
        while (roomsDictionary.Count < 5 && potentialRoomPositions.Count > 0)
        {
            var extraRoomPos = potentialRoomPositions.ElementAt(UnityEngine.Random.Range(0, potentialRoomPositions.Count));
            var extraRoom = RunRandomWalk(randomWalkParameters, extraRoomPos);
            roomPositions.UnionWith(extraRoom);
            roomsDictionary[extraRoomPos] = extraRoom;
        }

        Debug.Log($"[DungeonGen] Final room count: {roomsDictionary.Count}");
        return roomPositions;
    }

    private void CreateCorridors(HashSet<Vector2Int> floorPositions, HashSet<Vector2Int> potentialRoomPositions)
    {
        var currentPosition = startPosition;
        potentialRoomPositions.Add(currentPosition);

        for (int i = 0; i < corridorCount; i++)
        {
            var corridor = ProceduralGenerationAlgorithms.RandomWalkCorridor(currentPosition, corridorLength, corridorWidth);
            currentPosition = corridor[corridor.Count - 1];
            potentialRoomPositions.Add(currentPosition);
            floorPositions.UnionWith(corridor);
        }
    }


    // --- Room assignment and prefab spawning ---
    private List<RoomData> AssignRoomTypes(Dictionary<Vector2Int, HashSet<Vector2Int>> rooms)
    {
        List<RoomData> roomDataList = new List<RoomData>();

        // Fallback: if no rooms exist, use corridors as a "room"
        if (rooms.Count == 0 && currentFloorPositions != null && currentFloorPositions.Count > 0)
        {
            var fallbackRoom = new RoomData
            {
                definition = playerSpawnDefinition,
                floorTiles = currentFloorPositions
            };
            roomDataList.Add(fallbackRoom);
            return roomDataList;
        }

        // Player spawn = first room
        var firstRoom = rooms.First();
        roomDataList.Add(new RoomData { definition = playerSpawnDefinition, floorTiles = firstRoom.Value });

        // Boss room = furthest from start
        var furthestRoom = rooms.OrderByDescending(r => Vector2Int.Distance(startPosition, r.Key)).First();
        roomDataList.Add(new RoomData { definition = bossRoomDefinition, floorTiles = furthestRoom.Value });

        // Remaining rooms = enemy/treasure mix
        foreach (var room in rooms)
        {
            if (room.Key == firstRoom.Key || room.Key == furthestRoom.Key) continue;

            RoomDefinition def = (UnityEngine.Random.value > 0.5f) ? enemyRoomDefinition : treasureRoomDefinition;
            roomDataList.Add(new RoomData { definition = def, floorTiles = room.Value });
        }

        // --- Inject treasure room if none exists ---
        if (!roomDataList.Any(r => r.definition.roomType == RoomType.Treasure))
        {
            var candidate = roomDataList
                .Where(r => r.definition.roomType != RoomType.PlayerSpawn && r.definition.roomType != RoomType.Boss)
                .OrderBy(r => UnityEngine.Random.value)
                .FirstOrDefault();

            if (candidate != null)
                candidate.definition = treasureRoomDefinition;
        }

        return roomDataList;
    }

    private void PopulateRooms(List<RoomData> rooms)
    {
        foreach (var room in rooms)
        {
            Debug.Log($"Room type: {room.definition.roomType}, tiles: {room.floorTiles.Count}");
            if (room.definition.roomType == RoomType.PlayerSpawn)
            {
                TeleportPlayer(room);
            }
            else
            {
                SpawnPrefabs(room);
            }
        }
    }

    private void TeleportPlayer(RoomData room)
    {
        if (player == null) return;
        Vector2Int center = room.floorTiles.ElementAt(room.floorTiles.Count / 2);
        player.transform.position = new Vector3(center.x, center.y, 0);

        CameraFollow camFollow = GameObject.FindFirstObjectByType<CameraFollow>();
        if (camFollow != null) camFollow.SetTarget(player.transform);
    }

    // --- Graph update ---
    private void UpdateGraphBounds(HashSet<Vector2Int> floorPositions)
    {
        if (floorPositions == null || floorPositions.Count == 0) return;

        int minX = floorPositions.Min(p => p.x);
        int maxX = floorPositions.Max(p => p.x);
        int minY = floorPositions.Min(p => p.y);
        int maxY = floorPositions.Max(p => p.y);

        // Calculate bounds
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0);
        int width = (maxX - minX) + 10;
        int height = (maxY - minY) + 10;

        // Get the GridGraph
        var gg = AstarPath.active.data.gridGraph;
        if (gg != null)
        {
            // Update graph dimensions
            gg.center = center;
            gg.SetDimensions(width, height, gg.nodeSize);

            // Rescan to apply changes
            AstarPath.active.Scan(gg);
            Debug.Log($"Resized GridGraph to width={width}, height={height}, center={center}");
        }
    }

    private void SpawnPrefabs(RoomData room)
    {
        if (room.definition == null)
        {
            Debug.LogWarning("Room has no definition, skipping spawn.");
            return;
        }

        Debug.Log($"Spawning prefabs in room type {room.definition.roomType}");

        if (room.definition.roomType == RoomType.Enemy && enemySpawnerPrefab != null)
        {
            Vector2Int center = room.floorTiles.ElementAt(room.floorTiles.Count / 2);
            Debug.Log($"[Generator] Placing EnemySpawner in room at {center}");

            var spawnerObj = Instantiate(enemySpawnerPrefab, new Vector3(center.x, center.y, 0), Quaternion.identity);
            var spawner = spawnerObj.GetComponent<EnemySpawner>();
            spawner.Initialize(UnityEngine.Object.FindFirstObjectByType<DifficultyManager>());
        }

        if (room.definition.roomType == RoomType.Treasure && room.definition.itemPrefabs != null)
        {
            // Always spawn at least one chest
            var chestPrefabs = room.definition.itemPrefabs
                .Where(p => p.name.Contains("Chest")) // or tag them explicitly
                .ToList();

            if (chestPrefabs.Count > 0)
            {
                // Guarantee at least one chest
                Vector2Int pos = PickSpawnPosition(room.floorTiles, true);
                Instantiate(chestPrefabs[0], new Vector3(pos.x, pos.y, 0), Quaternion.identity);

                // Optionally spawn more chests if minItems/maxItems > 1
                int extraChestCount = UnityEngine.Random.Range(room.definition.minItems, room.definition.maxItems + 1) - 1;
                for (int i = 0; i < extraChestCount; i++)
                {
                    Vector2Int extraPos = PickSpawnPosition(room.floorTiles, true);
                    var prefab = chestPrefabs[UnityEngine.Random.Range(0, chestPrefabs.Count)];
                    Instantiate(prefab, new Vector3(extraPos.x, extraPos.y, 0), Quaternion.identity);
                }
            }
            else
            {
                Debug.LogWarning("Treasure room has no chest prefab assigned!");
            }
        }

        // Items
        if (room.definition.itemPrefabs != null && room.definition.itemPrefabs.Length > 0)
        {
            int itemCount = UnityEngine.Random.Range(room.definition.minItems, room.definition.maxItems + 1);
            for (int i = 0; i < itemCount; i++)
            {
                Vector2Int pos = PickSpawnPosition(room.floorTiles, room.definition.spawnNearWalls);
                var prefab = room.definition.itemPrefabs[UnityEngine.Random.Range(0, room.definition.itemPrefabs.Length)];
                Debug.Log($"Instantiating item {prefab.name} at {pos}");
                Instantiate(prefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
            }
        }
        else
        {
            Debug.LogWarning($"No item prefabs assigned for {room.definition.roomType}");
        }

        // Enemies
        if (room.definition.enemyPrefabs != null && room.definition.enemyPrefabs.Length > 0)
        {
            int enemyCount = UnityEngine.Random.Range(room.definition.minEnemies, room.definition.maxEnemies + 1);
            for (int i = 0; i < enemyCount; i++)
            {
                Vector2Int pos = PickSpawnPosition(room.floorTiles, room.definition.spawnInOpenSpace);
                var prefab = room.definition.enemyPrefabs[UnityEngine.Random.Range(0, room.definition.enemyPrefabs.Length)];
                Debug.Log($"Instantiating enemy {prefab.name} at {pos}");
                Instantiate(prefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity);
            }
        }
        else
        {
            Debug.LogWarning($"No enemy prefabs assigned for {room.definition.roomType}");
        }
    }

    private Vector2Int PickSpawnPosition(HashSet<Vector2Int> tiles, bool preferNearWall)
    {
        var candidates = tiles.Where(p => preferNearWall ? IsNearWall(p, tiles) : !IsNearWall(p, tiles)).ToList();
        if (candidates.Count == 0) candidates = tiles.ToList();
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private bool IsNearWall(Vector2Int pos, HashSet<Vector2Int> roomTiles)
    {
        int neighbours = 0;
        foreach (var dir in Direction2D.eightDirectionsList)
        {
            if (roomTiles.Contains(pos + dir)) neighbours++;
        }
        return neighbours < 8;
    }
}
