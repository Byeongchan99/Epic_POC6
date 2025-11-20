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

    private PlayerStats playerStats;
    private PlayerController playerController;
    private Gun playerGun;
    private Gun vehicleGun;

    // Mission tracking
    private Dictionary<string, GameObject> missionEntries = new Dictionary<string, GameObject>();

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
            TextMeshProUGUI text = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = missionName;
            }

            // Store entry for later updates
            missionEntries[missionName] = entry;

            // Find checkbox and uncheck it initially
            Toggle checkbox = entry.GetComponentInChildren<Toggle>();
            if (checkbox != null)
            {
                checkbox.isOn = false;
            }

            Debug.Log($"UIManager: Added mission '{missionName}' to list");
        }
    }

    public void UpdateMissionStatus(string missionName, bool isCompleted)
    {
        if (missionEntries.ContainsKey(missionName))
        {
            GameObject entry = missionEntries[missionName];
            Toggle checkbox = entry.GetComponentInChildren<Toggle>();
            if (checkbox != null)
            {
                checkbox.isOn = isCompleted;
                Debug.Log($"UIManager: Updated mission '{missionName}' checkbox to {isCompleted}");
            }
            else
            {
                Debug.LogWarning($"UIManager: No Toggle (checkbox) found in mission entry for '{missionName}'");
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
}
