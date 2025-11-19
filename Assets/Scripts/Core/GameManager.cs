using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private GameObject vehiclePrefab;
    [SerializeField] private Transform playerTransform;

    [Header("Vehicle Spawn Settings")]
    [SerializeField] private float vehicleSpawnDistanceFromPlayer = 5f;
    [SerializeField] private bool spawnVehicleOnStart = true;

    private GameObject spawnedVehicle;

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

    private void Start()
    {
        InitializeGame();

        if (mapGenerator != null)
        {
            Vector3 spawnPos = mapGenerator.GetPlayerSpawnPosition();
            transform.position = spawnPos;
            Debug.Log($"Player spawned at {spawnPos}");
        }

        // Spawn vehicle near player after a short delay
        if (spawnVehicleOnStart && vehiclePrefab != null)
        {
            Invoke(nameof(SpawnVehicleNearPlayer), 0.5f);
        }
    }

    private void InitializeGame()
    {
        Debug.Log("Game initialized");
    }

    /// <summary>
    /// Spawns a vehicle near the player's current position
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

        // Calculate spawn position near player
        Vector3 offset = playerTransform.right * vehicleSpawnDistanceFromPlayer;
        Vector3 spawnPosition = playerTransform.position + offset;
        spawnPosition.y = playerTransform.position.y; // Keep at same height

        // Spawn vehicle
        spawnedVehicle = Instantiate(vehiclePrefab, spawnPosition, Quaternion.identity);
        spawnedVehicle.name = "PlayerVehicle";

        Debug.Log($"Vehicle spawned at {spawnPosition} (near player)");
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
}
