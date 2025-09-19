using UnityEngine;

/// <summary>
/// Триггер для быстрой смены скина игрока
/// Работает с Collider триггерами (без Rigidbody)
/// </summary>
public class SkinChangeTrigger : MonoBehaviour
{
    [Header("Skin Settings")]
    [SerializeField] private string targetSkinId = ""; // ID скина для применения
    [SerializeField] private bool autoUnlock = true;   // Автоматически разблокировать скин
    [SerializeField] private bool oneTimeUse = true;   // Одноразовое использование
    [SerializeField] private bool requirePlayerTag = true; // Проверять тег игрока
    [SerializeField] private string playerTag = "Player";  // Тег игрока

    [Header("Conditions")]
    [SerializeField] private bool requireQuestCompleted = false; // Требовать выполнения квеста
    [SerializeField] private string requiredQuestId = "";
    [SerializeField] private bool requireLevel = false; // Требовать уровень
    [SerializeField] private int requiredLevel = 1;

    [Header("Effects")]
    [SerializeField] private bool playEffect = true;
    [SerializeField] private ParticleSystem changeEffect;
    [SerializeField] private AudioClip changeSound;
    [SerializeField] private float effectDuration = 1f;

    [Header("UI Feedback")]
    [SerializeField] private bool showNotification = true;
    [SerializeField] private string successMessage = "New skin equipped!";
    [SerializeField] private string failMessage = "Cannot equip this skin!";

    private bool isUsed = false;
    private Collider triggerCollider;
    private AudioSource audioSource;

    #region Unity Lifecycle

    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Срабатывает при входе в триггер
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"<Color=Green> >>> OnTriggerEnter </Color> {other.gameObject.tag}");
        if (CanTrigger(other))
        {
            ChangePlayerSkin(other.gameObject);

            if (oneTimeUse)
            {
                DeactivateTrigger();
            }
        }
    }


    /// <summary>
    /// Срабатывает при выходе из триггера (опционально)
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // Можно добавить логику при выходе из триггера
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Инициализация триггера
    /// </summary>
    private void Initialize()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true; // Убедимся что это триггер
        }

        audioSource = GetComponent<AudioSource>();

        // Отключаем коллайдер если нет скина
        if (string.IsNullOrEmpty(targetSkinId))
        {
            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }
            //Debug.LogWarning($"[SkinChangeTrigger] No skin ID set on {gameObject.name}");
        }

        //Debug.Log($"[SkinChangeTrigger] Initialized on {gameObject.name}");
    }

    #endregion

    #region Trigger Logic

    /// <summary>
    /// Проверка возможности срабатывания триггера
    /// </summary>
    private bool CanTrigger(Collider other)
    {
        // Проверка одноразового использования
        if (isUsed && oneTimeUse)
        {
            //Debug.Log($"[SkinChangeTrigger] Trigger {gameObject.name} already used");
            return false;
        }

        // Проверка тега игрока
        if (requirePlayerTag && !other.CompareTag(playerTag))
        {
            //Debug.Log($"[SkinChangeTrigger] Object {other.name} is not player");
            return false;
        }

        // Проверка наличия ID скина
        if (string.IsNullOrEmpty(targetSkinId))
        {
            //Debug.LogWarning($"[SkinChangeTrigger] No skin ID set");
            return false;
        }

        // Проверка наличия SkinManager
        if (!SkinManager.HasInstance)
        {
            //Debug.LogWarning($"[SkinChangeTrigger] SkinManager not found");
            return false;
        }

        // Проверяем дополнительные условия
        if (requireQuestCompleted && !IsQuestCompleted())
        {
            //Debug.Log($"[SkinChangeTrigger] Quest {requiredQuestId} not completed");
            return false;
        }

        if (requireLevel && !HasRequiredLevel())
        {
            //Debug.Log($"[SkinChangeTrigger] Level {requiredLevel} required");
            return false;
        }

        //Debug.Log($"[SkinChangeTrigger] Trigger conditions met for {other.name}");
        return true;
    }

    /// <summary>
    /// Смена скина игрока
    /// </summary>
    private void ChangePlayerSkin(GameObject player)
    {
        var skinManager = SkinManager.Instance;
        if (skinManager == null) return;

        // Проверяем существует ли скин
        var skinData = skinManager.GetSkin(targetSkinId);
        if (skinData == null)
        {
            //Debug.LogWarning($"[SkinChangeTrigger] Skin {targetSkinId} not found");
            ShowFeedback(false);
            return;
        }

        // Разблокируем скин если нужно
        if (autoUnlock && !skinData.isUnlocked)
        {
            bool unlocked = skinManager.UnlockSkin(targetSkinId);
            //Debug.Log($"[SkinChangeTrigger] Skin {targetSkinId} unlocked: {unlocked}");
        }

        // Экипируем скин
        bool success = skinManager.EquipSkin(targetSkinId);

        if (success)
        {
            PlayEffects();
            ShowFeedback(true);
            //Debug.Log($"[SkinChangeTrigger] Player skin changed to {skinData.skinName}");
        }
        else
        {
            ShowFeedback(false);
            //Debug.Log($"[SkinChangeTrigger] Failed to change player skin to {skinData.skinName}");
        }
    }

    /// <summary>
    /// Проверка выполнения квеста
    /// </summary>
    private bool IsQuestCompleted()
    {
        if (string.IsNullOrEmpty(requiredQuestId)) return true;

        // Здесь должна быть интеграция с вашей системой квестов
        // Например: return QuestManager.Instance.IsQuestCompleted(requiredQuestId);

        // Для примера используем PlayerPrefs
        return PlayerPrefs.GetInt($"Quest_{requiredQuestId}_Completed", 0) == 1;
    }

    /// <summary>
    /// Проверка уровня игрока
    /// </summary>
    private bool HasRequiredLevel()
    {
        // Здесь должна быть интеграция с вашей системой уровней
        // Например: return PlayerStats.Instance.Level >= requiredLevel;

        // Для примера используем PlayerPrefs
        int currentLevel = PlayerPrefs.GetInt("PlayerLevel", 1);
        return currentLevel >= requiredLevel;
    }

    /// <summary>
    /// Деактивация триггера
    /// </summary>
    private void DeactivateTrigger()
    {
        isUsed = true;

        // Отключаем визуальные элементы
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }

        // Отключаем коллайдер
        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }

        // Отключаем свет и другие эффекты
        var light = GetComponent<Light>();
        if (light != null)
        {
            light.enabled = false;
        }

        //Debug.Log($"[SkinChangeTrigger] Trigger {gameObject.name} deactivated");
    }

    #endregion

    #region Effects

    /// <summary>
    /// Проигрывание эффектов
    /// </summary>
    private void PlayEffects()
    {
        if (!playEffect) return;

        // Эффект частиц
        if (changeEffect != null)
        {
            changeEffect.Play();
            //Debug.Log("[SkinChangeTrigger] Particle effect played");
        }

        // Звук
        if (changeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(changeSound);
            //Debug.Log("[SkinChangeTrigger] Sound effect played");
        }

        // Можно добавить другие эффекты
    }

    #endregion

    #region Feedback

    /// <summary>
    /// Показ обратной связи
    /// </summary>
    private void ShowFeedback(bool success)
    {
        if (!showNotification) return;

        string message = success ? successMessage : failMessage;
        // Здесь можно интегрировать с вашей системой уведомлений
        // Например: NotificationManager.Instance.ShowMessage(message);

        //Debug.Log($"[SkinChangeTrigger] Feedback: {message}");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Установка ID скина
    /// </summary>
    public void SetTargetSkinId(string skinId)
    {
        targetSkinId = skinId;

        // Реактивируем триггер если он был отключен
        if (isUsed && triggerCollider != null)
        {
            isUsed = false;
            triggerCollider.enabled = true;

            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }

        //Debug.Log($"[SkinChangeTrigger] Target skin ID set to {skinId}");
    }

    /// <summary>
    /// Ручная активация триггера
    /// </summary>
    public void TriggerManually(GameObject player = null)
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag(playerTag);
        }

        if (player != null)
        {
            ChangePlayerSkin(player);
        }
    }

    /// <summary>
    /// Сброс триггера
    /// </summary>
    public void ResetTrigger()
    {
        isUsed = false;

        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }

        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
        }

        //Debug.Log($"[SkinChangeTrigger] Trigger {gameObject.name} reset");
    }

    /// <summary>
    /// Проверка использован ли триггер
    /// </summary>
    public bool IsUsed()
    {
        return isUsed;
    }

    #endregion
}