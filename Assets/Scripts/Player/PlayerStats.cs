using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] public float sprintStaminaCost = 10f;
    [SerializeField] private float staminaRegenRate = 15f;
    private float currentStamina;

    [Header("Hunger & Thirst")]
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float maxThirst = 100f;
    [SerializeField] private float hungerDecayRate = 1f;  // Reduced from 5 to 1 (100 seconds to deplete)
    [SerializeField] private float thirstDecayRate = 1.5f;  // Reduced from 7 to 1.5 (66 seconds to deplete)
    [SerializeField] private float hungerDamageRate = 2f;  // Reduced from 5 to 2
    [SerializeField] private float thirstDamageRate = 3f;  // Reduced from 5 to 3
    private float currentHunger;
    private float currentThirst;

    // Events
    public System.Action OnDeath;
    public System.Action<float, float> OnHealthChanged;
    public System.Action<float, float> OnStaminaChanged;
    public System.Action<float, float> OnHungerChanged;
    public System.Action<float, float> OnThirstChanged;

    private void Start()
    {
        // Initialize stats
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        currentHunger = maxHunger;
        currentThirst = maxThirst;

        // Trigger initial events for UI initialization
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
        OnThirstChanged?.Invoke(currentThirst, maxThirst);
    }

    private void Update()
    {
        // Debug keys
        HandleDebugKeys();

        // Stamina regeneration
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        // Hunger and thirst decay
        currentHunger -= hungerDecayRate * Time.deltaTime;
        currentThirst -= thirstDecayRate * Time.deltaTime;

        currentHunger = Mathf.Max(currentHunger, 0);
        currentThirst = Mathf.Max(currentThirst, 0);

        OnHungerChanged?.Invoke(currentHunger, maxHunger);
        OnThirstChanged?.Invoke(currentThirst, maxThirst);

        // Damage from hunger/thirst
        if (currentHunger <= 0)
        {
            TakeDamage(hungerDamageRate * Time.deltaTime);
        }

        if (currentThirst <= 0)
        {
            TakeDamage(thirstDamageRate * Time.deltaTime);
        }
    }

    private void HandleDebugKeys()
    {
        // F3: Restore all player stats (health, stamina, hunger, thirst)
        if (Input.GetKeyDown(KeyCode.F3))
        {
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            currentHunger = maxHunger;
            currentThirst = maxThirst;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            OnHungerChanged?.Invoke(currentHunger, maxHunger);
            OnThirstChanged?.Invoke(currentThirst, maxThirst);

            Debug.Log("[Debug] All player stats restored! (F3)");
        }
    }

    public void TakeDamage(float damage)
    {
        PlayerController player = GetComponent<PlayerController>();
        if (player != null && player.IsInvincible())
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void ConsumeStamina(float amount)
    {
        currentStamina -= amount;
        currentStamina = Mathf.Max(currentStamina, 0);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public void RestoreStamina(float amount)
    {
        currentStamina += amount;
        currentStamina = Mathf.Min(currentStamina, maxStamina);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public void RestoreHunger(float amount)
    {
        currentHunger += amount;
        currentHunger = Mathf.Min(currentHunger, maxHunger);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }

    public void RestoreThirst(float amount)
    {
        currentThirst += amount;
        currentThirst = Mathf.Min(currentThirst, maxThirst);
        OnThirstChanged?.Invoke(currentThirst, maxThirst);
    }

    private void Die()
    {
        Debug.Log("Player died!");
        OnDeath?.Invoke();

        // Game over logic here
        //Time.timeScale = 0;
    }

    // Getters
    public float GetHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public float GetStamina() => currentStamina;
    public float GetMaxStamina() => maxStamina;
    public float GetHunger() => currentHunger;
    public float GetMaxHunger() => maxHunger;
    public float GetThirst() => currentThirst;
    public float GetMaxThirst() => maxThirst;
}
