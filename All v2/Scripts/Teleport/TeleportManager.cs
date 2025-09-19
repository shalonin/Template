using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// �������� ������������ ��� �������������� ����������� ������
/// ������������ ����������� � �����, ����� ������� � �� ����� ��������
/// </summary>
public class TeleportManager : Singleton<TeleportManager>
{
    /// <summary>
    /// ���� ������������
    /// </summary>
    public enum TeleportType
    {
        Position,      // ����������� � ����� �� ������� �����
        Scene,         // �������� ����� �����
        Respawn,       // ����������� �� ����� ��������
        Custom         // ��������� ������������
    }

    /// <summary>
    /// ������ ������������
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
    /// ������� ������ ������������
    /// </summary>
    public static event Action<TeleportData> OnTeleportStart;

    /// <summary>
    /// ������� ���������� ������������
    /// </summary>
    public static event Action<TeleportData> OnTeleportComplete;

    /// <summary>
    /// ������� ������ ������ (��� ��������)
    /// </summary>
    public static event Action OnPlayerDeath;

    /// <summary>
    /// ������� ���������� ��������
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
    /// ������������� ��������� ������������
    /// </summary>
    private void Initialize()
    {
        lastCheckpoint = defaultRespawnPoint;
    }

    /// <summary>
    /// ����� ������ �� �����
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
    /// �������� �� �������
    /// </summary>
    private void SubscribeToEvents()
    {
        // ����� ����� ����������� �� ������� �������
    }

    /// <summary>
    /// ������� �� �������
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        // ����� ����� ���������� �� ������� �������
    }

    #endregion

    #region Teleport Methods

    /// <summary>
    /// ������������ � ����� �� ������� �����
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
    /// ������������ � ����� �����
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
    /// ������� ������
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

        // �����: �� �������� OnPlayerDeath �����, � ������ ����� ���������� ������������
    }

    /// <summary>
    /// ��������� ������������
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
    /// ���������� ������������
    /// </summary>
    private IEnumerator ExecuteTeleport(TeleportData data)
    {
        if (isTeleporting)
        {
            Debug.LogWarning("[TeleportManager] Teleport already in progress");
            yield break;
        }

        isTeleporting = true;

        // ��������� ������� ������
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

        // �������� ������� ������ ������������
        data.onTeleportStart?.Invoke();
        OnTeleportStart?.Invoke(data);

        // ������ ��������� ���� �������
        if (data.useFade && useFadeEffect)
        {
            yield return StartCoroutine(FadeOut(data.fadeDuration));
        }

        // �������� ����� ������������
        yield return new WaitForSeconds(teleportDelay);

        // ��������� ����������� � ����������� �� ����
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
                // �������� ������� ������ ����� ���������� ������������
                StartCoroutine(DelayedPlayerDeathEvent());
                break;

            case TeleportType.Custom:
                // ��������� ������ �������������� � TeleportCustom
                break;
        }

        // ������ ��������� ���� ��� ���������
        if (data.useFade && useFadeEffect)
        {
            yield return StartCoroutine(FadeIn(data.fadeDuration));
        }

        // ��������� ������������
        data.onTeleportComplete?.Invoke();
        OnTeleportComplete?.Invoke(data);

        isTeleporting = false;
    }

    /// <summary>
    /// ���������� ������� ������ ������ (����� ������������)
    /// </summary>
    private IEnumerator DelayedPlayerDeathEvent()
    {
        yield return new WaitForSeconds(teleportDelay + 0.1f);

        // ��������������� ���������� ����� PlayerDeathHandler
        if (player != null)
        {
            var deathHandler = player.GetComponent<PlayerDeathHandler>();
            if (deathHandler != null)
            {
                deathHandler.RestorePlayerControl();
            }
        }

        // �������� ������� ��������
        OnPlayerRespawned?.Invoke();
    }

    /// <summary>
    /// ����������� ������ � �����
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
    /// �������� ����� � ������������
    /// </summary>
    private IEnumerator LoadSceneAndTeleport(TeleportData data)
    {
        if (string.IsNullOrEmpty(data.targetScene))
        {
            Debug.LogError("[TeleportManager] Target scene name is empty");
            yield break;
        }

        // ����� ������ ���� ���������� � ����� �������� �������� ����
        // ��������, ���� � ��� ���� SceneLoader:
        /*
        if (SceneLoader.HasInstance)
        {
            yield return SceneLoader.Instance.LoadSceneAsync(data.targetScene);
            
            // ����� �������� ����� ������� ������ � ����������
            yield return new WaitForSeconds(0.1f); // ��������� �������� ��� �������������
            FindPlayer();
            MovePlayerToPosition(data.targetPosition);
        }
        else
        */
        {
            // ��� ������� ���������� ����������� ��������
            UnityEngine.SceneManagement.SceneManager.LoadScene(data.targetScene);
        }
    }

    #endregion

    #region Fade Effects

    /// <summary>
    /// ������ ���������
    /// </summary>
    private IEnumerator FadeOut(float duration)
    {
        // ����� ������ ���� ���� ������� ��������
        // ��������, ���������� � UI ��� ����-������������
        Debug.Log($"[TeleportManager] Fading out for {duration} seconds");
        yield return new WaitForSeconds(duration);
    }

    /// <summary>
    /// ������ ���������
    /// </summary>
    private IEnumerator FadeIn(float duration)
    {
        Debug.Log($"[TeleportManager] Fading in for {duration} seconds");
        yield return new WaitForSeconds(duration);
    }

    #endregion

    #region Respawn Management

    /// <summary>
    /// ��������� ���������
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
    /// ��������� �������� ���������
    /// </summary>
    public Transform GetCheckpoint()
    {
        return lastCheckpoint;
    }

    /// <summary>
    /// ������ ������ � �������������� ���������
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
    /// ������� � ���������
    /// </summary>
    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        RespawnPlayer();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// ��������� ������
    /// </summary>
    public void SetPlayer(GameObject newPlayer)
    {
        player = newPlayer;
    }

    /// <summary>
    /// �������� ���� �� ������������
    /// </summary>
    public bool IsTeleporting()
    {
        return isTeleporting;
    }

    /// <summary>
    /// ������ ������� ������������
    /// </summary>
    public void CancelTeleport()
    {
        StopAllCoroutines();
        isTeleporting = false;
    }

    #endregion
}