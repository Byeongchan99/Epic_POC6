using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MinimapController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RawImage minimapImage;
    [SerializeField] private RectTransform minimapRect;
    [SerializeField] private Image playerIcon;

    [Header("Colors")]
    [SerializeField] private Color landColor = Color.green;
    [SerializeField] private Color waterColor = Color.blue;
    [SerializeField] private Color missionZoneColor = Color.yellow;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private Texture2D minimapTexture;
    private MapGenerator mapGenerator;
    private Transform playerTransform;
    private PlayerController playerController;
    private int mapWidth;
    private int mapHeight;
    private float tileSize;

    // Mission markers
    private List<GameObject> missionMarkers = new List<GameObject>();
    [SerializeField] private GameObject missionMarkerPrefab;

    public void Initialize(MapGenerator generator, Transform player)
    {
        mapGenerator = generator;
        playerTransform = player;

        if (mapGenerator == null)
        {
            Debug.LogError("MinimapController: MapGenerator is null!");
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogError("MinimapController: Player Transform is null!");
            return;
        }

        // Get PlayerController component for vehicle tracking
        playerController = playerTransform.GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("MinimapController: PlayerController not found on player!");
        }

        mapWidth = mapGenerator.GetMapWidth();
        mapHeight = mapGenerator.GetMapHeight();
        tileSize = mapGenerator.GetTileSize();

        if (enableDebugLogs) Debug.Log($"MinimapController initialized - Map: {mapWidth}x{mapHeight} tiles, TileSize: {tileSize}, WorldSize: {mapWidth * tileSize}x{mapHeight * tileSize}, Player at: {player.position}");

        // Validate UI references
        if (minimapImage == null)
            Debug.LogWarning("MinimapController: minimapImage is not assigned!");
        if (minimapRect == null)
            Debug.LogWarning("MinimapController: minimapRect is not assigned!");
        if (playerIcon == null)
            Debug.LogWarning("MinimapController: playerIcon is not assigned!");

        GenerateMinimapTexture();
    }

    private void GenerateMinimapTexture()
    {
        int[,] mapData = mapGenerator.GetMapData();

        // Create texture
        minimapTexture = new Texture2D(mapWidth, mapHeight);
        minimapTexture.filterMode = FilterMode.Point; // Pixelated look

        // Generate pixels from map data
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Color color = mapData[x, y] == 1 ? landColor : waterColor;
                minimapTexture.SetPixel(x, y, color);
            }
        }

        // Draw mission zones
        DrawMissionZones();

        minimapTexture.Apply();

        // Assign to UI
        if (minimapImage != null)
        {
            minimapImage.texture = minimapTexture;
        }

        if (enableDebugLogs) Debug.Log("Minimap texture generated");
    }

    private void DrawMissionZones()
    {
        // Find all mission zones in the scene
        MissionBase[] missions = FindObjectsByType<MissionBase>(FindObjectsSortMode.None);

        foreach (MissionBase mission in missions)
        {
            MissionZoneInfo zoneInfo = mission.GetComponent<MissionZoneInfo>();
            if (zoneInfo == null) continue;

            Vector3 zoneWorldPos = mission.transform.position;
            Vector2Int zoneSize = zoneInfo.size;

            // Convert world position to tile coordinates
            int centerX = Mathf.RoundToInt(zoneWorldPos.x / tileSize);
            int centerY = Mathf.RoundToInt(zoneWorldPos.z / tileSize);

            // Calculate zone bounds in tile coordinates
            int halfWidth = zoneSize.x / 2;
            int halfHeight = zoneSize.y / 2;

            // Draw zone outline on minimap
            for (int x = centerX - halfWidth; x <= centerX + halfWidth; x++)
            {
                for (int y = centerY - halfHeight; y <= centerY + halfHeight; y++)
                {
                    // Only draw outline (border)
                    bool isBorder = (x == centerX - halfWidth || x == centerX + halfWidth ||
                                    y == centerY - halfHeight || y == centerY + halfHeight);

                    if (isBorder && x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
                    {
                        minimapTexture.SetPixel(x, y, missionZoneColor);
                    }
                }
            }

            if (enableDebugLogs) Debug.Log($"Drew mission zone on minimap at tile ({centerX}, {centerY}) size {zoneSize}");
        }
    }

    private void Update()
    {
        // Debug: Check if Update is being called
        if (playerTransform == null)
        {
            Debug.LogWarning("MinimapController Update: playerTransform is NULL!");
            return;
        }

        if (playerIcon == null)
        {
            Debug.LogWarning("MinimapController Update: playerIcon is NULL!");
            return;
        }

        if (minimapRect == null)
        {
            Debug.LogWarning("MinimapController Update: minimapRect is NULL!");
            return;
        }

        UpdatePlayerIcon();
    }

    private void UpdatePlayerIcon()
    {
        // Determine which transform to track (player or vehicle if in vehicle)
        Transform trackedTransform = playerTransform;

        if (playerController != null && playerController.IsInVehicle())
        {
            // Player is in vehicle, track vehicle position instead
            Vehicle currentVehicle = playerController.GetCurrentVehicle();
            if (currentVehicle != null)
            {
                trackedTransform = currentVehicle.transform;
            }
        }

        // Convert world position to minimap position
        Vector3 worldPos = trackedTransform.position;
        Vector2 minimapPos = WorldToMinimapPosition(worldPos);

        // Debug log every 60 frames (about once per second)
        if (Time.frameCount % 60 == 0)
        {
            if (enableDebugLogs) Debug.Log($"Minimap Update: World({worldPos.x:F1}, {worldPos.z:F1}) -> Minimap({minimapPos.x:F1}, {minimapPos.y:F1})");
        }

        // Update player icon position
        if (playerIcon != null && playerIcon.rectTransform != null)
        {
            playerIcon.rectTransform.anchoredPosition = minimapPos;

            // Rotate player icon to match rotation (Y rotation in world space)
            float angle = -trackedTransform.eulerAngles.y;
            playerIcon.rectTransform.rotation = Quaternion.Euler(0, 0, angle);
        }
        else
        {
            Debug.LogWarning("MinimapController: playerIcon or its RectTransform is null!");
        }
    }

    private Vector2 WorldToMinimapPosition(Vector3 worldPos)
    {
        if (mapWidth <= 0 || mapHeight <= 0)
        {
            Debug.LogWarning($"MinimapController: Invalid map dimensions ({mapWidth}x{mapHeight})");
            return Vector2.zero;
        }

        if (minimapRect == null)
        {
            Debug.LogWarning("MinimapController: minimapRect is null in WorldToMinimapPosition");
            return Vector2.zero;
        }

        // Calculate world size (tiles * tileSize)
        float worldWidth = mapWidth * tileSize;
        float worldHeight = mapHeight * tileSize;

        // Normalize world position to 0-1 range
        float normalizedX = worldPos.x / worldWidth;
        float normalizedY = worldPos.z / worldHeight;

        // Clamp to valid range
        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedY = Mathf.Clamp01(normalizedY);

        // Get minimap dimensions - use sizeDelta as it's more reliable than rect
        // rect.width/height can be 0 if layout hasn't been calculated
        float minimapWidth = minimapRect.rect.width;
        float minimapHeight = minimapRect.rect.height;

        // Fallback to sizeDelta if rect dimensions are invalid
        if (minimapWidth <= 0 || minimapHeight <= 0)
        {
            minimapWidth = minimapRect.sizeDelta.x;
            minimapHeight = minimapRect.sizeDelta.y;
            Debug.LogWarning($"MinimapRect.rect has invalid dimensions! Using sizeDelta instead: ({minimapWidth:F1} x {minimapHeight:F1})");
        }

        // Final check - if still invalid, return center
        if (minimapWidth <= 0 || minimapHeight <= 0)
        {
            Debug.LogError($"MinimapRect has no valid dimensions! rect: ({minimapRect.rect.width}, {minimapRect.rect.height}), sizeDelta: ({minimapRect.sizeDelta.x}, {minimapRect.sizeDelta.y})");
            return Vector2.zero;
        }

        // Debug: Log the calculation process every 60 frames
        if (Time.frameCount % 60 == 0)
        {
            //if (enableDebugLogs) Debug.Log($"[Minimap Calc] World({worldPos.x:F1}, {worldPos.z:F1}) / WorldSize({worldWidth:F0}x{worldHeight:F0}) = Norm({normalizedX:F3}, {normalizedY:F3})");
            //if (enableDebugLogs) Debug.Log($"[Minimap Calc] MinimapRect size: ({minimapWidth:F1} x {minimapHeight:F1})");
        }

        // Convert normalized position to minimap UI coordinates
        // Anchored position is relative to anchor point (typically center)
        Vector2 minimapPos = new Vector2(
            normalizedX * minimapWidth - minimapWidth / 2,
            normalizedY * minimapHeight - minimapHeight / 2
        );

        return minimapPos;
    }

    public void AddMissionMarker(Vector3 missionWorldPos)
    {
        AddMissionMarker(missionWorldPos, missionMarkerPrefab);
    }

    public void AddMissionMarker(Vector3 missionWorldPos, GameObject customMarkerPrefab)
    {
        GameObject prefabToUse = customMarkerPrefab != null ? customMarkerPrefab : missionMarkerPrefab;

        if (prefabToUse == null)
        {
            Debug.LogWarning("Mission marker prefab not assigned");
            return;
        }

        GameObject marker = Instantiate(prefabToUse, minimapRect);
        Vector2 minimapPos = WorldToMinimapPosition(missionWorldPos);
        marker.GetComponent<RectTransform>().anchoredPosition = minimapPos;
        missionMarkers.Add(marker);
    }

    public void RemoveMissionMarker(int index)
    {
        if (index >= 0 && index < missionMarkers.Count)
        {
            Destroy(missionMarkers[index]);
            missionMarkers.RemoveAt(index);
        }
    }

    public void ClearMissionMarkers()
    {
        foreach (GameObject marker in missionMarkers)
        {
            Destroy(marker);
        }
        missionMarkers.Clear();
    }

    public Texture2D GetMinimapTexture()
    {
        return minimapTexture;
    }

    public void RefreshMissionZones()
    {
        // Regenerate the minimap texture to include mission zones
        GenerateMinimapTexture();
    }
}
