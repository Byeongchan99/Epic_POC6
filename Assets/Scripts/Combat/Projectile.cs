using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float damage;
    private float speed;
    private float lifetime;
    private float timer;
    private Vector3 direction;
    private Vector3 velocity; // Combined velocity (direction * speed + initial velocity)
    private ProjectilePool pool;
    private string ownerTag; // "Player" or "Enemy"
    private bool enableDebugLogs = false; // Set by Gun when initializing

    public void Initialize(float damage, float speed, float lifetime, Vector3 direction, ProjectilePool pool, string ownerTag, bool debugLogs = false)
    {
        this.damage = damage;
        this.speed = speed;
        this.lifetime = lifetime;
        this.direction = direction.normalized;
        this.velocity = this.direction * speed;
        this.pool = pool;
        this.ownerTag = ownerTag;
        this.timer = 0f;
        this.enableDebugLogs = debugLogs;

        gameObject.SetActive(true);
    }

    // Overload with initial velocity (e.g., from vehicle movement)
    public void Initialize(float damage, float speed, float lifetime, Vector3 direction, ProjectilePool pool, string ownerTag, Vector3 initialVelocity, bool debugLogs = false)
    {
        this.damage = damage;
        this.speed = speed;
        this.lifetime = lifetime;
        this.direction = direction.normalized;
        this.velocity = this.direction * speed + initialVelocity; // Add vehicle velocity to projectile velocity
        this.pool = pool;
        this.ownerTag = ownerTag;
        this.timer = 0f;
        this.enableDebugLogs = debugLogs;

        if (enableDebugLogs)
        {
            Debug.Log($"[Projectile Init] BulletSpeed: {speed}, InitialVel: {initialVelocity.magnitude:F1}, FinalVel: {this.velocity.magnitude:F1} m/s");
        }

        gameObject.SetActive(true);
    }

    private void Update()
    {
        // Move projectile with combined velocity
        transform.Translate(velocity * Time.deltaTime, Space.World);

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
