using UnityEngine;

public class Item : MonoBehaviour, IInteractable
{
    [Header("Item Info")]
    [SerializeField] private ItemType itemType;
    [SerializeField] private float value = 20f; // Amount to restore/add

    public enum ItemType
    {
        Medical,    // Restores health
        Food,       // Restores hunger
        Water,      // Restores thirst
        Parts,      // Repairs vehicle
        Fuel        // Refuels vehicle
    }

    private void Start()
    {
        // Check if this item has a collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"[Item] {gameObject.name} ({itemType}) does NOT have a Collider! Add a Collider component to make this item interactable.", this);
        }
        else
        {
            Debug.Log($"[Item] {gameObject.name} ({itemType}) has Collider: {col.GetType().Name}", this);
        }
    }

    public void Interact(PlayerController player)
    {
        // Pick up item
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();

        if (inventory != null)
        {
            inventory.AddItem(itemType, 1);
            Debug.Log($"Picked up {itemType}");

            // Destroy item
            Destroy(gameObject);
        }
    }

    public string GetInteractionPrompt()
    {
        return $"Press F to pick up {itemType}";
    }

    public ItemType GetItemType() => itemType;
    public float GetValue() => value;

    // Visualize interaction range in Scene View
    private void OnDrawGizmosSelected()
    {
        // Draw the interaction range sphere (matches PlayerController's interactionRadius of 3f)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 3f);

        // Check if collider exists
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // Draw red sphere to indicate missing collider
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
