using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Система здоровья игрока
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private bool invincible = false;

    [Header("Damage Effects")]
    [SerializeField] private bool flashOnDamage = true;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private Color damageFlashColor = Color.red;

    [Header("Events")]
    [SerializeField] private UnityEvent<float> onHealthChanged;
    [SerializeField] private UnityEvent<float> onDamageTaken;
    [SerializeField] private UnityEvent onPlayerDied;
    [SerializeField] private UnityEvent onPlayerHealed;

    private Renderer playerRenderer;
    private Material originalMaterial;
    private PlayerDeathHandler deathHandler;
    private bool isDead = false;

    #region Unity Lifecycle

    private void Start()
    {
        Initialize();
        SubscribeToEvents();
        Debug.Log($"[PlayerHealth] Initialized with {currentHealth} HP");
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    #endregion

    #region Initialization

    private void Initialize()
    {
        currentHealth = maxHealth; // Всегда начинаем с максимального здоровья
        deathHandler = GetComponent<PlayerDeathHandler>();

        playerRenderer = GetComponent<Renderer>();
        if (playerRenderer != null)
        {
            originalMaterial = playerRenderer.material;
        }
    }

    #endregion

    #region Event Management

    private void SubscribeToEvents()
    {
        DamageSystem.DamageEvent += OnDamageEvent;
    }

    private void UnsubscribeFromEvents()
    {
        DamageSystem.DamageEvent -= OnDamageEvent;
    }

    private void OnDamageEvent(DamageEventData damageData)
    {
        if (damageData.target == gameObject)
        {
            Debug.Log($"[PlayerHealth] Received damage event: {damageData.damageAmount} from {damageData.damageType}");
            TakeDamage(damageData.damageAmount);
        }
    }

    #endregion

    #region Damage Handling

    public void TakeDamage(float damage)
    {
        if (isDead || invincible)
        {
            Debug.Log($"[PlayerHealth] Damage ignored - Dead: {isDead}, Invincible: {invincible}");
            return;
        }

        float actualDamage = Mathf.Max(0, damage);
        currentHealth -= actualDamage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"[PlayerHealth] Took {actualDamage} damage. Health: {currentHealth}/{maxHealth}");

        onDamageTaken?.Invoke(actualDamage);
        onHealthChanged?.Invoke(currentHealth);

        if (flashOnDamage && playerRenderer != null)
        {
            StartCoroutine(FlashEffect());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator FlashEffect()
    {
        if (playerRenderer == null) yield break;

        Material flashMaterial = new Material(originalMaterial);
        flashMaterial.color = damageFlashColor;
        playerRenderer.material = flashMaterial;

        yield return new WaitForSeconds(flashDuration);

        if (playerRenderer != null)
        {
            playerRenderer.material = originalMaterial;
        }

        Destroy(flashMaterial);
    }

    private void Die()
    {
        if (isDead) return; // Защита от повторного вызова

        isDead = true;
        onPlayerDied?.Invoke();

        Debug.Log("[PlayerHealth] Player died");

        if (deathHandler != null)
        {
            deathHandler.KillPlayer();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    #endregion

    #region Healing

    public void Heal(float amount)
    {
        if (isDead) return; // Не лечим мертвого игрока

        float actualHeal = Mathf.Max(0, amount);
        currentHealth = Mathf.Min(maxHealth, currentHealth + actualHeal);

        onPlayerHealed?.Invoke();
        onHealthChanged?.Invoke(currentHealth);

        Debug.Log($"[PlayerHealth] Healed {actualHeal} HP. Health: {currentHealth}/{maxHealth}");
    }

    public void FullHeal()
    {
        if (isDead) return; // Не лечим мертвого игрока

        float healAmount = maxHealth - currentHealth;
        currentHealth = maxHealth;

        onPlayerHealed?.Invoke();
        onHealthChanged?.Invoke(currentHealth);

        Debug.Log($"[PlayerHealth] Fully healed. Health: {currentHealth}/{maxHealth}");
    }

    #endregion

    #region Public Methods for Respawn

    /// <summary>
    /// Полное восстановление здоровья при респауне
    /// </summary>
    public void Respawn()
    {
        currentHealth = maxHealth; // Восстанавливаем здоровье
        isDead = false; // Сбрасываем состояние смерти
        invincible = false; // Сбрасываем неуязвимость

        onHealthChanged?.Invoke(currentHealth);

        Debug.Log($"[PlayerHealth] Player respawned with {currentHealth} HP");
    }

    #endregion

    #region Public Properties

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    public bool IsAlive => !isDead && currentHealth > 0;
    public bool IsDead => isDead;

    public void SetInvincible(bool invincibleState)
    {
        invincible = invincibleState;
    }

    #endregion
}
