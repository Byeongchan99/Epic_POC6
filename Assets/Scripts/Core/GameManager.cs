using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private GameObject vehiclePrefab;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private UIManager uiManager;

    [Header("Vehicle Spawn Settings")]
    [SerializeField] private float vehicleSpawnDistanceFromPlayer = 5f;
    [SerializeField] private bool spawnVehicleOnStart = true;

    private GameObject spawnedVehicle;
    private bool uiInitialized = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Subscribe to map generation complete event
        MapGenerator.OnMapGenerationComplete += OnMapReady;
    }

    private void OnDisable()
    {
        // Unsubscribe from event
        MapGenerator.OnMapGenerationComplete -= OnMapReady;
    }

    private void Start()
    {
        InitializeGame();
    }

    private void OnMapReady()
    {
        // Called when map generation is complete
        Debug.Log("GameManager: Map generation complete, ready to spawn vehicle");

        // Initialize Minimap
        InitializeMinimap();

        // Initialize UI (player stats)
        InitializeUI();

        // Spawn vehicle near player after map is ready
        if (spawnVehicleOnStart && vehiclePrefab != null)
        {
            // Wait a frame to ensure player has moved to spawn position
            Invoke(nameof(SpawnVehicleNearPlayer), 0.1f);
        }
    }

    private void InitializeGame()
    {
        Debug.Log("Game initialized");
    }

    /// <summary>
    /// Spawns a vehicle near the player's current position on valid land
    /// </summary>
    public void SpawnVehicleNearPlayer()
    {
        if (vehiclePrefab == null)
        {
            Debug.LogError("Vehicle prefab not assigned to GameManager!");
            return;
        }

        // Find player if not assigned
        if (playerTransform == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("Player not found! Cannot spawn vehicle.");
                return;
            }
        }

        // Find valid spawn position near player using MapGenerator
        Vector3 spawnPosition = FindValidVehicleSpawnPosition(playerTransform.position);

        // Spawn vehicle
        spawnedVehicle = Instantiate(vehiclePrefab, spawnPosition, Quaternion.identity);
        spawnedVehicle.name = "PlayerVehicle";

        Debug.Log($"Vehicle spawned at {spawnPosition} (on valid land near player)");
    }

    /// <summary>
    /// Finds a valid land position near the target position for vehicle spawning
    /// </summary>
    private Vector3 FindValidVehicleSpawnPosition(Vector3 nearPosition)
    {
        if (mapGenerator == null)
        {
            Debug.LogWarning("MapGenerator not assigned! Using simple offset spawn.");
            return nearPosition + Vector3.right * vehicleSpawnDistanceFromPlayer;
        }

        float searchRadius = vehicleSpawnDistanceFromPlayer;
        int maxAttempts = 16; // Check in 16 directions around player

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Calculate angle for this attempt
            float angle = (attempt / (float)maxAttempts) * 360f * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * searchRadius;
            Vector3 testPosition = nearPosition + offset;

            // Check if this position is on land
            if (mapGenerator.IsTileLand(testPosition))
            {
                // Use Raycast to find exact ground height
                testPosition = GetGroundPosition(testPosition);
                Debug.Log($"Found valid vehicle spawn position at angle {angle * Mathf.Rad2Deg:F0}° from player at height Y={testPosition.y:F2}");
                return testPosition;
            }
        }

        // If no valid position found at vehicleSpawnDistanceFromPlayer, expand search
        Debug.LogWarning($"No valid position found at {searchRadius}m, expanding search...");

        for (float radius = searchRadius + 2f; radius <= searchRadius * 2f; radius += 2f)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                float angle = (attempt / (float)maxAttempts) * 360f * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Vector3 testPosition = nearPosition + offset;

                if (mapGenerator.IsTileLand(testPosition))
                {
                    testPosition = GetGroundPosition(testPosition);
                    Debug.Log($"Found valid vehicle spawn position at {radius}m from player at height Y={testPosition.y:F2}");
                    return testPosition;
                }
            }
        }

        // Fallback: use player position (should always be valid)
        Debug.LogWarning("Could not find valid vehicle spawn position, spawning at player location");
        return GetGroundPosition(nearPosition);
    }

    /// <summary>
    /// Uses Raycast to find the exact ground position at given XZ coordinates
    /// </summary>
    private Vector3 GetGroundPosition(Vector3 position)
    {
        // Start raycast from high above the position
        Vector3 rayStart = new Vector3(position.x, 100f, position.z);
        RaycastHit hit;

        // Cast downward to find ground
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 200f))
        {
            // Return position slightly above ground (to prevent clipping)
            Vector3 groundPos = hit.point;
            groundPos.y += 0.5f; // Add small offset so vehicle sits properly on ground
            Debug.Log($"Ground found at Y={hit.point.y:F2}, vehicle spawn at Y={groundPos.y:F2}");
            return groundPos;
        }

        // Fallback if raycast fails
        Debug.LogWarning($"Raycast failed to find ground at ({position.x:F1}, {position.z:F1}), using default height");
        position.y = 1f;
        return position;
    }

    /// <summary>
    /// Spawns a vehicle at a specific position
    /// </summary>
    public GameObject SpawnVehicleAt(Vector3 position)
    {
        if (vehiclePrefab == null)
        {
            Debug.LogError("Vehicle prefab not assigned!");
            return null;
        }

        GameObject vehicle = Instantiate(vehiclePrefab, position, Quaternion.identity);
        Debug.Log($"Vehicle spawned at {position}");
        return vehicle;
    }

    public MapGenerator GetMapGenerator()
    {
        return mapGenerator;
    }

    public GameObject GetSpawnedVehicle()
    {
        return spawnedVehicle;
    }

    /// <summary>
    /// Initialize minimap after map generation is complete
    /// </summary>
    private void InitializeMinimap()
    {
        if (uiManager == null)
        {
            uiManager = FindAnyObjectByType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogWarning("UIManager not found! Minimap will not be initialized.");
                return;
            }
        }

        MinimapController minimapController = uiManager.GetMinimapController();
        if (minimapController != null && mapGenerator != null)
        {
            // Find player if not assigned
            if (playerTransform == null)
            {
                PlayerController player = FindAnyObjectByType<PlayerController>();
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }

            minimapController.Initialize(mapGenerator, playerTransform);
            Debug.Log("Minimap initialized successfully");
        }
        else
        {
            Debug.LogWarning("MinimapController or MapGenerator not found!");
        }
    }

    /// <summary>
    /// Initialize UI with player stats
    /// </summary>
    private void InitializeUI()
    {
        if (uiInitialized)
            return;

        if (uiManager == null)
        {
            uiManager = FindAnyObjectByType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogWarning("UIManager not found!");
                return;
            }
        }

        // Find player components
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("PlayerController not found! UI will not be initialized.");
            return;
        }

        PlayerStats playerStats = playerController.GetComponent<PlayerStats>();
        Gun playerGun = playerController.GetComponent<Gun>();

        if (playerStats == null)
        {
            Debug.LogWarning("PlayerStats not found on Player!");
        }

        if (playerGun == null)
        {
            Debug.LogWarning("Gun not found on Player!");
        }

        // Initialize UIManager
        uiManager.Initialize(playerStats, playerController, playerGun);
        uiInitialized = true;

        Debug.Log("UI initialized successfully");
    }
}
