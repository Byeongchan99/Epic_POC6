using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Gun Stats")]
    [SerializeField] private float range = 50f;
    [SerializeField] private float bulletSpeed = 30f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float fireRate = 0.2f; // Time between shots
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private int currentAmmo = 30;
    [SerializeField] private float reloadTime = 2f;

    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private ProjectilePool projectilePool;
    [SerializeField] private string ownerTag = "Player"; // "Player" or "Enemy"

    [Header("Collision Settings")]
    [Tooltip("Layers that projectiles should collide with (walls, terrain, etc.)")]
    [SerializeField] private LayerMask obstacleLayer = -1; // Default: all layers

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private float nextFireTime;
    private bool isReloading;
    private float reloadTimer;
    private PlayerController playerController; // For getting vehicle velocity

    // Events
    public System.Action<int, int> OnAmmoChanged;
    public System.Action OnReloadStart;
    public System.Action OnReloadComplete;

    private void Start()
    {
        // Automatically get ProjectilePool singleton instance
        if (projectilePool == null)
        {
            projectilePool = ProjectilePool.Instance;
        }

        // Try to find PlayerController (for vehicle velocity tracking)
        if (ownerTag == "Player")
        {
            playerController = GetComponentInParent<PlayerController>();
            if (enableDebugLogs)
            {
                if (playerController == null)
                {
                    Debug.LogWarning($"[Gun Start] PlayerController not found in parent hierarchy! Gun is on: {gameObject.name}");
                }
                else
                {
                    Debug.Log($"[Gun Start] PlayerController found successfully on: {playerController.gameObject.name}");
                }
            }
        }

        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
    }

    private void Update()
    {
        if (isReloading)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0)
            {
                CompleteReload();
            }
        }
    }

    public void Fire()
    {
        if (isReloading)
            return;

        if (currentAmmo <= 0)
        {
            Debug.Log("Out of ammo! Reload needed.");
            return;
        }

        if (Time.time < nextFireTime)
            return;

        // Get direction
        Vector3 direction = GetFireDirection();

        // Get vehicle velocity if player is in vehicle
        Vector3 vehicleVelocity = Vector3.zero;
        if (playerController != null && playerController.IsInVehicle())
        {
            Vehicle currentVehicle = playerController.GetCurrentVehicle();
            if (currentVehicle != null)
            {
                vehicleVelocity = currentVehicle.GetVelocity();
                if (enableDebugLogs)
                {
                    Debug.Log($"[Gun Fire] In vehicle - Velocity: {vehicleVelocity.magnitude:F1} m/s ({vehicleVelocity.x:F1}, {vehicleVelocity.y:F1}, {vehicleVelocity.z:F1})");
                }
            }
            else if (enableDebugLogs)
            {
                Debug.LogWarning("[Gun Fire] In vehicle but currentVehicle is NULL!");
            }
        }
        else if (enableDebugLogs)
        {
            Debug.Log($"[Gun Fire] On foot - playerController null? {playerController == null}");
        }

        // Spawn projectile from pool
        if (projectilePool != null)
        {
            Projectile projectile = projectilePool.GetProjectile();

            if (projectile != null)
            {
                float lifetime = range / bulletSpeed;

                projectile.transform.position = firePoint.position;
                projectile.transform.rotation = Quaternion.LookRotation(direction);

                // Initialize with vehicle velocity if in vehicle
                if (vehicleVelocity != Vector3.zero)
                {
                    projectile.Initialize(damage, bulletSpeed, lifetime, direction, projectilePool, ownerTag, obstacleLayer, vehicleVelocity, enableDebugLogs);
                }
                else
                {
                    projectile.Initialize(damage, bulletSpeed, lifetime, direction, projectilePool, ownerTag, obstacleLayer, enableDebugLogs);
                }

                // Consume ammo
                currentAmmo--;
                OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);

                // Set next fire time
                nextFireTime = Time.time + fireRate;

                if (enableDebugLogs) Debug.Log($"Fired! Ammo: {currentAmmo}/{maxAmmo}");
            }
        }
        else
        {
            Debug.LogWarning("ProjectilePool not found!");
        }
    }

    public void Fire(Vector3 targetDirection)
    {
        if (isReloading)
            return;

        if (currentAmmo <= 0)
            return;

        if (Time.time < nextFireTime)
            return;

        // Get vehicle velocity if player is in vehicle
        Vector3 vehicleVelocity = Vector3.zero;
        if (playerController != null && playerController.IsInVehicle())
        {
            Vehicle currentVehicle = playerController.GetCurrentVehicle();
            if (currentVehicle != null)
            {
                vehicleVelocity = currentVehicle.GetVelocity();
                if (enableDebugLogs)
                {
                    Debug.Log($"[Gun Fire(dir)] In vehicle - Velocity: {vehicleVelocity.magnitude:F1} m/s");
                }
            }
        }

        // Spawn projectile from pool
        if (projectilePool != null)
        {
            Projectile projectile = projectilePool.GetProjectile();

            if (projectile != null)
            {
                float lifetime = range / bulletSpeed;

                projectile.transform.position = firePoint.position;
                projectile.transform.rotation = Quaternion.LookRotation(targetDirection);

                // Initialize with vehicle velocity if in vehicle
                if (vehicleVelocity != Vector3.zero)
                {
                    projectile.Initialize(damage, bulletSpeed, lifetime, targetDirection, projectilePool, ownerTag, obstacleLayer, vehicleVelocity, enableDebugLogs);
                }
                else
                {
                    projectile.Initialize(damage, bulletSpeed, lifetime, targetDirection, projectilePool, ownerTag, obstacleLayer, enableDebugLogs);
                }

                // Consume ammo
                currentAmmo--;
                OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);

                // Set next fire time
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    private Vector3 GetFireDirection()
    {
        // For player, this will be overridden by PlayerController.GetAimDirection()
        // For enemy, use forward direction
        return firePoint.forward;
    }

    public void Reload()
    {
        if (isReloading)
            return;

        if (currentAmmo >= maxAmmo)
        {
            Debug.Log("Already full ammo");
            return;
        }

        isReloading = true;
        reloadTimer = reloadTime;
        OnReloadStart?.Invoke();

        Debug.Log("Reloading...");
    }

    private void CompleteReload()
    {
        isReloading = false;
        currentAmmo = maxAmmo;
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        OnReloadComplete?.Invoke();

        Debug.Log("Reload complete!");
    }

    // Getters
    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => maxAmmo;
    public bool IsReloading() => isReloading;

    // Setters for customization
    public void SetProjectilePool(ProjectilePool pool)
    {
        projectilePool = pool;
    }

    public void SetOwnerTag(string tag)
    {
        ownerTag = tag;
    }

    public void SetFirePoint(Transform point)
    {
        firePoint = point;
    }

    public void SetPlayerController(PlayerController controller)
    {
        playerController = controller;
        if (enableDebugLogs)
        {
            Debug.Log($"[Gun] PlayerController set to: {(controller != null ? controller.gameObject.name : "NULL")}");
        }
    }
}
