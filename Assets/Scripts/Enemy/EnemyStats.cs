using UnityEngine;

public class EnemyStats : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float moveSpeed = 3f;

    private float currentHealth;

    public System.Action OnDeath;
    public System.Action OnDamaged;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"Enemy took {damage} damage. Health: {currentHealth}/{maxHealth}");

        // Notify AI that we've been damaged
        OnDamaged?.Invoke();

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
        Debug.Log("Enemy died!");
        OnDeath?.Invoke();
    }

    // Getters
    public float GetHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetAttackDamage() => attackDamage;
    public float GetMoveSpeed() => moveSpeed;
}
