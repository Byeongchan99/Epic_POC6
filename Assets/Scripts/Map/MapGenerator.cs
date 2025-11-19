using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    [SerializeField] private int mapWidth = 100;
    [SerializeField] private int mapHeight = 100;
    [SerializeField] private int seed = 0;

    [Header("Noise Settings - Continent")]
    [SerializeField] private float continentScale = 20f;
    [SerializeField] private float continentThreshold = 0.4f;

    [Header("Noise Settings - Holes")]
    [SerializeField] private float holeScale = 10f;
    [SerializeField] private float holeWeight = 0.3f;

    [Header("Prefabs")]
    [SerializeField] private GameObject landTilePrefab;
    [SerializeField] private GameObject waterTilePrefab; // Optional for visualization

    [Header("References")]
    [SerializeField] private Transform mapParent;

    // Map data
    private int[,] mapData;
    private List<Vector2Int> landTiles = new List<Vector2Int>();

    public enum TileType
    {
        Water = 0,
        Land = 1
    }

    void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        // Set random seed
        if (seed == 0)
            seed = Random.Range(0, 10000);

        Random.InitState(seed);

        // Generate map data using Multi-Layer Perlin Noise
        mapData = new int[mapWidth, mapHeight];
        GenerateMapData();

        // Spawn tiles
        SpawnTiles();

        // Find all land tiles for mission placement
        FindLandTiles();

        Debug.Log($"Map generated with seed: {seed}");
        Debug.Log($"Total land tiles: {landTiles.Count}");
    }

    private void GenerateMapData()
    {
        float offsetX = Random.Range(0f, 10000f);
        float offsetY = Random.Range(0f, 10000f);

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                // First layer: Continent shape (large scale)
                float continentNoise = Mathf.PerlinNoise(
                    (x + offsetX) / continentScale,
                    (y + offsetY) / continentScale
                );

                // Second layer: Holes/Lakes (smaller scale)
                float holeNoise = Mathf.PerlinNoise(
                    (x + offsetX) / holeScale,
                    (y + offsetY) / holeScale
                );

                // Combine noises
                // Reduce continent noise where hole noise is high
                float combined = continentNoise * (1f - holeNoise * holeWeight);

                // Determine tile type
                if (combined > continentThreshold)
                {
                    mapData[x, y] = (int)TileType.Land;
                }
                else
                {
                    mapData[x, y] = (int)TileType.Water;
                }
            }
        }
    }

    private void SpawnTiles()
    {
        if (mapParent == null)
        {
            GameObject mapObj = new GameObject("Map");
            mapParent = mapObj.transform;
        }

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3 position = new Vector3(x, 0, y);
                GameObject tilePrefab = null;

                if (mapData[x, y] == (int)TileType.Land)
                {
                    tilePrefab = landTilePrefab;
                }
                else if (waterTilePrefab != null)
                {
                    tilePrefab = waterTilePrefab;
                }

                if (tilePrefab != null)
                {
                    GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, mapParent);
                    tile.name = $"Tile_{x}_{y}";

                    // Set static for optimization
                    tile.isStatic = true;
                }
            }
        }

        // Apply static batching to all tiles
        StaticBatchingUtility.Combine(mapParent.gameObject);
        Debug.Log("Static batching applied to map tiles");
    }

    private void FindLandTiles()
    {
        landTiles.Clear();

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (mapData[x, y] == (int)TileType.Land)
                {
                    landTiles.Add(new Vector2Int(x, y));
                }
            }
        }
    }

    // Public methods for other systems to use
    public bool IsTileLand(int x, int y)
    {
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
            return false;

        return mapData[x, y] == (int)TileType.Land;
    }

    public bool IsTileLand(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x);
        int y = Mathf.RoundToInt(worldPosition.z);
        return IsTileLand(x, y);
    }

    public Vector3 GetRandomLandPosition()
    {
        if (landTiles.Count == 0)
        {
            Debug.LogError("No land tiles available!");
            return Vector3.zero;
        }

        Vector2Int randomTile = landTiles[Random.Range(0, landTiles.Count)];
        return new Vector3(randomTile.x, 0, randomTile.y);
    }

    public bool CanPlaceMission(Vector3 center, Vector2Int missionSize)
    {
        int centerX = Mathf.RoundToInt(center.x);
        int centerY = Mathf.RoundToInt(center.z);

        int halfWidth = missionSize.x / 2;
        int halfHeight = missionSize.y / 2;

        // Check if all tiles in the mission area are land
        for (int x = centerX - halfWidth; x <= centerX + halfWidth; x++)
        {
            for (int y = centerY - halfHeight; y <= centerY + halfHeight; y++)
            {
                if (!IsTileLand(x, y))
                    return false;
            }
        }

        return true;
    }

    public Vector3 FindMissionPlacementPosition(Vector2Int missionSize, int maxAttempts = 100)
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 randomPos = GetRandomLandPosition();

            if (CanPlaceMission(randomPos, missionSize))
            {
                return randomPos;
            }
        }

        Debug.LogWarning($"Could not find suitable position for mission of size {missionSize} after {maxAttempts} attempts");
        return Vector3.zero;
    }

    public int[,] GetMapData()
    {
        return mapData;
    }

    public int GetMapWidth()
    {
        return mapWidth;
    }

    public int GetMapHeight()
    {
        return mapHeight;
    }
}
