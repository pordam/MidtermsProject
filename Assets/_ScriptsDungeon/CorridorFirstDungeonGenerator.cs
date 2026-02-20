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


    private Dictionary<Vector2Int, HashSet<Vector2Int>> roomsDictionary = new Dictionary<Vector2Int, HashSet<Vector2Int>>();
    private HashSet<Vector2Int> currentFloorPositions;

    protected override void RunProceduralGeneration()
    {
        CorridorFirstGeneration();
        StartCoroutine(DelayedGraphUpdate());
    }

    private IEnumerator DelayedGraphUpdate()
    {
        // Wait more than one frame if colliders need time to bake
        yield return new WaitForSeconds(0.1f);
        UpdateGraphBounds(currentFloorPositions);
    }

    private void CorridorFirstGeneration()
    {
        HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
        HashSet<Vector2Int> potentialRoomPositions = new HashSet<Vector2Int>();

        CreateCorridors(floorPositions, potentialRoomPositions);
        HashSet<Vector2Int> roomPositions = CreateRooms(potentialRoomPositions);
        List<Vector2Int> deadEnds = FindAllDeadEnds(floorPositions);
        CreateRoomsAtDeadEnd(deadEnds, roomPositions);

        floorPositions.UnionWith(roomPositions);

        tilemapVisualizer.PaintFloorTiles(floorPositions);
        WallGenerator.CreateWalls(floorPositions, tilemapVisualizer);

        currentFloorPositions = floorPositions;

        var rooms = AssignRoomTypes(roomsDictionary);
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

        List<Vector2Int> roomsToCreate = potentialRoomPositions.OrderBy(x => Guid.NewGuid()).Take(roomToCreateCount).ToList();

        foreach (var roomPosition in roomsToCreate)
        {
            var roomFloor = RunRandomWalk(randomWalkParameters, roomPosition);
            roomPositions.UnionWith(roomFloor);
            roomsDictionary[roomPosition] = roomFloor;
        }
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

        // Remaining rooms = enemy/treasure
        foreach (var room in rooms)
        {
            if (room.Key == firstRoom.Key || room.Key == furthestRoom.Key) continue;

            RoomDefinition def = (UnityEngine.Random.value > 0.5f) ? enemyRoomDefinition : treasureRoomDefinition;
            roomDataList.Add(new RoomData { definition = def, floorTiles = room.Value });
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

        // Items
        if (room.definition.itemPrefabs != null && room.definition.itemPrefabs.Length > 0)
        {
            int itemCount = UnityEngine.Random.Range(room.definition.minItems, room.definition.maxItems + 1);
            for (int i = 0; i < itemCount; i++)
            {
                Vector2Int pos = PickSpawnPosition(room.floorTiles, room.definition.spawnNearWalls);
                var prefab = room.definition.itemPrefabs[UnityEngine.Random.Range(0, room.definition.itemPrefabs.Length)];
                Debug.Log($"Instantiating item {prefab.name} at {pos}");
                Instantiate(prefab, new Vector3(pos.x, pos.y, -1), Quaternion.identity);
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
                Instantiate(prefab, new Vector3(pos.x, pos.y, -1), Quaternion.identity);
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
