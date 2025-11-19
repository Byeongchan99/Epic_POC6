using UnityEngine;
using ArcadeVP;

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
    [SerializeField] private ArcadeVehicleController arcadeVehicleController;

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
        if (arcadeVehicleController == null)
        {
            arcadeVehicleController = GetComponent<ArcadeVehicleController>();

            if (arcadeVehicleController != null)
            {
                Debug.Log("ArcadeVehicleController found and assigned automatically");
            }
            else
            {
                Debug.LogWarning("ArcadeVehicleController not found on vehicle! Vehicle movement will not work.");
            }
        }

        // Keep vehicle controller enabled initially so its Start() can run
        // Then disable it after a short delay
        if (arcadeVehicleController != null)
        {
            arcadeVehicleController.enabled = true;
            Invoke(nameof(DisableVehicleControllerAfterInit), 0.1f);
        }
    }

    private void DisableVehicleControllerAfterInit()
    {
        // Disable vehicle controller until player enters
        if (!isOccupied)
        {
            SetVehicleControllerEnabled(false);
        }
    }

    private void Update()
    {
        if (isOccupied)
        {
            // Provide input to ArcadeVehicleController
            HandleVehicleInput();

            // Handle our custom logic
            ConsumeFuel();
            CheckFuelForMovement();
            HandleVehicleShooting();
            HandleExit();
            HandleDebugKeys();
        }
    }

    private void HandleVehicleInput()
    {
        if (arcadeVehicleController == null)
        {
            Debug.LogWarning("ArcadeVehicleController is null! Cannot handle vehicle input.");
            return;
        }

        // Get input using GetAxisRaw for instant response (no smoothing)
        float steering = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right arrows
        float acceleration = Input.GetAxisRaw("Vertical"); // W/S or Up/Down arrows
        float brake = Input.GetKey(KeyCode.Space) ? 1f : 0f; // Space for brake/drift

        // Debug: Log input when there's any
        if (Mathf.Abs(steering) > 0.01f || Mathf.Abs(acceleration) > 0.01f || brake > 0.01f)
        {
            Debug.Log($"Vehicle Input - Steering: {steering:F2}, Acceleration: {acceleration:F2}, Brake: {brake:F2}, Controller enabled: {arcadeVehicleController.enabled}");
        }

        // Provide input to ArcadeVehicleController
        arcadeVehicleController.ProvideInputs(steering, acceleration, brake);
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
            Debug.Log($"ArcadeVehicleController enabled = {enabled}");
        }
        else
        {
            Debug.LogWarning("Cannot set vehicle controller enabled state - arcadeVehicleController is null!");
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
        {
            Debug.Log("Vehicle already occupied!");
            return;
        }

        Debug.Log($"EnterVehicle called - ArcadeVehicleController: {(arcadeVehicleController != null ? "Found" : "NULL")}");

        currentDriver = player;
        isOccupied = true;

        // Enable Arcade Vehicle Physics controller
        SetVehicleControllerEnabled(true);

        // Diagnostic: Check ArcadeVehicleController setup
        if (arcadeVehicleController != null)
        {
            Debug.Log($"[Vehicle Setup Check]");
            Debug.Log($"  - rb (main Rigidbody): {(arcadeVehicleController.rb != null ? "OK" : "MISSING!")}");
            Debug.Log($"  - carBody (Rigidbody): {(arcadeVehicleController.carBody != null ? "OK" : "MISSING!")}");
            Debug.Log($"  - MaxSpeed: {arcadeVehicleController.MaxSpeed}");
            Debug.Log($"  - Acceleration: {arcadeVehicleController.accelaration}");
            Debug.Log($"  - Turn: {arcadeVehicleController.turn}");
            Debug.Log($"  - MovementMode: {arcadeVehicleController.movementMode}");

            if (arcadeVehicleController.rb != null)
            {
                Debug.Log($"  - rb.mass: {arcadeVehicleController.rb.mass}");
                Debug.Log($"  - rb.isKinematic: {arcadeVehicleController.rb.isKinematic}");

                // Check for required SphereCollider
                SphereCollider sphereCol = arcadeVehicleController.rb.GetComponent<SphereCollider>();
                if (sphereCol != null)
                {
                    Debug.Log($"  - SphereCollider: OK (radius: {sphereCol.radius})");
                }
                else
                {
                    Debug.LogError($"  - SphereCollider: MISSING! ArcadeVehicleController requires a SphereCollider on rb GameObject!");
                }
            }

            // Check and auto-fix LayerMask for drivable surface
            Debug.Log($"  - Drivable Surface LayerMask (before): {arcadeVehicleController.drivableSurface.value}");

            // Auto-fix: Set to Everything if it's 0 (Nothing)
            if (arcadeVehicleController.drivableSurface.value == 0)
            {
                arcadeVehicleController.drivableSurface = ~0; // Everything
                Debug.LogWarning($"  - AUTO-FIXED: drivableSurface was 0 (Nothing), changed to Everything");
            }

            Debug.Log($"  - Drivable Surface LayerMask (after): {arcadeVehicleController.drivableSurface.value}");

            // Start grounded check monitoring
            Invoke(nameof(CheckGroundedStatus), 0.5f);
        }

        // Tell player to enter vehicle
        player.EnterVehicle(this);

        // Switch camera target and enable vehicle mode
        TopDownCamera camera = FindAnyObjectByType<TopDownCamera>();
        if (camera != null)
        {
            camera.SetTarget(transform);
            camera.EnableVehicleMode(true); // Use vehicle-specific camera settings
            Debug.Log("Camera switched to vehicle with vehicle mode enabled");
        }
        else
        {
            Debug.LogWarning("TopDownCamera not found!");
        }

        Debug.Log("Player entered vehicle - ready to drive!");
    }

    private void CheckGroundedStatus()
    {
        if (!isOccupied || arcadeVehicleController == null)
            return;

        // Check if vehicle is grounded
        bool grounded = arcadeVehicleController.grounded();
        Debug.Log($"[Grounded Check] Vehicle is {(grounded ? "GROUNDED" : "NOT GROUNDED (this prevents movement!)")}");

        if (!grounded)
        {
            Debug.LogWarning("Vehicle is not grounded! Check:");
            Debug.LogWarning("  1. Vehicle has SphereCollider on rb GameObject");
            Debug.LogWarning("  2. drivableSurface LayerMask includes ground layer");
            Debug.LogWarning("  3. Ground has proper layer assigned");
            Debug.LogWarning("  4. Vehicle Y position is close to ground");
        }

        // Continue monitoring while occupied
        if (isOccupied)
        {
            Invoke(nameof(CheckGroundedStatus), 2f);
        }
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

        // Switch camera back to player and disable vehicle mode
        TopDownCamera camera = FindAnyObjectByType<TopDownCamera>();
        if (camera != null)
        {
            camera.SetTarget(currentDriver.transform);
            camera.EnableVehicleMode(false); // Restore player camera settings
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

        // Collision with wall/terrain (only take damage if moving fast enough)
        if (collision.gameObject.CompareTag("Terrain") || collision.gameObject.CompareTag("Wall"))
        {
            // Only take damage if collision impact is strong enough
            float impactVelocity = collision.relativeVelocity.magnitude;
            if (impactVelocity > 5f) // Threshold: only damage if moving faster than 5 units/sec
            {
                TakeDamage(collisionDamageToSelf);
            }
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
        Debug.Log($"Vehicle.Interact() called by {player.gameObject.name}. isOccupied: {isOccupied}");

        if (!isOccupied)
        {
            EnterVehicle(player);
        }
        else
        {
            Debug.Log("Vehicle is already occupied!");
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
