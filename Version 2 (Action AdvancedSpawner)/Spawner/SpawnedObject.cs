using UnityEngine;
using System;

/// <summary>
/// Компонент для объектов, созданных через спавнер
/// Генерирует события для взаимодействия с другими системами
/// </summary>
public class SpawnedObject : MonoBehaviour
{
    /// <summary>
    /// Типы событий для заспавненного объекта
    /// </summary>
    public enum TriggerType
    {
        PlayerCollected,
        BotCollected,
        Timeout,
        Destroyed,
        Custom
    }

    /// <summary>
    /// Событие срабатывания триггера
    /// </summary>
    public static event Action<SpawnedObject, TriggerType, object> OnTriggerActivated;

    [Header("Object Settings")]
    [SerializeField] private string objectType = "Default";
    [SerializeField] private float lifetime = 30f;
    [SerializeField] private bool destroyOnPlayerTrigger = true;
    [SerializeField] private bool destroyOnBotTrigger = true;

    private float spawnTime;
    private bool isDestroyed = false;
    private Collider objectCollider;
    private Renderer objectRenderer;

    #region Unity Lifecycle

    private void Awake()
    {
        // Кэшируем компоненты при создании
        CacheComponents();
    }

    private void OnEnable()
    {
        // При активации объекта включаем компоненты
        EnableComponents();
        isDestroyed = false;
        spawnTime = Time.time;
    }

    private void OnDisable()
    {
        // При деактивации очищаем состояние
        isDestroyed = false;
    }

    private void Update()
    {
        if (isActiveAndEnabled && !isDestroyed)
        {
            CheckLifetime();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDestroyed || !isActiveAndEnabled) return;

        if (other.CompareTag("Player"))
        {
            ActivateTrigger(TriggerType.PlayerCollected, other.gameObject);
        }
        else if (other.CompareTag("Bot") || other.CompareTag("Enemy"))
        {
            ActivateTrigger(TriggerType.BotCollected, other.gameObject);
        }
    }

    #endregion

    #region Component Management

    /// <summary>
    /// Кэширование компонентов
    /// </summary>
    private void CacheComponents()
    {
        objectCollider = GetComponent<Collider>();
        objectRenderer = GetComponent<Renderer>();
    }

    /// <summary>
    /// Включение компонентов при активации
    /// </summary>
    private void EnableComponents()
    {
        if (objectCollider != null) objectCollider.enabled = true;
        if (objectRenderer != null) objectRenderer.enabled = true;
    }

    /// <summary>
    /// Отключение компонентов при деактивации
    /// </summary>
    private void DisableComponents()
    {
        if (objectCollider != null) objectCollider.enabled = false;
        if (objectRenderer != null) objectRenderer.enabled = false;
    }

    #endregion

    #region Trigger Management

    /// <summary>
    /// Активация триггера с указанным типом
    /// </summary>
    public void ActivateTrigger(TriggerType triggerType, object triggerData = null)
    {
        if (isDestroyed || !isActiveAndEnabled) return;

        OnTriggerActivated?.Invoke(this, triggerType, triggerData);

        bool shouldDestroy = false;

        switch (triggerType)
        {
            case TriggerType.PlayerCollected:
                shouldDestroy = destroyOnPlayerTrigger;
                break;
            case TriggerType.BotCollected:
                shouldDestroy = destroyOnBotTrigger;
                break;
            case TriggerType.Timeout:
                shouldDestroy = true;
                break;
            case TriggerType.Destroyed:
                shouldDestroy = true;
                break;
        }

        if (shouldDestroy)
        {
            ReturnToPool();
        }
    }

    /// <summary>
    /// Возврат объекта в пул
    /// </summary>
    public void ReturnToPool()
    {
        if (isDestroyed) return;

        isDestroyed = true;

        // Отключаем компоненты перед возвратом в пул
        DisableComponents();

        if (ObjectPoolManager.HasInstance)
        {
            ObjectPoolManager.Instance.ReturnObjectToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Принудительное уничтожение объекта
    /// </summary>
    public void ForceDestroy()
    {
        if (isDestroyed) return;

        isDestroyed = true;
        ActivateTrigger(TriggerType.Destroyed);
    }

    #endregion

    #region Lifetime Management

    /// <summary>
    /// Проверка времени жизни объекта
    /// </summary>
    private void CheckLifetime()
    {
        if (lifetime > 0f && Time.time - spawnTime >= lifetime)
        {
            ActivateTrigger(TriggerType.Timeout);
        }
    }

    #endregion

    #region Public Properties

    public string ObjectType => objectType;
    public float SpawnTime => spawnTime;
    public bool IsDestroyed => isDestroyed;

    #endregion
}