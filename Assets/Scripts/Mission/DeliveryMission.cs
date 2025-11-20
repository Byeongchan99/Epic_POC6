using UnityEngine;

public class DeliveryMission : MissionBase
{
    [Header("Delivery Mission")]
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private float minDistanceBetweenPoints = 10f; // Minimum distance between pickup and delivery

    [Header("Visual Prefabs")]
    [SerializeField] private GameObject pickupVisualPrefab; // Prefab for pickup point visual
    [SerializeField] private GameObject deliveryVisualPrefab; // Prefab for delivery point visual

    private bool hasPickedUpItem = false;
    private Transform player;
    private Vector3 pickupPosition;
    private Vector3 deliveryPosition;
    private GameObject pickupVisualInstance;
    private GameObject deliveryVisualInstance;

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
            deliveryVisualInstance.SetActive(false); // Hide until item is picked up
            Debug.Log($"Delivery visual created: {deliveryVisualInstance.name} (hidden)");
        }
        else
        {
            Debug.LogError("deliveryVisualPrefab is NULL! Assign it in Inspector!");
        }

        Debug.Log("=== DeliveryMission Initialize END ===");
    }

    private void GeneratePointPositions()
    {
        Debug.Log("--- GeneratePointPositions START ---");

        // Get mission zone size from MissionZoneInfo
        MissionZoneInfo zoneInfo = GetComponent<MissionZoneInfo>();
        if (zoneInfo == null)
        {
            Debug.LogError("MissionZoneInfo not found on DeliveryMission!");
            // Fallback to default positions
            pickupPosition = transform.position + new Vector3(-5, 0, -5);
            deliveryPosition = transform.position + new Vector3(5, 0, 5);
            Debug.LogWarning($"Using fallback positions - Pickup: {pickupPosition}, Delivery: {deliveryPosition}");
            return;
        }

        Vector2Int zoneSize = zoneInfo.size;
        Vector3 zoneCenter = transform.position;
        Debug.Log($"Zone size: {zoneSize}, Center: {zoneCenter}");

        // Get MapGenerator for tileSize
        MapGenerator mapGen = FindAnyObjectByType<MapGenerator>();
        float tileSize = mapGen != null ? mapGen.GetTileSize() : 1f;
        Debug.Log($"TileSize: {tileSize}");

        // Calculate world-space bounds of mission zone
        float halfWidth = (zoneSize.x * tileSize) / 2f;
        float halfHeight = (zoneSize.y * tileSize) / 2f;
        Debug.Log($"Half width: {halfWidth}, Half height: {halfHeight}");

        // Try to find valid positions
        int maxAttempts = 50;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Random position within zone for pickup
            pickupPosition = new Vector3(
                zoneCenter.x + Random.Range(-halfWidth, halfWidth),
                zoneCenter.y,
                zoneCenter.z + Random.Range(-halfHeight, halfHeight)
            );

            // Random position within zone for delivery
            deliveryPosition = new Vector3(
                zoneCenter.x + Random.Range(-halfWidth, halfWidth),
                zoneCenter.y,
                zoneCenter.z + Random.Range(-halfHeight, halfHeight)
            );

            // Check if distance is sufficient
            float distance = Vector3.Distance(pickupPosition, deliveryPosition);
            if (distance >= minDistanceBetweenPoints)
            {
                Debug.Log($"Valid positions found on attempt {attempt + 1}, distance: {distance}");
                // Use raycast to find ground level
                pickupPosition = GetGroundPosition(pickupPosition);
                deliveryPosition = GetGroundPosition(deliveryPosition);
                Debug.Log($"After ground position - Pickup: {pickupPosition}, Delivery: {deliveryPosition}");
                return;
            }
        }

        // Fallback: place at opposite corners
        Debug.LogWarning("Could not find valid random positions, using fallback corners");
        pickupPosition = zoneCenter + new Vector3(-halfWidth * 0.7f, 0, -halfHeight * 0.7f);
        deliveryPosition = zoneCenter + new Vector3(halfWidth * 0.7f, 0, halfHeight * 0.7f);
        pickupPosition = GetGroundPosition(pickupPosition);
        deliveryPosition = GetGroundPosition(deliveryPosition);
        Debug.Log($"Fallback positions - Pickup: {pickupPosition}, Delivery: {deliveryPosition}");
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

        // Visual feedback: hide pickup object, show delivery target
        if (pickupVisualInstance != null)
            pickupVisualInstance.SetActive(false);

        if (deliveryVisualInstance != null)
            deliveryVisualInstance.SetActive(true);

        // Add mission marker to minimap for delivery point
        MinimapController minimap = FindAnyObjectByType<MinimapController>();
        if (minimap != null)
        {
            minimap.AddMissionMarker(deliveryPosition);
        }
    }

    private void DeliverItem()
    {
        if (hasPickedUpItem)
        {
            Debug.Log("Item delivered!");
            CompleteMission();
        }
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
