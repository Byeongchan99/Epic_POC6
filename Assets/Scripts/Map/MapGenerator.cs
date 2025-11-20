using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;
using System;

public class MapGenerator : MonoBehaviour
{
    // Event fired when map generation is complete
    public static event Action OnMapGenerationComplete;

    [Header("Map Settings")]
    [SerializeField] private int mapWidth = 100;
    [SerializeField] private int mapHeight = 100;
    [SerializeField] private float tileSize = 1f; // Size of each tile (match your prefab scale)
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
    [SerializeField] private PhysicsMaterial groundPhysicsMaterial; // Assign Friction.physicMaterial from Arcade Vehicle Physics

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

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
            seed = UnityEngine.Random.Range(0, 10000);

        UnityEngine.Random.InitState(seed);

        // Generate map data using Multi-Layer Perlin Noise
        mapData = new int[mapWidth, mapHeight];
        GenerateMapData();

        // Ensure all land is connected (remove isolated islands)
        EnsureConnectedLand();

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

        if (enableDebugLogs) Debug.Log($"Map generated with seed: {seed}");
        if (enableDebugLogs) Debug.Log($"Total land tiles: {landTiles.Count}");
        if (enableDebugLogs) Debug.Log($"Mission zones placed: {placedMissionZones.Count}");

        // Notify all listeners that map generation is complete
        OnMapGenerationComplete?.Invoke();
        if (enableDebugLogs) Debug.Log("Map generation complete event fired");
    }

    private void GenerateMapData()
    {
        float offsetX = UnityEngine.Random.Range(0f, 10000f);
        float offsetY = UnityEngine.Random.Range(0f, 10000f);

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

                if (enableDebugLogs) Debug.Log($"Mission zone placement reserved at {position} with size {zoneSize}");
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
            int x = UnityEngine.Random.Range(size.x / 2, mapWidth - size.x / 2);
            int y = UnityEngine.Random.Range(size.y / 2, mapHeight - size.y / 2);

            Vector2Int candidate = new Vector2Int(x, y);

            // Check if the entire zone area is on land (accessible)
            if (!IsZoneAreaOnLand(candidate, size))
            {
                continue; // Skip if zone would be on water or isolated
            }

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

    private bool IsZoneAreaOnLand(Vector2Int center, Vector2Int size)
    {
        int halfWidth = size.x / 2;
        int halfHeight = size.y / 2;

        // Check center and surrounding area to ensure it's connected to main land mass
        int landTileCount = 0;
        int totalTiles = 0;

        // Check the zone area and a buffer around it
        int buffer = 3; // Check 3 tiles around the zone
        for (int x = center.x - halfWidth - buffer; x <= center.x + halfWidth + buffer; x++)
        {
            for (int y = center.y - halfHeight - buffer; y <= center.y + halfHeight + buffer; y++)
            {
                if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
                {
                    totalTiles++;
                    if (mapData[x, y] == (int)TileType.Land)
                    {
                        landTileCount++;
                    }
                }
            }
        }

        // Require at least 70% of the area (including buffer) to be land
        // This ensures the zone is on a large land mass, not an isolated island
        float landRatio = (float)landTileCount / totalTiles;
        bool isOnLand = landRatio >= 0.7f;

        if (enableDebugLogs && !isOnLand)
        {
            Debug.Log($"Zone position {center} rejected - only {landRatio:P0} land (need 70%+)");
        }

        return isOnLand;
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
                Vector3 position = new Vector3(x * tileSize, 0, y * tileSize);

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

                        // Reset scale to ensure consistent sizing
                        waterTile.transform.localScale = Vector3.one;

                        // Remove ALL mesh components - we only need BoxCollider
                        MeshFilter meshFilter = waterTile.GetComponent<MeshFilter>();
                        if (meshFilter != null)
                            DestroyImmediate(meshFilter);

                        MeshRenderer renderer = waterTile.GetComponent<MeshRenderer>();
                        if (renderer != null)
                            DestroyImmediate(renderer);

                        MeshCollider meshCollider = waterTile.GetComponent<MeshCollider>();
                        if (meshCollider != null)
                            DestroyImmediate(meshCollider);

                        // Add BoxCollider to make it a 5m tall wall
                        BoxCollider collider = waterTile.GetComponent<BoxCollider>();
                        if (collider == null)
                            collider = waterTile.AddComponent<BoxCollider>();

                        collider.center = new Vector3(0, waterWallHeight / 2, 0);
                        collider.size = new Vector3(tileSize, waterWallHeight, tileSize);
                    }
                }
            }
        }

        if (enableDebugLogs) Debug.Log("Tiles spawned");

        // Instantiate mission zone prefabs after tiles
        SpawnMissionZonePrefabs();
    }

    private void SpawnMissionZonePrefabs()
    {
        foreach (var zoneData in placedMissionZones)
        {
            Vector3 worldPos = new Vector3(zoneData.position.x * tileSize, 0, zoneData.position.y * tileSize);
            GameObject zone = Instantiate(zoneData.instance, worldPos, Quaternion.identity);
            zone.name = zoneData.instance.name + "_Instance";

            // Bake NavMesh for this zone
            NavMeshSurface surface = zone.GetComponent<NavMeshSurface>();
            if (surface != null)
            {
                surface.BuildNavMesh();
                if (enableDebugLogs) Debug.Log($"NavMesh baked for {zone.name}");
            }
        }

        if (enableDebugLogs) Debug.Log($"Spawned {placedMissionZones.Count} mission zones");
    }

    private void OptimizeMap()
    {
        if (enableDebugLogs) Debug.Log("Optimizing map with Mesh Combining...");

        // Combine Land tiles
        CombineTilesByTag("Terrain", "CombinedLandMap");

        // Combine Water wall BoxColliders into one MeshCollider
        CombineWaterWallColliders();

        if (enableDebugLogs) Debug.Log("Map optimization complete!");
    }

    /// <summary>
    /// Optimizes water wall BoxColliders by grouping adjacent tiles into larger colliders
    /// CharacterController works much better with BoxColliders than complex MeshColliders
    /// </summary>
    private void CombineWaterWallColliders()
    {
        if (enableDebugLogs) Debug.Log("Optimizing Water wall colliders...");

        // Build a 2D grid of water tile positions
        bool[,] waterGrid = new bool[mapWidth, mapHeight];
        bool[,] processed = new bool[mapWidth, mapHeight];

        // Mark all water tiles
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                waterGrid[x, y] = (mapData[x, y] == (int)TileType.Water);
            }
        }

        // Destroy all individual water tile GameObjects
        List<GameObject> waterTilesToDestroy = new List<GameObject>();
        foreach (Transform child in mapParent)
        {
            if (child.CompareTag("Wall"))
            {
                waterTilesToDestroy.Add(child.gameObject);
            }
        }

        int originalCount = waterTilesToDestroy.Count;
        foreach (GameObject tile in waterTilesToDestroy)
        {
            Destroy(tile);
        }

        if (enableDebugLogs) Debug.Log($"Destroyed {originalCount} individual water tiles");

        // Create parent for optimized colliders as child of mapParent
        GameObject waterWallsParent = new GameObject("CombinedWaterWalls");
        waterWallsParent.transform.SetParent(mapParent);
        waterWallsParent.transform.position = Vector3.zero;
        waterWallsParent.isStatic = true;
        waterWallsParent.tag = "Wall";
        waterWallsParent.layer = LayerMask.NameToLayer("Default"); // Ensure it's on a collidable layer

        int colliderCount = 0;

        // Group water tiles into rectangular regions
        for (int startX = 0; startX < mapWidth; startX++)
        {
            for (int startY = 0; startY < mapHeight; startY++)
            {
                if (!waterGrid[startX, startY] || processed[startX, startY])
                    continue;

                // Find the largest rectangle starting from (startX, startY)
                int width = 1;
                int height = 1;

                // Expand horizontally
                while (startX + width < mapWidth &&
                       waterGrid[startX + width, startY] &&
                       !processed[startX + width, startY])
                {
                    width++;
                }

                // Try to expand vertically
                bool canExpandVertically = true;
                while (startY + height < mapHeight && canExpandVertically)
                {
                    // Check if the entire row can be added
                    for (int x = startX; x < startX + width; x++)
                    {
                        if (!waterGrid[x, startY + height] || processed[x, startY + height])
                        {
                            canExpandVertically = false;
                            break;
                        }
                    }

                    if (canExpandVertically)
                        height++;
                }

                // Mark all tiles in this rectangle as processed
                for (int x = startX; x < startX + width; x++)
                {
                    for (int y = startY; y < startY + height; y++)
                    {
                        processed[x, y] = true;
                    }
                }

                // Create a single BoxCollider for this rectangular region
                GameObject colliderObj = new GameObject($"WaterWall_Region_{colliderCount}");
                colliderObj.transform.parent = waterWallsParent.transform;
                colliderObj.tag = "Wall";
                colliderObj.isStatic = true;
                colliderObj.layer = LayerMask.NameToLayer("Default");

                // Calculate center position
                float centerX = (startX + (width - 1) * 0.5f) * tileSize;
                float centerZ = (startY + (height - 1) * 0.5f) * tileSize;
                float centerY = waterWallHeight / 2f;

                colliderObj.transform.position = new Vector3(centerX, centerY, centerZ);

                // Add BoxCollider
                BoxCollider boxCollider = colliderObj.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(width * tileSize, waterWallHeight, height * tileSize);
                boxCollider.center = Vector3.zero;

                colliderCount++;
            }
        }

        if (enableDebugLogs) Debug.Log($"✓ Optimized {originalCount} water tiles into {colliderCount} BoxColliders (reduction: {((1f - colliderCount / (float)originalCount) * 100f):F1}%)");
        if (enableDebugLogs) Debug.Log($"Water wall collision is now CharacterController-compatible with reliable physics");
    }

    private void CombineTilesByTag(string tag, string combinedName, bool addRenderer = true)
    {
        List<MeshFilter> meshFilters = new List<MeshFilter>();
        List<GameObject> tilesToDestroy = new List<GameObject>();
        Material sharedMaterial = null;
        int skippedCount = 0;

        foreach (Transform child in mapParent)
        {
            if (child.CompareTag(tag))
            {
                MeshFilter mf = child.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    // Check if mesh is readable (can be combined)
                    if (!mf.sharedMesh.isReadable)
                    {
                        Debug.LogWarning($"Mesh '{mf.sharedMesh.name}' is not readable. Skipping tile '{child.name}'. Enable Read/Write in mesh import settings.");
                        skippedCount++;
                        continue;
                    }

                    meshFilters.Add(mf);
                    tilesToDestroy.Add(child.gameObject);

                    // Get material from first tile
                    if (sharedMaterial == null && addRenderer)
                    {
                        MeshRenderer mr = child.GetComponent<MeshRenderer>();
                        if (mr != null && mr.sharedMaterial != null)
                        {
                            sharedMaterial = mr.sharedMaterial;
                            if (enableDebugLogs) Debug.Log($"Using material: {sharedMaterial.name} from {child.name}");
                        }
                    }
                }
            }
        }

        if (enableDebugLogs) Debug.Log($"CombineTilesByTag [{tag}]: Found {meshFilters.Count} readable meshes, skipped {skippedCount} non-readable meshes");

        if (meshFilters.Count == 0)
        {
            Debug.LogWarning($"No combinable meshes found with tag {tag}. All meshes may be non-readable.");
            return;
        }

        // Combine meshes
        CombineInstance[] combine = new CombineInstance[meshFilters.Count];

        for (int i = 0; i < meshFilters.Count; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
        }

        // Create combined object as child of mapParent
        GameObject combinedObj = new GameObject(combinedName);
        combinedObj.transform.SetParent(mapParent);
        combinedObj.transform.position = Vector3.zero;
        combinedObj.isStatic = true;
        combinedObj.tag = tag;

        MeshFilter combinedMeshFilter = combinedObj.AddComponent<MeshFilter>();
        combinedMeshFilter.mesh = new Mesh();

        // Use 32-bit index format to support more than 65535 vertices
        combinedMeshFilter.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        if (enableDebugLogs) Debug.Log($"Set {combinedName} to use UInt32 index format (supports up to ~4 billion vertices)");

        try
        {
            combinedMeshFilter.mesh.CombineMeshes(combine, true, true);
            if (enableDebugLogs) Debug.Log($"Mesh combining successful! Total vertices: {combinedMeshFilter.mesh.vertexCount}");

            // OPTIMIZATION: Remove duplicate vertices and optimize mesh
            combinedMeshFilter.mesh.Optimize();
            if (enableDebugLogs) Debug.Log($"Mesh optimized. Vertices after optimization: {combinedMeshFilter.mesh.vertexCount}");

            // Recalculate normals for smooth surface
            combinedMeshFilter.mesh.RecalculateNormals();
            combinedMeshFilter.mesh.RecalculateTangents();
            if (enableDebugLogs) Debug.Log("Normals and tangents recalculated for smooth surface");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to combine meshes for {combinedName}: {e.Message}");
            Destroy(combinedObj);
            return;
        }

        // Add renderer if needed
        if (addRenderer)
        {
            MeshRenderer combinedRenderer = combinedObj.AddComponent<MeshRenderer>();

            if (sharedMaterial != null)
            {
                combinedRenderer.material = sharedMaterial;
                if (enableDebugLogs) Debug.Log($"Applied material '{sharedMaterial.name}' to {combinedName}");
            }
            else
            {
                Debug.LogWarning($"No material found for {combinedName}. Using default material. Make sure your tile prefab has a Material assigned!");
                // Unity will use default pink material
            }
        }
        else
        {
            if (enableDebugLogs) Debug.Log($"Skipping renderer for {combinedName} (invisible collider only)");
        }

        // Add Mesh Collider with optimized settings
        MeshCollider combinedCollider = combinedObj.AddComponent<MeshCollider>();
        combinedCollider.sharedMesh = combinedMeshFilter.mesh;
        combinedCollider.convex = false; // Non-convex for accurate terrain collision

        // Set cooking options for better accuracy
        combinedCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation |
                                          MeshColliderCookingOptions.EnableMeshCleaning |
                                          MeshColliderCookingOptions.WeldColocatedVertices;

        // Apply Physics Material for proper vehicle physics (matches Arcade Vehicle Physics asset)
        if (groundPhysicsMaterial != null)
        {
            combinedCollider.material = groundPhysicsMaterial;
            if (enableDebugLogs) Debug.Log($"Applied '{groundPhysicsMaterial.name}' Physics Material to {combinedName}");
        }
        else
        {
            Debug.LogWarning($"Ground Physics Material not assigned in MapGenerator! Assign Friction.physicMaterial for proper vehicle physics.");
        }

        if (enableDebugLogs) Debug.Log($"MeshCollider created with WeldColocatedVertices for smooth surface");

        // Destroy original tiles
        foreach (GameObject tile in tilesToDestroy)
        {
            Destroy(tile);
        }

        if (enableDebugLogs) Debug.Log($"✓ Successfully combined {meshFilters.Count} tiles into {combinedName}");
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

    /// <summary>
    /// Ensures all land tiles are connected by removing isolated islands
    /// Keeps only the largest connected land mass
    /// </summary>
    private void EnsureConnectedLand()
    {
        if (enableDebugLogs) Debug.Log("Ensuring land connectivity...");

        bool[,] visited = new bool[mapWidth, mapHeight];
        List<List<Vector2Int>> landMasses = new List<List<Vector2Int>>();

        // Find all separate land masses using flood fill
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (mapData[x, y] == (int)TileType.Land && !visited[x, y])
                {
                    List<Vector2Int> landMass = FloodFill(x, y, visited);
                    landMasses.Add(landMass);
                }
            }
        }

        if (landMasses.Count == 0)
        {
            Debug.LogError("No land found! Generating fallback land area.");
            CreateFallbackLand();
            return;
        }

        // Find the largest land mass
        List<Vector2Int> largestLandMass = landMasses[0];
        foreach (var landMass in landMasses)
        {
            if (landMass.Count > largestLandMass.Count)
            {
                largestLandMass = landMass;
            }
        }

        if (enableDebugLogs) Debug.Log($"Found {landMasses.Count} separate land masses. Largest has {largestLandMass.Count} tiles.");

        // Convert all tiles to water first
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                mapData[x, y] = (int)TileType.Water;
            }
        }

        // Restore only the largest land mass
        foreach (var tile in largestLandMass)
        {
            mapData[tile.x, tile.y] = (int)TileType.Land;
        }

        if (enableDebugLogs) Debug.Log($"Kept largest land mass with {largestLandMass.Count} tiles. Removed {landMasses.Count - 1} isolated islands.");
    }

    /// <summary>
    /// Flood fill algorithm to find all connected land tiles
    /// </summary>
    private List<Vector2Int> FloodFill(int startX, int startY, bool[,] visited)
    {
        List<Vector2Int> landMass = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(new Vector2Int(startX, startY));
        visited[startX, startY] = true;

        // 4-directional neighbors
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            landMass.Add(current);

            // Check all 4 neighbors
            for (int i = 0; i < 4; i++)
            {
                int nx = current.x + dx[i];
                int ny = current.y + dy[i];

                // Check bounds
                if (nx >= 0 && nx < mapWidth && ny >= 0 && ny < mapHeight)
                {
                    // Check if land and not visited
                    if (mapData[nx, ny] == (int)TileType.Land && !visited[nx, ny])
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue(new Vector2Int(nx, ny));
                    }
                }
            }
        }

        return landMass;
    }

    /// <summary>
    /// Creates a fallback land area in the center if no land was generated
    /// </summary>
    private void CreateFallbackLand()
    {
        int centerX = mapWidth / 2;
        int centerY = mapHeight / 2;
        int radius = Mathf.Min(mapWidth, mapHeight) / 4;

        for (int x = centerX - radius; x < centerX + radius; x++)
        {
            for (int y = centerY - radius; y < centerY + radius; y++)
            {
                if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    if (distance < radius)
                    {
                        mapData[x, y] = (int)TileType.Land;
                    }
                }
            }
        }

        if (enableDebugLogs) Debug.Log($"Created fallback land area with radius {radius} at center ({centerX}, {centerY})");
    }

    /// <summary>
    /// Finds a valid player spawn position near the center of the map
    /// Guarantees position is on land
    /// </summary>
    private Vector3 FindPlayerSpawnPosition()
    {
        int centerX = mapWidth / 2;
        int centerY = mapHeight / 2;

        // Try to find land near center in expanding circles
        for (int radius = 0; radius < Mathf.Max(mapWidth, mapHeight) / 2; radius++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    // Only check tiles on the current radius circle
                    if (Mathf.Abs(x - centerX) == radius || Mathf.Abs(y - centerY) == radius)
                    {
                        if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
                        {
                            if (mapData[x, y] == (int)TileType.Land)
                            {
                                Vector3 spawnPos = new Vector3(x * tileSize, 1f, y * tileSize);
                                if (enableDebugLogs) Debug.Log($"Player spawn position found at ({x}, {y}) world position {spawnPos}");
                                return spawnPos;
                            }
                        }
                    }
                }
            }
        }

        // Fallback: just find any land tile
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (mapData[x, y] == (int)TileType.Land)
                {
                    Vector3 spawnPos = new Vector3(x * tileSize, 1f, y * tileSize);
                    Debug.LogWarning($"Using fallback spawn position at ({x}, {y})");
                    return spawnPos;
                }
            }
        }

        Debug.LogError("No land tiles found for player spawn!");
        return new Vector3(centerX * tileSize, 1f, centerY * tileSize);
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
        // Convert world position to tile coordinates
        int x = Mathf.RoundToInt(worldPosition.x / tileSize);
        int y = Mathf.RoundToInt(worldPosition.z / tileSize);
        return IsTileLand(x, y);
    }

    public Vector3 GetRandomLandPosition()
    {
        if (landTiles.Count == 0)
        {
            Debug.LogError("No land tiles available!");
            return Vector3.zero;
        }

        Vector2Int randomTile = landTiles[UnityEngine.Random.Range(0, landTiles.Count)];
        return new Vector3(randomTile.x * tileSize, 0, randomTile.y * tileSize);
    }

    /// <summary>
    /// Returns a guaranteed valid player spawn position on land near map center
    /// </summary>
    public Vector3 GetPlayerSpawnPosition()
    {
        return FindPlayerSpawnPosition();
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

    public float GetTileSize()
    {
        return tileSize;
    }
}
