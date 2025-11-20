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

    [Header("Debug Info (Read Only)")]
    [SerializeField] private bool isGrounded;
    [SerializeField] private float distanceToGround;
    [SerializeField] private string groundObjectName;
    [SerializeField] private int groundLayer;
    [SerializeField] private Vector3 vehiclePosition;
    [SerializeField] private Vector3 sphereRBPosition;
    [SerializeField] private int drivableSurfaceMask;
    [SerializeField] private float sphereRadius;
    [SerializeField] private float raycastMaxDistance;
    [SerializeField] private string groundCheckMode;

    private float currentHealth;
    private float currentFuel;
    private PlayerController currentDriver;
    private bool isOccupied;
    private InputManager_ArcadeVP inputManager;

    private void Start()
    {
        currentHealth = maxHealth;
        currentFuel = maxFuel;

        if (mainCamera == null)
            mainCamera = Camera.main;

        // Auto-find Arcade Vehicle Physics components if not assigned
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

        // Find InputManager_ArcadeVP (original asset's input handler)
        inputManager = GetComponent<InputManager_ArcadeVP>();
        if (inputManager == null)
        {
            Debug.LogWarning("InputManager_ArcadeVP not found! Add this component for vehicle input control.");
        }
        else
        {
            // Disable input manager until player enters vehicle
            inputManager.enabled = false;
            Debug.Log("InputManager_ArcadeVP found and disabled until player enters");
        }

        // Keep ArcadeVehicleController always enabled for continuous physics
        if (arcadeVehicleController != null)
        {
            arcadeVehicleController.enabled = true;
        }
    }

    private void Update()
    {
        // Update debug info every frame for Inspector visibility
        UpdateDebugInfo();

        if (isOccupied)
        {
            // Handle game-specific logic (stats only, not input!)
            ConsumeFuel();
            CheckFuelForInput();
            HandleVehicleShooting();
            HandleExit();
            HandleDebugKeys();
        }
    }

    private void UpdateDebugInfo()
    {
        if (arcadeVehicleController == null)
            return;

        // Get ArcadeVehicleController settings
        drivableSurfaceMask = arcadeVehicleController.drivableSurface.value;
        groundCheckMode = arcadeVehicleController.GroundCheck.ToString();

        if (arcadeVehicleController.rb != null)
        {
            SphereCollider sphereCol = arcadeVehicleController.rb.GetComponent<SphereCollider>();
            if (sphereCol != null)
            {
                sphereRadius = sphereCol.radius;
                raycastMaxDistance = sphereCol.radius + 0.2f;
            }
        }

        // Check if grounded using ArcadeVehicleController's method
        isGrounded = arcadeVehicleController.grounded();

        // Get vehicle positions
        vehiclePosition = transform.position;
        if (arcadeVehicleController.rb != null)
        {
            sphereRBPosition = arcadeVehicleController.rb.position;
        }

        // Perform manual raycast to get detailed info (without LayerMask to see everything)
        if (arcadeVehicleController.rb != null)
        {
            RaycastHit hit;
            Vector3 rayOrigin = arcadeVehicleController.rb.position;
            float maxDistance = 10f; // Check further down to see what's below

            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, maxDistance))
            {
                distanceToGround = hit.distance;
                groundObjectName = hit.collider.gameObject.name;
                groundLayer = hit.collider.gameObject.layer;

                // Check if this layer is included in drivableSurface mask
                int layerBit = 1 << hit.collider.gameObject.layer;
                bool isInMask = (drivableSurfaceMask & layerBit) != 0;

                if (!isInMask)
                {
                    groundObjectName += " (NOT IN DRIVABLE MASK!)";
                }
            }
            else
            {
                distanceToGround = -1f;
                groundObjectName = "No ground detected within 10m";
                groundLayer = -1;
            }
        }
    }

    private void CheckFuelForInput()
    {
        if (inputManager == null)
            return;

        // Disable input if out of fuel, enable if has fuel
        if (currentFuel <= 0 && inputManager.enabled)
        {
            inputManager.enabled = false;
            Debug.Log("Out of fuel! Vehicle input disabled.");
        }
        else if (currentFuel > 0 && !inputManager.enabled)
        {
            inputManager.enabled = true;
            Debug.Log("Vehicle refueled! Input enabled.");
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

        // Set player controller for vehicle gun (for velocity tracking)
        if (vehicleGun != null)
        {
            vehicleGun.SetPlayerController(player);
        }

        // Enable InputManager_ArcadeVP to allow player control (original asset's input system)
        if (inputManager != null && currentFuel > 0)
        {
            inputManager.enabled = true;
            Debug.Log("InputManager enabled - vehicle control active");
        }

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

        // Disable InputManager_ArcadeVP (original asset's input system)
        if (inputManager != null)
        {
            inputManager.enabled = false;
            Debug.Log("InputManager disabled - vehicle input deactivated");
        }

        // Tell player to exit
        currentDriver.ExitVehicle(exitPosition);

        // Switch camera back to player and disable vehicle mode
        TopDownCamera camera = FindAnyObjectByType<TopDownCamera>();
        if (camera != null)
        {
            camera.SetTarget(currentDriver.transform);
            camera.EnableVehicleMode(false); // Restore player camera settings
        }

        // Clear player controller from vehicle gun
        if (vehicleGun != null)
        {
            vehicleGun.SetPlayerController(null);
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

    public Vector3 GetVelocity()
    {
        if (arcadeVehicleController != null && arcadeVehicleController.rb != null)
        {
            return arcadeVehicleController.rb.linearVelocity;
        }
        return Vector3.zero;
    }

    // Visualize grounded check in Scene View
    private void OnDrawGizmos()
    {
        if (arcadeVehicleController == null || arcadeVehicleController.rb == null)
            return;

        Vector3 rayOrigin = arcadeVehicleController.rb.position;
        float maxDistance = raycastMaxDistance > 0 ? raycastMaxDistance : 1f;

        // Draw raycast line - green if grounded, red if not
        if (isGrounded)
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = Color.red;
        }

        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * maxDistance);
        Gizmos.DrawWireSphere(rayOrigin, 0.1f);

        // Draw hit point if ground detected
        if (distanceToGround > 0)
        {
            Vector3 hitPoint = rayOrigin + Vector3.down * distanceToGround;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(hitPoint, 0.2f);
        }
    }
}
