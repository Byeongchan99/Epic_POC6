using UnityEngine;
using System.Collections.Generic;

public class ProjectilePool : MonoBehaviour
{
    public static ProjectilePool Instance { get; private set; }

    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private Transform poolParent;

    private Queue<Projectile> pool = new Queue<Projectile>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Create pool parent if not assigned
        if (poolParent == null)
        {
            GameObject poolObj = new GameObject("ProjectilePool");
            poolParent = poolObj.transform;
        }

        // Pre-instantiate projectiles
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewProjectile();
        }
    }

    private Projectile CreateNewProjectile()
    {
        GameObject obj = Instantiate(projectilePrefab, poolParent);
        obj.SetActive(false);
        Projectile projectile = obj.GetComponent<Projectile>();
        pool.Enqueue(projectile);
        return projectile;
    }

    public Projectile GetProjectile()
    {
        if (pool.Count == 0)
        {
            // Create new projectile if pool is empty
            return CreateNewProjectile();
        }

        Projectile projectile = pool.Dequeue();
        return projectile;
    }

    public void ReturnProjectile(Projectile projectile)
    {
        projectile.gameObject.SetActive(false);
        projectile.transform.position = poolParent.position;
        pool.Enqueue(projectile);
    }

    public void SetProjectilePrefab(GameObject prefab)
    {
        projectilePrefab = prefab;
    }
}
