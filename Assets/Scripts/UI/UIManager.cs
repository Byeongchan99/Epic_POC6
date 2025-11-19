using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Player Stats UI - Bottom Left")]
    [SerializeField] private Image healthBar;
    [SerializeField] private Image staminaBar;
    [SerializeField] private Image hungerBar;
    [SerializeField] private Image thirstBar;
    [SerializeField] private TextMeshProUGUI ammoText;

    [Header("Vehicle Stats UI - Bottom Left")]
    [SerializeField] private GameObject vehicleStatsPanel;
    [SerializeField] private Image vehicleHealthBar;
    [SerializeField] private Image fuelBar;

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

        // Hide vehicle stats initially
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
            healthBar.fillAmount = current / max;
    }

    private void UpdateStaminaBar(float current, float max)
    {
        if (staminaBar != null)
            staminaBar.fillAmount = current / max;
    }

    private void UpdateHungerBar(float current, float max)
    {
        if (hungerBar != null)
            hungerBar.fillAmount = current / max;
    }

    private void UpdateThirstBar(float current, float max)
    {
        if (thirstBar != null)
            thirstBar.fillAmount = current / max;
    }

    private void UpdateAmmoText(int current, int max)
    {
        if (ammoText != null)
            ammoText.text = $"{current}/{max}";
    }

    private void UpdateVehicleStats()
    {
        Vehicle vehicle = playerController.GetCurrentVehicle();

        if (vehicle != null)
        {
            if (vehicleStatsPanel != null && !vehicleStatsPanel.activeSelf)
                vehicleStatsPanel.SetActive(true);

            if (vehicleHealthBar != null)
                vehicleHealthBar.fillAmount = vehicle.GetHealth() / vehicle.GetMaxHealth();

            if (fuelBar != null)
                fuelBar.fillAmount = vehicle.GetFuel() / vehicle.GetMaxFuel();
        }
        else
        {
            if (vehicleStatsPanel != null && vehicleStatsPanel.activeSelf)
                vehicleStatsPanel.SetActive(false);
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
    }
}
