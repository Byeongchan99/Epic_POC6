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
}
