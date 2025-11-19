using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float damage;
    private float speed;
    private float lifetime;
    private float timer;
    private Vector3 direction;
    private ProjectilePool pool;
    private string ownerTag; // "Player" or "Enemy"

    public void Initialize(float damage, float speed, float lifetime, Vector3 direction, ProjectilePool pool, string ownerTag)
    {
        this.damage = damage;
        this.speed = speed;
        this.lifetime = lifetime;
        this.direction = direction.normalized;
        this.pool = pool;
        this.ownerTag = ownerTag;
        this.timer = 0f;

        gameObject.SetActive(true);
    }

    private void Update()
    {
        // Move projectile
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        // Update timer
        timer += Time.deltaTime;

        // Return to pool when lifetime expires
        if (timer >= lifetime)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Don't hit the owner
        if (other.CompareTag(ownerTag))
            return;

        // Check for damageable object
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            ReturnToPool();
            return;
        }

        // Hit terrain/wall
        if (other.CompareTag("Terrain") || other.CompareTag("Wall"))
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (pool != null)
        {
            pool.ReturnProjectile(this);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
