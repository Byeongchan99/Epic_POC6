using UnityEngine;

public class Vehicle : MonoBehaviour, IDamageable, IInteractable
{
    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxFuel = 100f;
    [SerializeField] private float fuelConsumptionRate = 5f;
    [SerializeField] private float boostFuelMultiplier = 2f;

    [Header("Combat")]
    [SerializeField] private float collisionDamageToEnemy = 50f;
    [SerializeField] private float collisionKnockbackForce = 10f;
    [SerializeField] private float collisionDamageToSelf = 10f;

    [Header("References")]
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Gun vehicleGun;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Camera mainCamera;

    [Header("Arcade Vehicle Physics")]
    [Tooltip("Assign your Arcade Vehicle Physics controller component here (optional)")]
    [SerializeField] private MonoBehaviour arcadeVehicleController;

    private float currentHealth;
    private float currentFuel;
    private PlayerController currentDriver;
    private bool isOccupied;

    private void Start()
    {
        currentHealth = maxHealth;
        currentFuel = maxFuel;

        if (mainCamera == null)
            mainCamera = Camera.main;

        // Auto-find Arcade Vehicle Physics component if not assigned
        // Replace "ArcadeVehicleController" with actual component name from asset
        if (arcadeVehicleController == null)
        {
            // Try to find common vehicle controller component names
            // arcadeVehicleController = GetComponent<ArcadeCarController>();
            // arcadeVehicleController = GetComponent<VehicleController>();
            // Uncomment and replace with actual component type from your asset
        }

        // Start with vehicle controller disabled
        SetVehicleControllerEnabled(false);
    }

    private void Update()
    {
        if (isOccupied)
        {
            // Only handle our custom logic, let Arcade Physics handle movement
            ConsumeFuel();
            CheckFuelForMovement();
            HandleVehicleShooting();
            HandleExit();
            HandleDebugKeys();
        }
    }

    private void CheckFuelForMovement()
    {
        // Disable vehicle movement if out of fuel
        if (currentFuel <= 0)
        {
            SetVehicleControllerEnabled(false);
            Debug.Log("Out of fuel! Vehicle disabled.");
        }
        else if (arcadeVehicleController != null && !arcadeVehicleController.enabled)
        {
            SetVehicleControllerEnabled(true);
        }
    }

    private void SetVehicleControllerEnabled(bool enabled)
    {
        if (arcadeVehicleController != null)
        {
            arcadeVehicleController.enabled = enabled;
        }
    }

    private void ConsumeFuel()
    {
        if (currentFuel <= 0)
            return;

        float consumption = fuelConsumptionRate * Time.deltaTime;

        // Extra consumption for boost
        if (Input.GetKey(KeyCode.LeftShift))
        {
            consumption *= boostFuelMultiplier;
        }

        currentFuel -= consumption;
        currentFuel = Mathf.Max(currentFuel, 0);
    }

    private void HandleVehicleShooting()
    {
        if (vehicleGun == null)
            return;

        // Fire
        if (Input.GetMouseButton(0))
        {
            Vector3 aimDirection = GetAimDirection();
            vehicleGun.Fire(aimDirection);
        }

        // Reload
        if (Input.GetKeyDown(KeyCode.R))
        {
            vehicleGun.Reload();
        }
    }

    private Vector3 GetAimDirection()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 targetPoint = ray.GetPoint(distance);
            Vector3 direction = targetPoint - firePoint.position;
            direction.y = 0;
            return direction.normalized;
        }

        return transform.forward;
    }

    private void HandleExit()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            ExitVehicle();
        }
    }

    private void HandleDebugKeys()
    {
        // F1: Repair vehicle
        if (Input.GetKeyDown(KeyCode.F1))
        {
            RepairFull();
        }

        // F2: Refuel vehicle
        if (Input.GetKeyDown(KeyCode.F2))
        {
            RefuelFull();
        }
    }

    public void EnterVehicle(PlayerController player)
    {
        if (isOccupied)
            return;

        currentDriver = player;
        isOccupied = true;

        // Enable Arcade Vehicle Physics controller
        SetVehicleControllerEnabled(true);

        // Tell player to enter vehicle
        player.EnterVehicle(this);

        // Switch camera target
        TopDownCamera camera = FindAnyObjectByType<TopDownCamera>();
        if (camera != null)
        {
            camera.SetTarget(transform);
        }

        Debug.Log("Player entered vehicle");
    }

    private void ExitVehicle()
    {
        if (currentDriver == null)
            return;

        Vector3 exitPosition = exitPoint != null ? exitPoint.position : transform.position + transform.right * 2f;

        // Disable Arcade Vehicle Physics controller
        SetVehicleControllerEnabled(false);

        // Tell player to exit
        currentDriver.ExitVehicle(exitPosition);

        // Switch camera back to player
        TopDownCamera camera = FindAnyObjectByType<TopDownCamera>();
        if (camera != null)
        {
            camera.SetTarget(currentDriver.transform);
        }

        currentDriver = null;
        isOccupied = false;

        Debug.Log("Player exited vehicle");
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Collision with enemy
        if (collision.gameObject.CompareTag("Enemy"))
        {
            IDamageable enemy = collision.gameObject.GetComponent<IDamageable>();
            if (enemy != null)
            {
                enemy.TakeDamage(collisionDamageToEnemy);

                // Knockback
                Rigidbody enemyRb = collision.gameObject.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    Vector3 knockbackDirection = (collision.transform.position - transform.position).normalized;
                    enemyRb.AddForce(knockbackDirection * collisionKnockbackForce, ForceMode.Impulse);
                }
            }
        }

        // Collision with wall/terrain
        if (collision.gameObject.CompareTag("Terrain") || collision.gameObject.CompareTag("Wall"))
        {
            TakeDamage(collisionDamageToSelf);
        }
    }

    // IDamageable implementation
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"Vehicle took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    private void Die()
    {
        Debug.Log("Vehicle destroyed!");

        // Eject player if occupied
        if (isOccupied && currentDriver != null)
        {
            ExitVehicle();
        }

        // Destroy vehicle or disable it
        gameObject.SetActive(false);
    }

    // IInteractable implementation
    public void Interact(PlayerController player)
    {
        if (!isOccupied)
        {
            EnterVehicle(player);
        }
    }

    public string GetInteractionPrompt()
    {
        return "Press F to enter vehicle";
    }

    // Repair and refuel
    public void Repair(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        Debug.Log($"Vehicle repaired. Health: {currentHealth}/{maxHealth}");
    }

    public void RepairFull()
    {
        currentHealth = maxHealth;
        Debug.Log("Vehicle fully repaired!");
    }

    public void Refuel(float amount)
    {
        currentFuel += amount;
        currentFuel = Mathf.Min(currentFuel, maxFuel);
        Debug.Log($"Vehicle refueled. Fuel: {currentFuel}/{maxFuel}");
    }

    public void RefuelFull()
    {
        currentFuel = maxFuel;
        Debug.Log("Vehicle fully refueled!");
    }

    // Getters
    public float GetHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetFuel() => currentFuel;
    public float GetMaxFuel() => maxFuel;
    public bool IsOccupied() => isOccupied;
}
