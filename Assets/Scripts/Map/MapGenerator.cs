using UnityEngine;
using System.Collections.Generic;

#if UNITY_AI_NAVIGATION
using Unity.AI.Navigation;
#endif

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
    [SerializeField] private GameObject waterTilePrefab;

    [Header("Mission Zones")]
    [SerializeField] private List<GameObject> missionZonePrefabs = new List<GameObject>();
    [SerializeField] private int missionZoneSpacing = 20; // Minimum distance between zones

    [Header("Optimization")]
    [SerializeField] private bool optimizeMesh = true;
    [SerializeField] private float waterWallHeight = 5f;

    [Header("References")]
    [SerializeField] private Transform mapParent;

    // Map data
    private int[,] mapData;
    private List<Vector2Int> landTiles = new List<Vector2Int>();
    private List<MissionZoneData> placedMissionZones = new List<MissionZoneData>();

    public enum TileType
    {
        Water = 0,
        Land = 1
    }

    private class MissionZoneData
    {
        public Vector2Int position;
        public Vector2Int size;
        public GameObject instance;
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

        // Place mission zones and carve out land for them
        PlaceMissionZones();

        // Spawn tiles
        SpawnTiles();

        // Optimize mesh
        if (optimizeMesh)
        {
            OptimizeMap();
        }

        // Find all land tiles
        FindLandTiles();

        Debug.Log($"Map generated with seed: {seed}");
        Debug.Log($"Total land tiles: {landTiles.Count}");
        Debug.Log($"Mission zones placed: {placedMissionZones.Count}");
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

    private void PlaceMissionZones()
    {
        if (missionZonePrefabs == null || missionZonePrefabs.Count == 0)
        {
            Debug.LogWarning("No mission zone prefabs assigned!");
            return;
        }

        placedMissionZones.Clear();

        foreach (GameObject zonePrefab in missionZonePrefabs)
        {
            if (zonePrefab == null) continue;

            // Get mission zone size from MissionZoneInfo component
            MissionZoneInfo zoneInfo = zonePrefab.GetComponent<MissionZoneInfo>();
            Vector2Int zoneSize = zoneInfo != null ? zoneInfo.size : new Vector2Int(15, 15);

            // Find valid position
            Vector2Int position = FindMissionZonePosition(zoneSize);

            if (position != Vector2Int.zero)
            {
                // Carve out land for this mission zone
                CarveLandForMissionZone(position, zoneSize);

                // Store data (will instantiate after tiles are spawned)
                placedMissionZones.Add(new MissionZoneData
                {
                    position = position,
                    size = zoneSize,
                    instance = zonePrefab
                });

                Debug.Log($"Mission zone placement reserved at {position} with size {zoneSize}");
            }
            else
            {
                Debug.LogWarning($"Could not find valid position for mission zone of size {zoneSize}");
            }
        }
    }

    private Vector2Int FindMissionZonePosition(Vector2Int size)
    {
        int maxAttempts = 100;

        for (int i = 0; i < maxAttempts; i++)
        {
            int x = Random.Range(size.x / 2, mapWidth - size.x / 2);
            int y = Random.Range(size.y / 2, mapHeight - size.y / 2);

            Vector2Int candidate = new Vector2Int(x, y);

            // Check if position is valid (not too close to other zones)
            bool tooClose = false;
            foreach (var zone in placedMissionZones)
            {
                float distance = Vector2Int.Distance(candidate, zone.position);
                if (distance < missionZoneSpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                return candidate;
            }
        }

        return Vector2Int.zero;
    }

    private void CarveLandForMissionZone(Vector2Int center, Vector2Int size)
    {
        int halfWidth = size.x / 2;
        int halfHeight = size.y / 2;

        for (int x = center.x - halfWidth; x <= center.x + halfWidth; x++)
        {
            for (int y = center.y - halfHeight; y <= center.y + halfHeight; y++)
            {
                if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
                {
                    mapData[x, y] = (int)TileType.Land;
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

                if (mapData[x, y] == (int)TileType.Land)
                {
                    // Spawn Land tile
                    if (landTilePrefab != null)
                    {
                        GameObject tile = Instantiate(landTilePrefab, position, Quaternion.identity, mapParent);
                        tile.name = $"LandTile_{x}_{y}";
                        tile.tag = "Terrain";
                        tile.isStatic = true;
                    }
                }
                else // Water
                {
                    // Spawn Water wall (invisible barrier)
                    if (waterTilePrefab != null)
                    {
                        GameObject waterTile = Instantiate(waterTilePrefab, position, Quaternion.identity, mapParent);
                        waterTile.name = $"WaterWall_{x}_{y}";
                        waterTile.tag = "Wall";
                        waterTile.isStatic = true;

                        // Add or modify BoxCollider to make it a wall
                        BoxCollider collider = waterTile.GetComponent<BoxCollider>();
                        if (collider == null)
                            collider = waterTile.AddComponent<BoxCollider>();

                        collider.center = new Vector3(0, waterWallHeight / 2, 0);
                        collider.size = new Vector3(1, waterWallHeight, 1);

                        // Make renderer invisible (optional: keep visible for debugging)
                        MeshRenderer renderer = waterTile.GetComponent<MeshRenderer>();
                        if (renderer != null)
                            renderer.enabled = false; // Set to true to see water
                    }
                }
            }
        }

        Debug.Log("Tiles spawned");

        // Instantiate mission zone prefabs after tiles
        SpawnMissionZonePrefabs();
    }

    private void SpawnMissionZonePrefabs()
    {
        foreach (var zoneData in placedMissionZones)
        {
            Vector3 worldPos = new Vector3(zoneData.position.x, 0, zoneData.position.y);
            GameObject zone = Instantiate(zoneData.instance, worldPos, Quaternion.identity);
            zone.name = zoneData.instance.name + "_Instance";

            // Bake NavMesh for this zone
            NavMeshSurface surface = zone.GetComponent<NavMeshSurface>();
            if (surface != null)
            {
                surface.BuildNavMesh();
                Debug.Log($"NavMesh baked for {zone.name}");
            }
        }

        Debug.Log($"Spawned {placedMissionZones.Count} mission zones");
    }

    private void OptimizeMap()
    {
        Debug.Log("Optimizing map with Mesh Combining...");

        // Combine Land tiles
        CombineTilesByTag("Terrain", "CombinedLandMap");

        // Combine Water walls
        CombineTilesByTag("Wall", "CombinedWaterWalls", false);

        Debug.Log("Map optimization complete!");
    }

    private void CombineTilesByTag(string tag, string combinedName, bool addRenderer = true)
    {
        List<MeshFilter> meshFilters = new List<MeshFilter>();
        List<GameObject> tilesToDestroy = new List<GameObject>();
        Material sharedMaterial = null;

        foreach (Transform child in mapParent)
        {
            if (child.CompareTag(tag))
            {
                MeshFilter mf = child.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    meshFilters.Add(mf);
                    tilesToDestroy.Add(child.gameObject);

                    // Get material from first tile
                    if (sharedMaterial == null && addRenderer)
                    {
                        MeshRenderer mr = child.GetComponent<MeshRenderer>();
                        if (mr != null)
                            sharedMaterial = mr.sharedMaterial;
                    }
                }
            }
        }

        if (meshFilters.Count == 0)
        {
            Debug.LogWarning($"No meshes found with tag {tag}");
            return;
        }

        // Combine meshes
        CombineInstance[] combine = new CombineInstance[meshFilters.Count];

        for (int i = 0; i < meshFilters.Count; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
        }

        // Create combined object
        GameObject combinedObj = new GameObject(combinedName);
        combinedObj.transform.position = Vector3.zero;
        combinedObj.isStatic = true;
        combinedObj.tag = tag;

        MeshFilter combinedMeshFilter = combinedObj.AddComponent<MeshFilter>();
        combinedMeshFilter.mesh = new Mesh();
        combinedMeshFilter.mesh.CombineMeshes(combine, true, true);

        // Add renderer if needed
        if (addRenderer && sharedMaterial != null)
        {
            MeshRenderer combinedRenderer = combinedObj.AddComponent<MeshRenderer>();
            combinedRenderer.material = sharedMaterial;
        }

        // Add Mesh Collider
        MeshCollider combinedCollider = combinedObj.AddComponent<MeshCollider>();
        combinedCollider.sharedMesh = combinedMeshFilter.mesh;

        // Destroy original tiles
        foreach (GameObject tile in tilesToDestroy)
        {
            Destroy(tile);
        }

        Debug.Log($"Combined {meshFilters.Count} tiles into {combinedName}");
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

    // Public methods
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
