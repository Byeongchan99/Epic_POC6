using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Player Stats UI - Bottom Left")]
    [SerializeField] private GameObject playerStatsPanel;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider staminaBar;
    [SerializeField] private Slider hungerBar;
    [SerializeField] private Slider thirstBar;
    [SerializeField] private TextMeshProUGUI ammoText;

    [Header("Vehicle Stats UI - Bottom Left")]
    [SerializeField] private GameObject vehicleStatsPanel;
    [SerializeField] private Slider vehicleHealthBar;
    [SerializeField] private Slider fuelBar;
    [SerializeField] private TextMeshProUGUI vehicleAmmoText; // Ammo display for vehicle panel

    [Header("Minimap - Bottom Right")]
    [SerializeField] private MinimapController minimapController;

    [Header("Mission List - Top Right")]
    [SerializeField] private Transform missionListContainer;
    [SerializeField] private GameObject missionEntryPrefab;

    [Header("Inventory Panel - Tab")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform inventoryItemContainer;
    [SerializeField] private GameObject inventoryItemPrefab;
    [SerializeField] private RawImage fullMapImage;
    [SerializeField] private Image fullMapPlayerIcon;
    [SerializeField] private Image fullMapVehicleIcon;

    [Header("Minigame UI")]
    [SerializeField] private GameObject minigamePanel;
    [SerializeField] private TextMeshProUGUI minigameText;
    [SerializeField] private Slider minigameProgressBar;

    [Header("Interaction Prompt")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TextMeshProUGUI interactionPromptText;

    [Header("Notification")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private float notificationDuration = 5f;

    [Header("Crosshair")]
    [SerializeField] private Image crosshairImage;

    private PlayerStats playerStats;
    private PlayerController playerController;
    private PlayerInventory playerInventory;
    private Gun playerGun;
    private Gun vehicleGun;

    // Mission tracking
    private Dictionary<string, GameObject> missionEntries = new Dictionary<string, GameObject>();

    // Inventory tracking
    private Dictionary<Item.ItemType, InventoryItemUI> inventoryItemUIs = new Dictionary<Item.ItemType, InventoryItemUI>();

    // Notification state
    private float notificationTimer = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Hide inventory panel initially
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        // Show player stats, hide vehicle stats initially (player is on foot)
        if (playerStatsPanel != null)
            playerStatsPanel.SetActive(true);

        if (vehicleStatsPanel != null)
            vehicleStatsPanel.SetActive(false);

        // Hide minigame UI initially
        if (minigamePanel != null)
            minigamePanel.SetActive(false);

        // Hide interaction prompt initially
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);

        // Hide notification initially
        if (notificationPanel != null)
            notificationPanel.SetActive(false);

        // Setup crosshair
        if (crosshairImage != null)
        {
            Debug.Log("UIManager: Crosshair image found, setting up cursor");
            // Hide hardware cursor
            Cursor.visible = false;
            // Lock cursor to game window
            Cursor.lockState = CursorLockMode.Confined;
        }
        else
        {
            Debug.LogWarning("UIManager: Crosshair image is NULL! Please assign it in the Inspector.");
        }
    }

    public void Initialize(PlayerStats stats, PlayerController controller, Gun gun)
    {
        playerStats = stats;
        playerController = controller;
        playerGun = gun;

        // Get PlayerInventory
        if (controller != null)
        {
            playerInventory = controller.GetComponent<PlayerInventory>();
            if (playerInventory != null)
            {
                playerInventory.OnInventoryChanged += OnInventoryChanged;
            }
        }

        // Subscribe to events
        if (playerStats != null)
        {
            playerStats.OnHealthChanged += UpdateHealthBar;
            playerStats.OnStaminaChanged += UpdateStaminaBar;
            playerStats.OnHungerChanged += UpdateHungerBar;
            playerStats.OnThirstChanged += UpdateThirstBar;
        }

        if (playerGun != null)
        {
            playerGun.OnAmmoChanged += UpdateAmmoText;
        }

        // Initial update
        UpdateAllUI();
    }

    private void Update()
    {
        // Update crosshair position
        UpdateCrosshair();

        // Toggle inventory with Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }

        // Always update vehicle stats to handle transitions
        if (playerController != null)
        {
            UpdateVehicleStats();
        }

        // Update notification timer
        if (notificationTimer > 0f)
        {
            notificationTimer -= Time.deltaTime;
            if (notificationTimer <= 0f && notificationPanel != null)
            {
                notificationPanel.SetActive(false);
            }
        }
    }

    private void UpdateCrosshair()
    {
        if (crosshairImage != null && crosshairImage.gameObject.activeInHierarchy)
        {
            // Update crosshair position to follow mouse
            Vector2 mousePosition = Input.mousePosition;
            RectTransform rectTransform = crosshairImage.rectTransform;
            if (rectTransform != null)
            {
                // Get the Canvas to convert screen coordinates properly
                Canvas canvas = crosshairImage.canvas;
                if (canvas != null)
                {
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvas.transform as RectTransform,
                        mousePosition,
                        canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                        out localPoint
                    );

                    rectTransform.anchoredPosition = localPoint;
                }
                else
                {
                    // Fallback: direct position assignment
                    rectTransform.anchoredPosition = mousePosition;
                }
            }
            else
            {
                Debug.LogWarning("UIManager: Crosshair RectTransform is null!");
            }
        }
    }

    private void UpdateAllUI()
    {
        if (playerStats != null)
        {
            UpdateHealthBar(playerStats.GetHealth(), playerStats.GetMaxHealth());
            UpdateStaminaBar(playerStats.GetStamina(), playerStats.GetMaxStamina());
            UpdateHungerBar(playerStats.GetHunger(), playerStats.GetMaxHunger());
            UpdateThirstBar(playerStats.GetThirst(), playerStats.GetMaxThirst());
        }

        if (playerGun != null)
        {
            UpdateAmmoText(playerGun.GetCurrentAmmo(), playerGun.GetMaxAmmo());
        }
    }

    private void UpdateHealthBar(float current, float max)
    {
        if (healthBar != null)
            healthBar.value = current / max;
    }

    private void UpdateStaminaBar(float current, float max)
    {
        if (staminaBar != null)
            staminaBar.value = current / max;
    }

    private void UpdateHungerBar(float current, float max)
    {
        if (hungerBar != null)
            hungerBar.value = current / max;
    }

    private void UpdateThirstBar(float current, float max)
    {
        if (thirstBar != null)
            thirstBar.value = current / max;
    }

    private void UpdateAmmoText(int current, int max)
    {
        string ammoString = $"남은 탄약: {current}/{max}";

        // Update player panel ammo text
        if (ammoText != null)
            ammoText.text = ammoString;

        // Update vehicle panel ammo text
        if (vehicleAmmoText != null)
            vehicleAmmoText.text = ammoString;
    }

    private void UpdateVehicleStats()
    {
        Vehicle vehicle = playerController.GetCurrentVehicle();

        if (vehicle != null)
        {
            // Player is in vehicle - show vehicle stats, hide player stats
            if (vehicleStatsPanel != null && !vehicleStatsPanel.activeSelf)
                vehicleStatsPanel.SetActive(true);

            if (playerStatsPanel != null && playerStatsPanel.activeSelf)
                playerStatsPanel.SetActive(false);

            if (vehicleHealthBar != null)
                vehicleHealthBar.value = vehicle.GetHealth() / vehicle.GetMaxHealth();

            if (fuelBar != null)
                fuelBar.value = vehicle.GetFuel() / vehicle.GetMaxFuel();
        }
        else
        {
            // Player is on foot - show player stats, hide vehicle stats
            if (vehicleStatsPanel != null && vehicleStatsPanel.activeSelf)
                vehicleStatsPanel.SetActive(false);

            if (playerStatsPanel != null && !playerStatsPanel.activeSelf)
                playerStatsPanel.SetActive(true);
        }
    }

    private void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            bool isActive = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(isActive);

            if (isActive)
            {
                UpdateInventoryDisplay();
                UpdateFullMap();
            }

            // Pause game when inventory is open (optional)
            Time.timeScale = isActive ? 0f : 1f;

            // Show/hide cursor and crosshair based on inventory state
            if (crosshairImage != null)
            {
                Cursor.visible = isActive; // Show cursor when inventory is open
                crosshairImage.gameObject.SetActive(!isActive); // Hide crosshair when inventory is open
            }
        }
    }

    private void UpdateInventoryDisplay()
    {
        if (playerInventory == null || inventoryItemContainer == null || inventoryItemPrefab == null)
        {
            Debug.LogWarning("UIManager: Cannot update inventory display - missing references");
            return;
        }

        // Get all items from inventory
        Dictionary<Item.ItemType, int> items = playerInventory.GetAllItems();

        // Update or create UI for each item type
        foreach (Item.ItemType itemType in System.Enum.GetValues(typeof(Item.ItemType)))
        {
            int quantity = items.ContainsKey(itemType) ? items[itemType] : 0;

            // Only show items that exist
            if (quantity > 0)
            {
                if (!inventoryItemUIs.ContainsKey(itemType))
                {
                    // Create new item UI
                    GameObject itemUIObj = Instantiate(inventoryItemPrefab, inventoryItemContainer);
                    InventoryItemUI itemUI = itemUIObj.GetComponent<InventoryItemUI>();

                    if (itemUI != null)
                    {
                        itemUI.Initialize(itemType, quantity, playerInventory);
                        inventoryItemUIs[itemType] = itemUI;
                    }
                }
                else
                {
                    // Update existing item UI
                    inventoryItemUIs[itemType].UpdateQuantity(quantity);
                }
            }
            else
            {
                // Remove item UI if quantity is 0
                if (inventoryItemUIs.ContainsKey(itemType))
                {
                    Destroy(inventoryItemUIs[itemType].gameObject);
                    inventoryItemUIs.Remove(itemType);
                }
            }
        }
    }

    private void OnInventoryChanged(Item.ItemType type, int quantity)
    {
        // If inventory panel is open, update display immediately
        if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
            UpdateInventoryDisplay();
        }
    }

    private void UpdateFullMap()
    {
        // Use the minimap texture as the full map
        if (minimapController != null && fullMapImage != null)
        {
            fullMapImage.texture = minimapController.GetMinimapTexture();

            // Update player and vehicle icons on full map
            UpdateFullMapIcons();
        }
    }

    private void UpdateFullMapIcons()
    {
        if (minimapController == null || fullMapImage == null || playerController == null)
            return;

        RectTransform fullMapRect = fullMapImage.rectTransform;
        if (fullMapRect == null)
            return;

        // Update player icon
        if (fullMapPlayerIcon != null)
        {
            Vector3 playerWorldPos;
            float playerAngle;

            if (playerController.IsInVehicle())
            {
                // Player is in vehicle - show player icon at vehicle position
                Vehicle vehicle = playerController.GetCurrentVehicle();
                if (vehicle != null)
                {
                    playerWorldPos = vehicle.transform.position;
                    playerAngle = -vehicle.transform.eulerAngles.y;
                }
                else
                {
                    playerWorldPos = playerController.transform.position;
                    playerAngle = -playerController.transform.eulerAngles.y;
                }
            }
            else
            {
                // Player is on foot
                playerWorldPos = playerController.transform.position;
                playerAngle = -playerController.transform.eulerAngles.y;
            }

            Vector2 playerMapPos = minimapController.WorldToMinimapPosition(playerWorldPos, fullMapRect);
            fullMapPlayerIcon.rectTransform.anchoredPosition = playerMapPos;
            fullMapPlayerIcon.rectTransform.rotation = Quaternion.Euler(0, 0, playerAngle);
            fullMapPlayerIcon.gameObject.SetActive(true);
        }

        // Update vehicle icon
        if (fullMapVehicleIcon != null)
        {
            Vehicle vehicle = FindAnyObjectByType<Vehicle>();
            bool playerInVehicle = playerController.IsInVehicle();

            if (vehicle != null && !playerInVehicle)
            {
                // Show vehicle icon when player is NOT in it
                Vector3 vehicleWorldPos = vehicle.transform.position;
                Vector2 vehicleMapPos = minimapController.WorldToMinimapPosition(vehicleWorldPos, fullMapRect);

                fullMapVehicleIcon.rectTransform.anchoredPosition = vehicleMapPos;
                fullMapVehicleIcon.rectTransform.rotation = Quaternion.Euler(0, 0, -vehicle.transform.eulerAngles.y);
                fullMapVehicleIcon.gameObject.SetActive(true);
            }
            else
            {
                // Hide vehicle icon when player is in it or vehicle doesn't exist
                fullMapVehicleIcon.gameObject.SetActive(false);
            }
        }
    }

    public MinimapController GetMinimapController()
    {
        return minimapController;
    }

    public void AddMissionToList(string missionName)
    {
        if (missionEntryPrefab != null && missionListContainer != null)
        {
            GameObject entry = Instantiate(missionEntryPrefab, missionListContainer);

            // Try to get MissionEntryUI component (new method - Inspector assignment)
            MissionEntryUI entryUI = entry.GetComponent<MissionEntryUI>();
            if (entryUI != null)
            {
                // Use component-based approach
                entryUI.SetMissionName(missionName);
                entryUI.SetCheckboxState(false); // Start unchecked

                if (!entryUI.HasValidCheckboxes())
                {
                    Debug.LogWarning($"UIManager: MissionEntryUI has missing checkbox references! Assign them in Inspector.");
                }
            }
            else
            {
                // Fallback to old method (Find by name)
                Debug.LogWarning($"UIManager: Add MissionEntryUI component to prefab for Inspector assignment!");

                TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = missionName;
                }

                Transform uncheckedTransform = entry.transform.Find("CheckboxUnchecked");
                Transform checkedTransform = entry.transform.Find("CheckboxChecked");

                if (uncheckedTransform != null && checkedTransform != null)
                {
                    uncheckedTransform.gameObject.SetActive(true);
                    checkedTransform.gameObject.SetActive(false);
                }
            }

            // Store entry for later updates
            missionEntries[missionName] = entry;
            Debug.Log($"UIManager: Added mission '{missionName}' to list (unchecked)");
        }
    }

    public void UpdateMissionStatus(string missionName, bool isCompleted)
    {
        if (missionEntries.ContainsKey(missionName))
        {
            GameObject entry = missionEntries[missionName];

            // Try to get MissionEntryUI component (new method - Inspector assignment)
            MissionEntryUI entryUI = entry.GetComponent<MissionEntryUI>();
            if (entryUI != null)
            {
                entryUI.SetCheckboxState(isCompleted);
                Debug.Log($"UIManager: Updated mission '{missionName}' checkbox to {(isCompleted ? "checked" : "unchecked")}");
            }
            else
            {
                // Fallback to old method (Find by name)
                Transform uncheckedTransform = entry.transform.Find("CheckboxUnchecked");
                Transform checkedTransform = entry.transform.Find("CheckboxChecked");

                if (uncheckedTransform != null && checkedTransform != null)
                {
                    if (isCompleted)
                    {
                        uncheckedTransform.gameObject.SetActive(false);
                        checkedTransform.gameObject.SetActive(true);
                    }
                    else
                    {
                        uncheckedTransform.gameObject.SetActive(true);
                        checkedTransform.gameObject.SetActive(false);
                    }
                    Debug.Log($"UIManager: Updated mission '{missionName}' checkbox to {(isCompleted ? "checked" : "unchecked")}");
                }
                else
                {
                    Debug.LogWarning($"UIManager: CheckboxUnchecked or CheckboxChecked not found in mission entry for '{missionName}'");
                }
            }
        }
        else
        {
            Debug.LogWarning($"UIManager: Mission '{missionName}' not found in mission entries");
        }
    }

    public void ClearMissionList()
    {
        if (missionListContainer != null)
        {
            foreach (Transform child in missionListContainer)
            {
                Destroy(child.gameObject);
            }
        }

        missionEntries.Clear();
    }

    public void ConnectVehicleGun(Gun gun)
    {
        // Disconnect old vehicle gun if any
        if (vehicleGun != null)
        {
            vehicleGun.OnAmmoChanged -= UpdateAmmoText;
        }

        // Connect new vehicle gun
        vehicleGun = gun;
        if (vehicleGun != null)
        {
            vehicleGun.OnAmmoChanged += UpdateAmmoText;
            // Trigger initial update
            UpdateAmmoText(vehicleGun.GetCurrentAmmo(), vehicleGun.GetMaxAmmo());
        }
    }

    public void DisconnectVehicleGun()
    {
        if (vehicleGun != null)
        {
            vehicleGun.OnAmmoChanged -= UpdateAmmoText;
            vehicleGun = null;
        }

        // Restore player gun ammo display
        if (playerGun != null)
        {
            UpdateAmmoText(playerGun.GetCurrentAmmo(), playerGun.GetMaxAmmo());
        }
    }

    // Minigame UI methods
    public void ShowMinigamePanel(bool show)
    {
        if (minigamePanel != null)
        {
            minigamePanel.SetActive(show);

            // Reset progress bar when showing
            if (show && minigameProgressBar != null)
            {
                minigameProgressBar.value = 0f;
            }
        }
    }

    public void UpdateMinigameText(string text)
    {
        if (minigameText != null)
        {
            minigameText.text = text;
        }
    }

    public void UpdateMinigameProgress(float progress)
    {
        if (minigameProgressBar != null)
        {
            minigameProgressBar.value = progress;
        }
    }

    // Interaction prompt methods
    public void ShowInteractionPrompt(bool show, string text = "")
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(show);
        }

        if (interactionPromptText != null && show)
        {
            interactionPromptText.text = text;
        }
    }

    // Notification methods
    public void ShowNotification(string message, float duration = -1f)
    {
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(true);
        }

        if (notificationText != null)
        {
            notificationText.text = message;
        }

        // Use default duration if not specified
        notificationTimer = duration > 0f ? duration : notificationDuration;
    }

    public void HideNotification()
    {
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
        notificationTimer = 0f;
    }
}
