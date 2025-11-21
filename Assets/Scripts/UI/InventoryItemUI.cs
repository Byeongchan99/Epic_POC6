using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemQuantityText;
    [SerializeField] private Button useButton;

    private Item.ItemType itemType;
    private PlayerInventory playerInventory;

    public void Initialize(Item.ItemType type, int quantity, PlayerInventory inventory)
    {
        itemType = type;
        playerInventory = inventory;

        // Set item name in Korean
        if (itemNameText != null)
        {
            itemNameText.text = GetItemNameKorean(type);
        }

        // Set quantity
        UpdateQuantity(quantity);

        // Setup button
        if (useButton != null)
        {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(OnUseButtonClicked);
        }
    }

    public void UpdateQuantity(int quantity)
    {
        if (itemQuantityText != null)
        {
            itemQuantityText.text = $"x{quantity}";
        }

        // Disable button if no items
        if (useButton != null)
        {
            useButton.interactable = quantity > 0;
        }
    }

    private void OnUseButtonClicked()
    {
        if (playerInventory != null)
        {
            playerInventory.UseItem(itemType);
        }
    }

    private string GetItemNameKorean(Item.ItemType type)
    {
        switch (type)
        {
            case Item.ItemType.Medical:
                return "의료 키트";
            case Item.ItemType.Food:
                return "음식";
            case Item.ItemType.Water:
                return "물";
            case Item.ItemType.Parts:
                return "부품";
            case Item.ItemType.Fuel:
                return "연료";
            default:
                return type.ToString();
        }
    }
}
