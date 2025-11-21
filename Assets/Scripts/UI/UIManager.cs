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
    [SerializeField] private RawImage fullMapImage;

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

    private PlayerStats playerStats;
    private PlayerController playerController;
    private Gun playerGun;
    private Gun vehicleGun;

    // Mission tracking
    private Dictionary<string, GameObject> missionEntries = new Dictionary<string, GameObject>();

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
    }

    public void Initialize(PlayerStats stats, PlayerController controller, Gun gun)
    {
        playerStats = stats;
        playerController = controller;
        playerGun = gun;

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
        // Toggle inventory with Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }

        // Update vehicle stats if in vehicle
        if (playerController != null && playerController.IsInVehicle())
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
        string ammoString = $"{current}/{max}";

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
        }
    }

    private void UpdateInventoryDisplay()
    {
        // This will be implemented when we create the inventory system
        Debug.Log("Update inventory display");
    }

    private void UpdateFullMap()
    {
        // Use the minimap texture as the full map
        if (minimapController != null && fullMapImage != null)
        {
            fullMapImage.texture = minimapController.GetMinimapTexture();
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
