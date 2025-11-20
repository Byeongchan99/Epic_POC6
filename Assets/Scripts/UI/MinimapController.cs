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

    private Texture2D minimapTexture;
    private MapGenerator mapGenerator;
    private Transform playerTransform;
    private PlayerController playerController;
    private int mapWidth;
    private int mapHeight;

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

        Debug.Log($"MinimapController initialized with map size: {mapWidth}x{mapHeight}, player at: {player.position}");

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

        minimapTexture.Apply();

        // Assign to UI
        if (minimapImage != null)
        {
            minimapImage.texture = minimapTexture;
        }

        Debug.Log("Minimap texture generated");
    }

    private void Update()
    {
        if (playerTransform != null && playerIcon != null && minimapRect != null)
        {
            UpdatePlayerIcon();
        }
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
        Vector2 minimapPos = WorldToMinimapPosition(trackedTransform.position);

        // Update player icon position
        if (playerIcon != null && playerIcon.rectTransform != null)
        {
            playerIcon.rectTransform.anchoredPosition = minimapPos;

            // Rotate player icon to match rotation (Y rotation in world space)
            float angle = -trackedTransform.eulerAngles.y;
            playerIcon.rectTransform.rotation = Quaternion.Euler(0, 0, angle);
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

        // Normalize world position to 0-1 range
        float normalizedX = worldPos.x / mapWidth;
        float normalizedY = worldPos.z / mapHeight;

        // Clamp to valid range
        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedY = Mathf.Clamp01(normalizedY);

        // Convert to minimap UI coordinates (anchored position)
        float minimapWidth = minimapRect.rect.width;
        float minimapHeight = minimapRect.rect.height;

        Vector2 minimapPos = new Vector2(
            normalizedX * minimapWidth - minimapWidth / 2,
            normalizedY * minimapHeight - minimapHeight / 2
        );

        return minimapPos;
    }

    public void AddMissionMarker(Vector3 missionWorldPos)
    {
        if (missionMarkerPrefab == null)
        {
            Debug.LogWarning("Mission marker prefab not assigned");
            return;
        }

        GameObject marker = Instantiate(missionMarkerPrefab, minimapRect);
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
}
