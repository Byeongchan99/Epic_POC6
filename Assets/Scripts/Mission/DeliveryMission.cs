using UnityEngine;
using System.Collections.Generic;

public class DeliveryMission : MissionBase
{
    [Header("Delivery Mission")]
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private float minDistanceBetweenPoints = 10f; // Minimum distance between pickup and delivery

    [Header("Visual Prefabs")]
    [SerializeField] private GameObject pickupVisualPrefab; // Prefab for pickup point visual
    [SerializeField] private GameObject deliveryVisualPrefab; // Prefab for delivery point visual

    [Header("Minimap Marker Prefabs")]
    [SerializeField] private GameObject pickupMarkerPrefab; // Minimap marker for pickup point (A)
    [SerializeField] private GameObject deliveryMarkerPrefab; // Minimap marker for delivery point (B)

    [Header("Timer")]
    [SerializeField] private float deliveryTimeLimit = 60f; // Time limit in seconds (default: 60 seconds)

    private bool hasPickedUpItem = false;
    private Transform player;
    private Vector3 pickupPosition;
    private Vector3 deliveryPosition;
    private GameObject pickupVisualInstance;
    private GameObject deliveryVisualInstance;

    // Timer
    private bool isTimerRunning = false;
    private float elapsedTime = 0f;

    public override void Initialize()
    {
        base.Initialize();

        Debug.Log("=== DeliveryMission Initialize START ===");

        // Find player
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.transform;
            Debug.Log("Player found");
        }
        else
        {
            Debug.LogWarning("Player NOT found!");
        }

        // Generate random positions for pickup and delivery points within mission zone
        GeneratePointPositions();
        Debug.Log($"Positions generated - Pickup: {pickupPosition}, Delivery: {deliveryPosition}");

        // Instantiate visual objects at those positions
        if (pickupVisualPrefab != null)
        {
            Debug.Log($"Creating pickup visual at {pickupPosition}");
            pickupVisualInstance = Instantiate(pickupVisualPrefab, pickupPosition, Quaternion.identity, transform);
            pickupVisualInstance.name = "PickupPoint";
            Debug.Log($"Pickup visual created: {pickupVisualInstance.name}");
        }
        else
        {
            Debug.LogError("pickupVisualPrefab is NULL! Assign it in Inspector!");
        }

        if (deliveryVisualPrefab != null)
        {
            Debug.Log($"Creating delivery visual at {deliveryPosition}");
            deliveryVisualInstance = Instantiate(deliveryVisualPrefab, deliveryPosition, Quaternion.identity, transform);
            deliveryVisualInstance.name = "DeliveryPoint";
            // Keep visible from the start
            Debug.Log($"Delivery visual created: {deliveryVisualInstance.name}");
        }
        else
        {
            Debug.LogError("deliveryVisualPrefab is NULL! Assign it in Inspector!");
        }

        // Add both pickup and delivery points to minimap with custom markers
        MinimapController minimap = FindAnyObjectByType<MinimapController>();
        if (minimap != null)
        {
            minimap.AddMissionMarker(pickupPosition, pickupMarkerPrefab);
            minimap.AddMissionMarker(deliveryPosition, deliveryMarkerPrefab);
            Debug.Log("Added pickup and delivery markers to minimap with custom prefabs");
        }

        Debug.Log("=== DeliveryMission Initialize END ===");
    }

    private void GeneratePointPositions()
    {
        Debug.Log("--- GeneratePointPositions START ---");

        // Get MapGenerator for entire map bounds
        MapGenerator mapGen = FindAnyObjectByType<MapGenerator>();
        if (mapGen == null)
        {
            Debug.LogError("MapGenerator not found!");
            pickupPosition = new Vector3(10, 0, 10);
            deliveryPosition = new Vector3(20, 0, 20);
            return;
        }

        int mapWidth = mapGen.GetMapWidth();
        int mapHeight = mapGen.GetMapHeight();
        float tileSize = mapGen.GetTileSize();

        Debug.Log($"Map size: {mapWidth}x{mapHeight} tiles, TileSize: {tileSize}");

        // Calculate world-space bounds of entire map
        float worldWidth = mapWidth * tileSize;
        float worldHeight = mapHeight * tileSize;

        // Try to find valid positions on land tiles
        int maxAttempts = 100;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Random position anywhere on the map
            Vector3 randomPickup = new Vector3(
                Random.Range(0, worldWidth),
                0,
                Random.Range(0, worldHeight)
            );

            Vector3 randomDelivery = new Vector3(
                Random.Range(0, worldWidth),
                0,
                Random.Range(0, worldHeight)
            );

            // Check if both positions are on land
            if (!mapGen.IsTileLand(randomPickup) || !mapGen.IsTileLand(randomDelivery))
            {
                continue; // Skip if either is on water
            }

            // Check if distance is sufficient
            float distance = Vector3.Distance(randomPickup, randomDelivery);
            if (distance >= minDistanceBetweenPoints)
            {
                Debug.Log($"Valid positions found on attempt {attempt + 1}, distance: {distance}");

                // Use raycast to find ground level
                pickupPosition = GetGroundPosition(randomPickup);
                deliveryPosition = GetGroundPosition(randomDelivery);

                Debug.Log($"Final positions - Pickup: {pickupPosition}, Delivery: {deliveryPosition}");
                return;
            }
        }

        // Fallback: find any two land tiles
        Debug.LogWarning("Could not find valid random positions with sufficient distance, searching for any land tiles...");

        List<Vector3> landPositions = new List<Vector3>();
        for (int x = 0; x < mapWidth; x += 5) // Sample every 5 tiles for performance
        {
            for (int y = 0; y < mapHeight; y += 5)
            {
                Vector3 worldPos = new Vector3(x * tileSize, 0, y * tileSize);
                if (mapGen.IsTileLand(worldPos))
                {
                    landPositions.Add(worldPos);
                }
            }
        }

        if (landPositions.Count >= 2)
        {
            // Pick two random land positions
            int index1 = Random.Range(0, landPositions.Count);
            int index2 = Random.Range(0, landPositions.Count);
            while (index2 == index1 && landPositions.Count > 1)
            {
                index2 = Random.Range(0, landPositions.Count);
            }

            pickupPosition = GetGroundPosition(landPositions[index1]);
            deliveryPosition = GetGroundPosition(landPositions[index2]);
            Debug.Log($"Fallback positions - Pickup: {pickupPosition}, Delivery: {deliveryPosition}");
        }
        else
        {
            Debug.LogError("Could not find any land tiles for mission positions!");
            pickupPosition = new Vector3(worldWidth / 2, 0, worldHeight / 2);
            deliveryPosition = new Vector3(worldWidth / 2 + 10, 0, worldHeight / 2 + 10);
        }
    }

    private Vector3 GetGroundPosition(Vector3 position)
    {
        RaycastHit hit;
        Vector3 rayStart = new Vector3(position.x, position.y + 10f, position.z);

        // Raycast down to find terrain
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 50f))
        {
            Debug.Log($"Raycast hit: {hit.collider.name}, Tag: {hit.collider.tag}, Point: {hit.point}");
            if (hit.collider.CompareTag("Terrain"))
            {
                Vector3 groundPos = hit.point + Vector3.up * 0.1f;
                Debug.Log($"Found terrain ground at: {groundPos}");
                return groundPos;
            }
            else
            {
                Debug.LogWarning($"Raycast hit non-terrain object: {hit.collider.name} (Tag: {hit.collider.tag})");
            }
        }
        else
        {
            Debug.LogWarning($"Raycast did not hit anything from {rayStart}");
        }

        // Fallback: use original Y position
        Debug.LogWarning($"Using fallback Y position for {position}");
        return new Vector3(position.x, 0.1f, position.z);
    }

    public override void UpdateMission()
    {
        if (player == null)
            return;

        // Update timer if running
        if (isTimerRunning)
        {
            elapsedTime += Time.deltaTime;

            // Calculate remaining time
            float remainingTime = deliveryTimeLimit - elapsedTime;

            // Show timer UI
            if (UIManager.Instance != null)
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60);
                int seconds = Mathf.FloorToInt(remainingTime % 60);
                UIManager.Instance.ShowNotification($"배달 시간: {minutes:00}:{seconds:00}", 0.2f);
            }

            // Check if time ran out
            if (elapsedTime >= deliveryTimeLimit)
            {
                FailMission();
                return;
            }
        }

        if (!hasPickedUpItem)
        {
            // Check if player is near pickup point
            if (IsPlayerNearPosition(pickupPosition))
            {
                // Show prompt
                if (Input.GetKeyDown(KeyCode.F))
                {
                    PickupItem();
                }
            }
        }
        else
        {
            // Check if player is near delivery point
            if (IsPlayerNearPosition(deliveryPosition))
            {
                // Show prompt
                if (Input.GetKeyDown(KeyCode.F))
                {
                    DeliverItem();
                }
            }
        }
    }

    private bool IsPlayerNearPosition(Vector3 position)
    {
        if (player == null)
            return false;

        float distance = Vector3.Distance(player.position, position);
        return distance <= interactionRange;
    }

    private void PickupItem()
    {
        hasPickedUpItem = true;
        Debug.Log("Item picked up! Deliver it to the delivery point.");

        // Start timer
        isTimerRunning = true;
        elapsedTime = 0f;
        Debug.Log($"Timer started! Deliver within {deliveryTimeLimit} seconds.");

        // Visual feedback: hide pickup object (delivery is already visible)
        if (pickupVisualInstance != null)
            pickupVisualInstance.SetActive(false);

        // Both markers are already on minimap from Initialize()
    }

    private void DeliverItem()
    {
        if (hasPickedUpItem)
        {
            // Stop timer
            isTimerRunning = false;

            // Hide notification
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideNotification();
            }

            Debug.Log($"Item delivered in {elapsedTime:F1} seconds!");
            CompleteMission();
        }
    }

    private void FailMission()
    {
        // Stop timer
        isTimerRunning = false;

        Debug.Log("Delivery mission failed! Time ran out.");

        // Show failure notification
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideNotification();
            UIManager.Instance.ShowNotification("배달 실패! 시간 초과", 3f);
        }

        // Reset mission state (optional - depends on game design)
        hasPickedUpItem = false;
        elapsedTime = 0f;

        // Show pickup visual again
        if (pickupVisualInstance != null)
            pickupVisualInstance.SetActive(true);
    }

    private void OnDrawGizmos()
    {
        // Only draw if initialized (positions are set)
        if (Application.isPlaying && pickupPosition != Vector3.zero)
        {
            // Draw pickup point
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pickupPosition, interactionRange);

            // Draw delivery point
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(deliveryPosition, interactionRange);

            // Draw line between them
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pickupPosition, deliveryPosition);
        }
    }
}
