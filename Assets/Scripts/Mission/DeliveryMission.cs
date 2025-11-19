using UnityEngine;

public class DeliveryMission : MissionBase
{
    [Header("Delivery Mission")]
    [SerializeField] private Transform pickupPoint;
    [SerializeField] private Transform deliveryPoint;
    [SerializeField] private float interactionRange = 2f;

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

        // TODO: Show visual feedback (disable pickup point visual)
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
