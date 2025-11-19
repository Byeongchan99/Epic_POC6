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

    private float nextFireTime;
    private bool isReloading;
    private float reloadTimer;

    // Events
    public System.Action<int, int> OnAmmoChanged;
    public System.Action OnReloadStart;
    public System.Action OnReloadComplete;

    private void Start()
    {
        if (projectilePool == null)
        {
            projectilePool = FindObjectOfType<ProjectilePool>();
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

        // Spawn projectile from pool
        if (projectilePool != null)
        {
            Projectile projectile = projectilePool.GetProjectile();

            if (projectile != null)
            {
                float lifetime = range / bulletSpeed;

                projectile.transform.position = firePoint.position;
                projectile.transform.rotation = Quaternion.LookRotation(direction);
                projectile.Initialize(damage, bulletSpeed, lifetime, direction, projectilePool, ownerTag);

                // Consume ammo
                currentAmmo--;
                OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);

                // Set next fire time
                nextFireTime = Time.time + fireRate;

                Debug.Log($"Fired! Ammo: {currentAmmo}/{maxAmmo}");
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

        // Spawn projectile from pool
        if (projectilePool != null)
        {
            Projectile projectile = projectilePool.GetProjectile();

            if (projectile != null)
            {
                float lifetime = range / bulletSpeed;

                projectile.transform.position = firePoint.position;
                projectile.transform.rotation = Quaternion.LookRotation(targetDirection);
                projectile.Initialize(damage, bulletSpeed, lifetime, targetDirection, projectilePool, ownerTag);

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
}
