using UnityEngine;

/// <summary>
/// Препятствие, которое наносит урон игроку при столкновении
/// Поддерживает как Collider, так и CharacterController
/// </summary>


/// <summary>
/// Интерфейс для объектов, которые могут получать урон
/// </summary>
public interface IDamageable
{
    void TakeDamage(float damage);
}


/// <summary>
/// Препятствие, которое наносит урон игроку при столкновении
/// Поддерживает как Collider, так и CharacterController
/// </summary>
public class DamageObstacle : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private float damageInterval = 1f;
    [SerializeField] private bool continuousDamage = true;
    [SerializeField] private bool destroyOnHit = false;
    [SerializeField] private bool onlyPlayer = true;

    [Header("Damage Type")]
    [SerializeField] private string damageType = "Obstacle";

    [Header("Collision Settings")]
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private bool useTrigger = true;

    [Header("Effects")]
    [SerializeField] private bool playHitEffect = true;
    [SerializeField] private ParticleSystem hitEffect;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private bool shakeCamera = false;
    [SerializeField] private float shakeIntensity = 0.5f;
    [SerializeField] private float shakeDuration = 0.2f;

    [Header("Visual Feedback")]
    [SerializeField] private bool changeColorOnHit = true;
    [SerializeField] private Material hitMaterial;
    [SerializeField] private float hitDuration = 0.3f;

    private Material originalMaterial;
    private Renderer obstacleRenderer;
    private AudioSource audioSource;
    private Collider obstacleCollider;
    private bool isPlayerInContact = false;
    private float lastDamageTime = 0f;
    private Material currentHitMaterial;
    private GameObject playerInTrigger;

    #region Unity Lifecycle

    private void Start()
    {
        InitializeComponents();

        // Подписываемся на событие респауна игрока
        TeleportManager.OnPlayerRespawned += OnPlayerRespawned;
    }

    private void OnDestroy()
    {
        // Отписываемся от события
        TeleportManager.OnPlayerRespawned -= OnPlayerRespawned;
    }

    private void Update()
    {
        // Обработка непрерывного урона
        if (continuousDamage && isPlayerInContact && playerInTrigger != null && Time.time - lastDamageTime >= damageInterval)
        {
            // Проверяем, жив ли игрок перед нанесением урона
            var playerHealth = playerInTrigger.GetComponent<PlayerHealth>();
            if (playerHealth != null && playerHealth.IsAlive)
            {
                DealDamageToTarget(playerInTrigger);
                lastDamageTime = Time.time;
            }
            else
            {
                // Если игрок мертв, прекращаем наносить урон
                isPlayerInContact = false;
                playerInTrigger = null;
            }
        }
    }

    #endregion

    #region Event Handling

    /// <summary>
    /// Обработка события респауна игрока
    /// </summary>
    private void OnPlayerRespawned()
    {
        // Сбрасываем состояние контакта при респауне игрока
        isPlayerInContact = false;
        playerInTrigger = null;
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        obstacleRenderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();
        obstacleCollider = GetComponent<Collider>();

        if (obstacleRenderer != null && obstacleRenderer.material != null)
        {
            originalMaterial = obstacleRenderer.material;
        }

        if (obstacleCollider != null)
        {
            obstacleCollider.isTrigger = useTrigger;
        }

        if (hitMaterial != null && obstacleRenderer != null)
        {
            currentHitMaterial = new Material(hitMaterial);
        }
    }

    #endregion

    #region Collision Handling

    private void OnTriggerEnter(Collider other)
    {
        if (useTrigger && CheckTarget(other))
        {
            OnTargetEnter(other.gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (useTrigger && CheckTarget(other))
        {
            // Для одноразового урона
            if (!continuousDamage && Time.time - lastDamageTime >= damageInterval)
            {
                var playerHealth = other.gameObject.GetComponent<PlayerHealth>();
                if (playerHealth != null && playerHealth.IsAlive)
                {
                    DealDamageToTarget(other.gameObject);
                    lastDamageTime = Time.time;
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (useTrigger && CheckTarget(other))
        {
            OnTargetExit(other.gameObject);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!useTrigger && CheckTarget(hit.gameObject))
        {
            OnTargetEnter(hit.gameObject);

            if (!continuousDamage)
            {
                var playerHealth = hit.gameObject.GetComponent<PlayerHealth>();
                if (playerHealth != null && playerHealth.IsAlive)
                {
                    DealDamageToTarget(hit.gameObject);
                }
            }
        }
    }

    private bool CheckTarget(GameObject other)
    {
        if (onlyPlayer)
        {
            return other.CompareTag(targetTag);
        }
        return true;
    }

    private bool CheckTarget(Collider other)
    {
        if (onlyPlayer)
        {
            return other.CompareTag(targetTag);
        }
        return true;
    }

    private void OnTargetEnter(GameObject target)
    {
        if (onlyPlayer && target.CompareTag(targetTag))
        {
            // Проверяем, жив ли игрок
            var playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null && playerHealth.IsAlive)
            {
                isPlayerInContact = true;
                playerInTrigger = target;
                lastDamageTime = Time.time - damageInterval;

                DealDamageToTarget(target);
                lastDamageTime = Time.time;
            }
        }
        else if (!onlyPlayer)
        {
            DealDamageToTarget(target);
        }

        PlayHitEffects();

        if (destroyOnHit)
        {
            DestroyObstacle();
        }
    }

    private void OnTargetExit(GameObject target)
    {
        if (onlyPlayer && target.CompareTag(targetTag))
        {
            isPlayerInContact = false;
            playerInTrigger = null;
        }
    }

    #endregion

    #region Damage Handling

    private void DealDamageToTarget(GameObject target)
    {
        // Проверяем, жив ли цель
        var playerHealth = target.GetComponent<PlayerHealth>();
        if (playerHealth != null && !playerHealth.IsAlive)
        {
            // Не наносим урон мертвому игроку
            return;
        }

        var healthSystem = target.GetComponent<PlayerHealth>();

        if (healthSystem != null)
        {
            healthSystem.TakeDamage(damageAmount);

            DamageSystem.DamageEvent?.Invoke(new DamageEventData
            {
                target = target,
                attacker = gameObject,
                damageAmount = damageAmount,
                damageType = damageType,
                position = target.transform.position
            });
        }
        else
        {
            var damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damageAmount);
            }
            else
            {
                Debug.Log($"[DamageObstacle] Target {target.name} has no health system");
            }
        }
    }

    #endregion

    #region Effects

    private void PlayHitEffects()
    {
        if (playHitEffect && hitEffect != null)
        {
            hitEffect.Play();
        }

        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        if (shakeCamera && Camera.main != null)
        {
            // CameraShake.Instance.Shake(shakeIntensity, shakeDuration);
        }

        if (changeColorOnHit && obstacleRenderer != null && currentHitMaterial != null)
        {
            obstacleRenderer.material = currentHitMaterial;
            Invoke(nameof(ResetMaterial), hitDuration);
        }
    }

    private void ResetMaterial()
    {
        if (obstacleRenderer != null && originalMaterial != null)
        {
            obstacleRenderer.material = originalMaterial;
        }
    }

    #endregion

    #region Destruction

    private void DestroyObstacle()
    {
        if (hitEffect != null)
        {
            var effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, effect.main.duration);
        }

        Destroy(gameObject);
    }

    #endregion

    #region Public Methods

    public void SetDamage(float damage)
    {
        damageAmount = Mathf.Max(0, damage);
    }

    public void SetDamageInterval(float interval)
    {
        damageInterval = Mathf.Max(0, interval);
    }

    public void SetContinuousDamage(bool continuous)
    {
        continuousDamage = continuous;
    }

    public void ForceDamageTarget(GameObject target)
    {
        // Проверяем, жив ли цель перед нанесением урона
        var playerHealth = target.GetComponent<PlayerHealth>();
        if (playerHealth != null && !playerHealth.IsAlive)
        {
            return;
        }

        DealDamageToTarget(target);
        PlayHitEffects();
    }

    #endregion
}

