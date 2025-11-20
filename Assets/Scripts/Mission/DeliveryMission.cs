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

        // Find player
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.transform;
        }

        // Generate random positions for pickup and delivery points within mission zone
        GeneratePointPositions();

        // Instantiate visual objects at those positions
        if (pickupVisualPrefab != null)
        {
            pickupVisualInstance = Instantiate(pickupVisualPrefab, pickupPosition, Quaternion.identity, transform);
            pickupVisualInstance.name = "PickupPoint";
        }

        if (deliveryVisualPrefab != null)
        {
            deliveryVisualInstance = Instantiate(deliveryVisualPrefab, deliveryPosition, Quaternion.identity, transform);
            deliveryVisualInstance.name = "DeliveryPoint";
            deliveryVisualInstance.SetActive(false); // Hide until item is picked up
        }

        Debug.Log($"Delivery mission initialized - Pickup: {pickupPosition}, Delivery: {deliveryPosition}");
    }

    private void GeneratePointPositions()
    {
        // Get mission zone size from MissionZoneInfo
        MissionZoneInfo zoneInfo = GetComponent<MissionZoneInfo>();
        if (zoneInfo == null)
        {
            Debug.LogError("MissionZoneInfo not found on DeliveryMission!");
            // Fallback to default positions
            pickupPosition = transform.position + new Vector3(-5, 0, -5);
            deliveryPosition = transform.position + new Vector3(5, 0, 5);
            return;
        }

        Vector2Int zoneSize = zoneInfo.size;
        Vector3 zoneCenter = transform.position;

        // Get MapGenerator for tileSize
        MapGenerator mapGen = FindAnyObjectByType<MapGenerator>();
        float tileSize = mapGen != null ? mapGen.GetTileSize() : 1f;

        // Calculate world-space bounds of mission zone
        float halfWidth = (zoneSize.x * tileSize) / 2f;
        float halfHeight = (zoneSize.y * tileSize) / 2f;

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
                // Use raycast to find ground level
                pickupPosition = GetGroundPosition(pickupPosition);
                deliveryPosition = GetGroundPosition(deliveryPosition);
                return;
            }
        }

        // Fallback: place at opposite corners
        pickupPosition = zoneCenter + new Vector3(-halfWidth * 0.7f, 0, -halfHeight * 0.7f);
        deliveryPosition = zoneCenter + new Vector3(halfWidth * 0.7f, 0, halfHeight * 0.7f);
        pickupPosition = GetGroundPosition(pickupPosition);
        deliveryPosition = GetGroundPosition(deliveryPosition);
    }

    private Vector3 GetGroundPosition(Vector3 position)
    {
        RaycastHit hit;
        Vector3 rayStart = new Vector3(position.x, position.y + 10f, position.z);

        // Raycast down to find terrain
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 50f))
        {
            if (hit.collider.CompareTag("Terrain"))
            {
                return hit.point + Vector3.up * 0.1f; // Slightly above ground
            }
        }

        // Fallback: use original Y position
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
