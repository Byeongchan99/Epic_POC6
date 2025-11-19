using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory")]
    private Dictionary<Item.ItemType, int> items = new Dictionary<Item.ItemType, int>();

    [Header("Item Values")]
    [SerializeField] private float medicalValue = 30f;
    [SerializeField] private float foodValue = 40f;
    [SerializeField] private float waterValue = 40f;
    [SerializeField] private float partsValue = 50f;
    [SerializeField] private float fuelValue = 30f;

    private PlayerStats stats;
    private PlayerController controller;

    public System.Action<Item.ItemType, int> OnInventoryChanged;

    private void Start()
    {
        stats = GetComponent<PlayerStats>();
        controller = GetComponent<PlayerController>();

        // Initialize inventory
        foreach (Item.ItemType type in System.Enum.GetValues(typeof(Item.ItemType)))
        {
            items[type] = 0;
        }
    }

    private void Update()
    {
        // Use items with number keys
        if (Input.GetKeyDown(KeyCode.Alpha1))
            UseItem(Item.ItemType.Medical);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            UseItem(Item.ItemType.Food);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            UseItem(Item.ItemType.Water);
        if (Input.GetKeyDown(KeyCode.Alpha4))
            UseItem(Item.ItemType.Parts);
        if (Input.GetKeyDown(KeyCode.Alpha5))
            UseItem(Item.ItemType.Fuel);
    }

    public void AddItem(Item.ItemType type, int amount)
    {
        if (items.ContainsKey(type))
        {
            items[type] += amount;
        }
        else
        {
            items[type] = amount;
        }

        OnInventoryChanged?.Invoke(type, items[type]);
        Debug.Log($"Added {amount}x {type}. Total: {items[type]}");
    }

    public void UseItem(Item.ItemType type)
    {
        if (!items.ContainsKey(type) || items[type] <= 0)
        {
            Debug.Log($"No {type} in inventory");
            return;
        }

        bool used = false;

        switch (type)
        {
            case Item.ItemType.Medical:
                if (stats != null && stats.GetHealth() < stats.GetMaxHealth())
                {
                    stats.Heal(medicalValue);
                    used = true;
                    Debug.Log($"Used Medical. Restored {medicalValue} health.");
                }
                break;

            case Item.ItemType.Food:
                if (stats != null && stats.GetHunger() < stats.GetMaxHunger())
                {
                    stats.RestoreHunger(foodValue);
                    used = true;
                    Debug.Log($"Used Food. Restored {foodValue} hunger.");
                }
                break;

            case Item.ItemType.Water:
                if (stats != null && stats.GetThirst() < stats.GetMaxThirst())
                {
                    stats.RestoreThirst(waterValue);
                    used = true;
                    Debug.Log($"Used Water. Restored {waterValue} thirst.");
                }
                break;

            case Item.ItemType.Parts:
                if (controller != null && controller.IsInVehicle())
                {
                    Vehicle vehicle = controller.GetCurrentVehicle();
                    if (vehicle != null && vehicle.GetHealth() < vehicle.GetMaxHealth())
                    {
                        vehicle.Repair(partsValue);
                        used = true;
                        Debug.Log($"Used Parts. Repaired {partsValue} vehicle health.");
                    }
                }
                else
                {
                    Debug.Log("Must be in a vehicle to use Parts");
                }
                break;

            case Item.ItemType.Fuel:
                if (controller != null && controller.IsInVehicle())
                {
                    Vehicle vehicle = controller.GetCurrentVehicle();
                    if (vehicle != null && vehicle.GetFuel() < vehicle.GetMaxFuel())
                    {
                        vehicle.Refuel(fuelValue);
                        used = true;
                        Debug.Log($"Used Fuel. Refueled {fuelValue} fuel.");
                    }
                }
                else
                {
                    Debug.Log("Must be in a vehicle to use Fuel");
                }
                break;
        }

        if (used)
        {
            items[type]--;
            OnInventoryChanged?.Invoke(type, items[type]);
        }
    }

    public int GetItemCount(Item.ItemType type)
    {
        if (items.ContainsKey(type))
            return items[type];
        return 0;
    }

    public Dictionary<Item.ItemType, int> GetAllItems()
    {
        return new Dictionary<Item.ItemType, int>(items);
    }
}
