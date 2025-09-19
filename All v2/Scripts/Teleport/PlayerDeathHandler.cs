using UnityEngine;

/// <summary>
/// Обработчик смерти игрока с интеграцией телепортации
/// </summary>
public class PlayerDeathHandler : MonoBehaviour
{
    [Header("Death Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float deathDelay = 1f;
    [SerializeField] private bool useTeleportRespawn = true;

    [Header("Health System Integration")]
    [SerializeField] private MonoBehaviour healthSystem;

    [Header("Death Effects")]
    [SerializeField] private bool disableControlsOnDeath = true;
    [SerializeField] private bool playDeathAnimation = true;
    [SerializeField] private AudioClip deathSound;

    private bool isDead = false;
    private AudioSource audioSource;
    private ThirdPersonController playerController;

    #region Unity Lifecycle

    private void Start()
    {
        SubscribeToEvents();
        audioSource = GetComponent<AudioSource>();
        playerController = GetComponent<ThirdPersonController>();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    #endregion

    #region Event Management

    private void SubscribeToEvents()
    {
        // Убираем подписку на TeleportManager.OnPlayerDeath
        // Вместо этого используем прямой вызов из TeleportManager
    }

    private void UnsubscribeFromEvents()
    {
        // Пусто, так как мы убрали подписку
    }

    #endregion

    #region Death Handling

    private void HandlePlayerDeath()
    {
        if (isDead) return; // Защита от повторного вызова

        isDead = true;

        // Отключаем управление игроком
        if (disableControlsOnDeath && playerController != null)
        {
            playerController.enabled = false;
        }

        // Проигрываем звук смерти
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        if (playDeathAnimation)
        {
            PlayDeathAnimation();
        }

        Debug.Log("[PlayerDeathHandler] Player died");

        if (useTeleportRespawn)
        {
            Invoke(nameof(RespawnPlayer), deathDelay);
        }
        else
        {
            Invoke(nameof(ResetPlayer), deathDelay);
        }
    }

    private void RespawnPlayer()
    {
        if (TeleportManager.HasInstance)
        {
            TeleportManager.Instance.RespawnPlayer();
        }
        else
        {
            ResetPlayer();
        }
    }

    private void ResetPlayer()
    {
        RestorePlayerControl();
    }

    private void PlayDeathAnimation()
    {
        var animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Принудительная смерть игрока
    /// </summary>
    public void KillPlayer()
    {
        if (isDead) return;

        HandlePlayerDeath();
    }

    public bool IsPlayerAlive()
    {
        return !isDead;
    }

    public void ManualRespawn()
    {
        if (isDead)
        {
            CancelInvoke();
            RespawnPlayer();
        }
    }

    /// <summary>
    /// Восстановление управления игроком (вызывается после респауна)
    /// </summary>
    public void RestorePlayerControl()
    {
        // Восстанавливаем здоровье при респауне
        var playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.Respawn(); // Используем специальный метод для респауна
        }

        // Восстанавливаем управление
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        // Сбрасываем состояние смерти
        isDead = false;
        Debug.Log("[PlayerDeathHandler] Player control restored");
    }

    #endregion
}