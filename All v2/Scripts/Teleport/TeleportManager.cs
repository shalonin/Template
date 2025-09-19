using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Менеджер телепортации для универсального перемещения игрока
/// Поддерживает перемещение в точку, между сценами и на точки респауна
/// </summary>
public class TeleportManager : Singleton<TeleportManager>
{
    /// <summary>
    /// Типы телепортации
    /// </summary>
    public enum TeleportType
    {
        Position,      // Перемещение в точку на текущей сцене
        Scene,         // Загрузка новой сцены
        Respawn,       // Перемещение на точку респауна
        Custom         // Кастомная телепортация
    }

    /// <summary>
    /// Данные телепортации
    /// </summary>
    public class TeleportData
    {
        public TeleportType type;
        public Vector3 targetPosition;
        public string targetScene;
        public Transform respawnPoint;
        public object customData;
        public bool useFade = true;
        public float fadeDuration = 0.5f;
        public Action onTeleportComplete;
        public Action onTeleportStart;

        public TeleportData(TeleportType teleportType)
        {
            type = teleportType;
        }
    }

    /// <summary>
    /// Событие начала телепортации
    /// </summary>
    public static event Action<TeleportData> OnTeleportStart;

    /// <summary>
    /// Событие завершения телепортации
    /// </summary>
    public static event Action<TeleportData> OnTeleportComplete;

    /// <summary>
    /// Событие смерти игрока (для респауна)
    /// </summary>
    public static event Action OnPlayerDeath;

    /// <summary>
    /// Событие завершения респауна
    /// </summary>
    public static event System.Action OnPlayerRespawned;

    [Header("Player Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float teleportDelay = 0.1f;

    [Header("Respawn Settings")]
    [SerializeField] private Transform defaultRespawnPoint;
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private bool autoRespawn = true;

    [Header("Fade Settings")]
    [SerializeField] private bool useFadeEffect = true;
    [SerializeField] private float defaultFadeDuration = 0.5f;

    private GameObject player;
    private bool isTeleporting = false;
    private Transform lastCheckpoint;

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    private void Start()
    {
        FindPlayer();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Инициализация менеджера телепортации
    /// </summary>
    private void Initialize()
    {
        lastCheckpoint = defaultRespawnPoint;
    }

    /// <summary>
    /// Поиск игрока на сцене
    /// </summary>
    private void FindPlayer()
    {
        player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            Debug.LogWarning("[TeleportManager] Player not found. Make sure player has tag: " + playerTag);
        }
    }

    /// <summary>
    /// Подписка на события
    /// </summary>
    private void SubscribeToEvents()
    {
        // Здесь можно подписаться на внешние события
    }

    /// <summary>
    /// Отписка от событий
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        // Здесь можно отписаться от внешних событий
    }

    #endregion

    #region Teleport Methods

    /// <summary>
    /// Телепортация в точку на текущей сцене
    /// </summary>
    public void TeleportToPosition(Vector3 position, bool useFade = true, float fadeDuration = -1f)
    {
        var teleportData = new TeleportData(TeleportType.Position)
        {
            targetPosition = position,
            useFade = useFade,
            fadeDuration = fadeDuration > 0 ? fadeDuration : defaultFadeDuration
        };

        StartCoroutine(ExecuteTeleport(teleportData));
    }

    /// <summary>
    /// Телепортация в новую сцену
    /// </summary>
    public void TeleportToScene(string sceneName, Vector3 spawnPosition = default, bool useFade = true, float fadeDuration = -1f)
    {
        var teleportData = new TeleportData(TeleportType.Scene)
        {
            targetScene = sceneName,
            targetPosition = spawnPosition,
            useFade = useFade,
            fadeDuration = fadeDuration > 0 ? fadeDuration : defaultFadeDuration
        };

        StartCoroutine(ExecuteTeleport(teleportData));
    }

    /// <summary>
    /// Респаун игрока
    /// </summary>
    public void RespawnPlayer(Transform respawnPoint = null, bool useFade = true, float fadeDuration = -1f)
    {
        var targetPoint = respawnPoint ?? lastCheckpoint ?? defaultRespawnPoint;

        if (targetPoint == null)
        {
            Debug.LogWarning("[TeleportManager] No respawn point available");
            return;
        }

        var teleportData = new TeleportData(TeleportType.Respawn)
        {
            respawnPoint = targetPoint,
            targetPosition = targetPoint.position,
            useFade = useFade,
            fadeDuration = fadeDuration > 0 ? fadeDuration : defaultFadeDuration
        };

        StartCoroutine(ExecuteTeleport(teleportData));

        // ВАЖНО: Не вызываем OnPlayerDeath здесь, а только после завершения телепортации
    }

    /// <summary>
    /// Кастомная телепортация
    /// </summary>
    public void TeleportCustom(object customData, Action<TeleportData> customHandler)
    {
        var teleportData = new TeleportData(TeleportType.Custom)
        {
            customData = customData
        };

        customHandler?.Invoke(teleportData);
        OnTeleportComplete?.Invoke(teleportData);
    }

    /// <summary>
    /// Выполнение телепортации
    /// </summary>
    private IEnumerator ExecuteTeleport(TeleportData data)
    {
        if (isTeleporting)
        {
            Debug.LogWarning("[TeleportManager] Teleport already in progress");
            yield break;
        }

        isTeleporting = true;

        // Проверяем наличие игрока
        if (player == null)
        {
            FindPlayer();
            if (player == null)
            {
                Debug.LogError("[TeleportManager] Cannot teleport - player not found");
                isTeleporting = false;
                yield break;
            }
        }

        // Вызываем событие начала телепортации
        data.onTeleportStart?.Invoke();
        OnTeleportStart?.Invoke(data);

        // Эффект затухания если включен
        if (data.useFade && useFadeEffect)
        {
            yield return StartCoroutine(FadeOut(data.fadeDuration));
        }

        // Задержка перед перемещением
        yield return new WaitForSeconds(teleportDelay);

        // Выполняем перемещение в зависимости от типа
        switch (data.type)
        {
            case TeleportType.Position:
                MovePlayerToPosition(data.targetPosition);
                break;

            case TeleportType.Scene:
                yield return StartCoroutine(LoadSceneAndTeleport(data));
                break;

            case TeleportType.Respawn:
                MovePlayerToPosition(data.targetPosition);
                // Вызываем событие смерти ПОСЛЕ завершения телепортации
                StartCoroutine(DelayedPlayerDeathEvent());
                break;

            case TeleportType.Custom:
                // Кастомная логика обрабатывается в TeleportCustom
                break;
        }

        // Эффект появления если был затухание
        if (data.useFade && useFadeEffect)
        {
            yield return StartCoroutine(FadeIn(data.fadeDuration));
        }

        // Завершаем телепортацию
        data.onTeleportComplete?.Invoke();
        OnTeleportComplete?.Invoke(data);

        isTeleporting = false;
    }

    /// <summary>
    /// Отложенное событие смерти игрока (после телепортации)
    /// </summary>
    private IEnumerator DelayedPlayerDeathEvent()
    {
        yield return new WaitForSeconds(teleportDelay + 0.1f);

        // Восстанавливаем управление через PlayerDeathHandler
        if (player != null)
        {
            var deathHandler = player.GetComponent<PlayerDeathHandler>();
            if (deathHandler != null)
            {
                deathHandler.RestorePlayerControl();
            }
        }

        // Вызываем событие респауна
        OnPlayerRespawned?.Invoke();
    }

    /// <summary>
    /// Перемещение игрока в точку
    /// </summary>
    private void MovePlayerToPosition(Vector3 position)
    {
        if (player != null)
        {
            var characterController = player.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
                player.transform.position = position;
                characterController.enabled = true;
            }
            else
            {
                player.transform.position = position;
            }
        }
    }

    /// <summary>
    /// Загрузка сцены и телепортация
    /// </summary>
    private IEnumerator LoadSceneAndTeleport(TeleportData data)
    {
        if (string.IsNullOrEmpty(data.targetScene))
        {
            Debug.LogError("[TeleportManager] Target scene name is empty");
            yield break;
        }

        // Здесь должна быть интеграция с вашей системой загрузки сцен
        // Например, если у вас есть SceneLoader:
        /*
        if (SceneLoader.HasInstance)
        {
            yield return SceneLoader.Instance.LoadSceneAsync(data.targetScene);
            
            // После загрузки сцены находим игрока и перемещаем
            yield return new WaitForSeconds(0.1f); // Небольшая задержка для инициализации
            FindPlayer();
            MovePlayerToPosition(data.targetPosition);
        }
        else
        */
        {
            // Для примера используем стандартную загрузку
            UnityEngine.SceneManagement.SceneManager.LoadScene(data.targetScene);
        }
    }

    #endregion

    #region Fade Effects

    /// <summary>
    /// Эффект затухания
    /// </summary>
    private IEnumerator FadeOut(float duration)
    {
        // Здесь должна быть ваша система эффектов
        // Например, интеграция с UI или пост-процессингом
        Debug.Log($"[TeleportManager] Fading out for {duration} seconds");
        yield return new WaitForSeconds(duration);
    }

    /// <summary>
    /// Эффект появления
    /// </summary>
    private IEnumerator FadeIn(float duration)
    {
        Debug.Log($"[TeleportManager] Fading in for {duration} seconds");
        yield return new WaitForSeconds(duration);
    }

    #endregion

    #region Respawn Management

    /// <summary>
    /// Установка чекпоинта
    /// </summary>
    public void SetCheckpoint(Transform checkpoint)
    {
        if (checkpoint != null)
        {
            lastCheckpoint = checkpoint;
            Debug.Log($"[TeleportManager] Checkpoint set to {checkpoint.name}");
        }
    }

    /// <summary>
    /// Получение текущего чекпоинта
    /// </summary>
    public Transform GetCheckpoint()
    {
        return lastCheckpoint;
    }

    /// <summary>
    /// Смерть игрока с автоматическим респауном
    /// </summary>
    public void PlayerDied()
    {
        if (autoRespawn)
        {
            StartCoroutine(RespawnAfterDelay());
        }
        else
        {
            OnPlayerDeath?.Invoke();
        }
    }

    /// <summary>
    /// Респаун с задержкой
    /// </summary>
    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        RespawnPlayer();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Установка игрока
    /// </summary>
    public void SetPlayer(GameObject newPlayer)
    {
        player = newPlayer;
    }

    /// <summary>
    /// Проверка идет ли телепортация
    /// </summary>
    public bool IsTeleporting()
    {
        return isTeleporting;
    }

    /// <summary>
    /// Отмена текущей телепортации
    /// </summary>
    public void CancelTeleport()
    {
        StopAllCoroutines();
        isTeleporting = false;
    }

    #endregion
}