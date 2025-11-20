using UnityEngine;

public class DeliveryMission : MissionBase
{
    [Header("Delivery Mission")]
    [SerializeField] private Transform pickupPoint;
    [SerializeField] private Transform deliveryPoint;
    [SerializeField] private float interactionRange = 2f;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject pickupVisual; // Visual object to hide after pickup
    [SerializeField] private GameObject deliveryVisual; // Visual object to show after pickup

    private bool hasPickedUpItem = false;
    private Transform player;

    public override void Initialize()
    {
        base.Initialize();

        // Find player
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.transform;
        }

        // Initial state: show pickup, hide delivery
        if (pickupVisual != null)
            pickupVisual.SetActive(true);

        if (deliveryVisual != null)
            deliveryVisual.SetActive(false);

        Debug.Log("Delivery mission initialized");
    }

    public override void UpdateMission()
    {
        if (player == null)
            return;

        if (!hasPickedUpItem)
        {
            // Check if player is near pickup point
            if (IsPlayerNearPoint(pickupPoint))
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
            if (IsPlayerNearPoint(deliveryPoint))
            {
                // Show prompt
                if (Input.GetKeyDown(KeyCode.F))
                {
                    DeliverItem();
                }
            }
        }
    }

    private bool IsPlayerNearPoint(Transform point)
    {
        if (point == null || player == null)
            return false;

        float distance = Vector3.Distance(player.position, point.position);
        return distance <= interactionRange;
    }

    private void PickupItem()
    {
        hasPickedUpItem = true;
        Debug.Log("Item picked up! Deliver it to the delivery point.");

        // Visual feedback: hide pickup object, show delivery target
        if (pickupVisual != null)
            pickupVisual.SetActive(false);

        if (deliveryVisual != null)
            deliveryVisual.SetActive(true);

        // Add mission marker to minimap for delivery point
        MinimapController minimap = FindAnyObjectByType<MinimapController>();
        if (minimap != null && deliveryPoint != null)
        {
            minimap.AddMissionMarker(deliveryPoint.position);
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
        // Draw pickup point
        if (pickupPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pickupPoint.position, interactionRange);
        }

        // Draw delivery point
        if (deliveryPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(deliveryPoint.position, interactionRange);
        }

        // Draw line between them
        if (pickupPoint != null && deliveryPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pickupPoint.position, deliveryPoint.position);
        }
    }
}
