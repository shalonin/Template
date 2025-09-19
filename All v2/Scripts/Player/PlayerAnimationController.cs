using UnityEngine;

/// <summary>
/// Контроллер анимаций игрока с отладкой
/// </summary>
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private string playerModelTag = "PlayerModel";

    [Header("Animation Parameters")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string isGroundedParameter = "IsGrounded";
    [SerializeField] private string jumpParameter = "Jump";
    [SerializeField] private string isDeadParameter = "IsDead";
    [SerializeField] private string isRunningParameter = "IsRunning";

    [Header("Movement Detection")]
    [SerializeField] private float speedThreshold = 0.1f;
    [SerializeField] private float runThreshold = 4f;
    [SerializeField] private bool showDebug = true;

    // Сделаем currentSpeed публичным для отладки
    [SerializeField] public float currentSpeed = 0f;

    private GameObject playerModel;
    private Transform playerTransform;
    private Vector3 lastPosition;
    private bool isGrounded = true;
    private bool isDead = false;
    private bool isRunning = false;
    [SerializeField] private Animator currentAnimator;
    private float lastDebugLogTime = 0f;
    private float debugLogInterval = 0.5f;
    private bool positionInitialized = false;

    #region Unity Lifecycle

    private void Start()
    {
        Initialize();
        //LogDebug($"[PlayerAnimationController] Started. Animator found: {currentAnimator != null}");
    }

    private void Update()
    {
        // Инициализируем позицию на первом кадре
        if (!positionInitialized)
        {
            lastPosition = transform.position;
            positionInitialized = true;
            return;
        }

        CalculateMovement();
        UpdateAnimationParameters();
        LogAnimationDebug();
    }

    private void LateUpdate()
    {
        // Обновляем последнюю позицию
        lastPosition = transform.position;
    }

    #endregion

    #region Initialization

    private void Initialize()
    {
        playerTransform = transform;
        lastPosition = playerTransform.position;
        positionInitialized = true;

        FindPlayerModel();
        FindAnimator();
    }

    private void FindPlayerModel()
    {
        playerModel = GameObject.FindGameObjectWithTag(playerModelTag);

        if (playerModel == null)
        {
            var modelTransform = transform.Find("PlayerModel");
            if (modelTransform != null)
            {
                playerModel = modelTransform.gameObject;
            }
        }

        if (playerModel == null)
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<Renderer>() != null)
                {
                    playerModel = child.gameObject;
                    break;
                }
            }
        }
    }

    private void FindAnimator()
    {
        if (playerModel != null)
        {
            currentAnimator = playerModel.GetComponentInChildren<Animator>();
        }
    }

    #endregion

    #region Movement Calculation

    private void CalculateMovement()
    {
        Vector3 currentPosition = transform.position;
        Vector3 deltaPosition = currentPosition - lastPosition;

        // Защита от слишком больших значений
        if (Time.deltaTime <= 0f)
        {
            currentSpeed = 0f;
            return;
        }

        // Вычисляем скорость только по горизонтали
        Vector3 horizontalDelta = new Vector3(deltaPosition.x, 0, deltaPosition.z);
        currentSpeed = horizontalDelta.magnitude / Time.deltaTime;

        // Ограничение максимальной скорости (защита от артефактов)
        if (currentSpeed > 50f)
        {
            currentSpeed = 0f;
        }

        // Минимальный порог
        if (currentSpeed < 0.01f)
        {
            currentSpeed = 0f;
        }

        // Отладка только при значительном движении
        if (currentSpeed > 0.1f)
        {
            //LogDebug($"[PlayerAnimationController] Delta: {deltaPosition:F4}, Speed: {currentSpeed:F2}");
        }
    }

    #endregion

    #region Animation Control

    private void UpdateAnimationParameters()
    {
        if (currentAnimator == null) return;

        // Обновляем параметры
        currentAnimator.SetFloat(speedParameter, currentSpeed);
        currentAnimator.SetBool(isGroundedParameter, isGrounded);
        currentAnimator.SetBool(isDeadParameter, isDead);

        // Определяем бежит ли игрок
        isRunning = currentSpeed > runThreshold && isGrounded;
        currentAnimator.SetBool(isRunningParameter, isRunning);

        // Отладка изменений скорости
        if (currentSpeed > 0.5f)
        {
            //LogDebug($"[PlayerAnimationController] Moving - Speed: {currentSpeed:F2}, IsRunning: {isRunning}");
        }
    }

    public void PlayJumpAnimation()
    {
        if (currentAnimator == null) return;

        //LogDebug("[PlayerAnimationController] Playing jump animation");
        currentAnimator.SetTrigger(jumpParameter);
    }

    public void PlayDeathAnimation()
    {
        if (currentAnimator == null || isDead) return;

        //LogDebug("[PlayerAnimationController] Playing death animation");
        isDead = true;
        currentAnimator.SetBool(isDeadParameter, true);
    }

    #endregion

    #region State Management

    public void SetGrounded(bool grounded)
    {
        if (isGrounded != grounded)
        {
            //LogDebug($"[PlayerAnimationController] Grounded state changed to: {grounded}");
        }
        isGrounded = grounded;
    }

    public void SetDead(bool dead)
    {
        if (isDead != dead)
        {
            //LogDebug($"[PlayerAnimationController] Dead state changed to: {dead}");
        }
        isDead = dead;
    }

    /// <summary>
    /// Установка скорости напрямую (от ThirdPersonController)
    /// </summary>
    public void SetSpeed(float speed)
    {
        // Ограничение скорости
        if (speed > 50f)
        {
            speed = 0f;
        }

        currentSpeed = speed;

        // Обновляем параметры немедленно
        if (currentAnimator != null)
        {
            currentAnimator.SetFloat(speedParameter, currentSpeed);
            isRunning = currentSpeed > runThreshold && isGrounded;
            currentAnimator.SetBool(isRunningParameter, isRunning);
        }

        if (showDebug && currentSpeed > 0.1f)
        {
            //LogDebug($"PlayerAnimationController.SetSpeed: {speed:F2}");
        }
    }

    #endregion

    #region Skin Integration

    /// <summary>
    /// Обновление Animator при смене скина
    /// </summary>
    public void OnSkinChanged(SkinManager.SkinData newSkin)
    {
        /*if (newSkin == null) return;

        LogDebug($"[PlayerAnimationController] Skin changed to: {newSkin.skinName}");

        if (newSkin.skinType == SkinManager.SkinType.Model && newSkin.modelPrefab != null)
        {
            var newAnimator = newSkin.modelPrefab.GetComponentInChildren<Animator>();
            if (newAnimator != null)
            {
                currentAnimator = newAnimator;
                LogDebug($"[PlayerAnimationController] New animator assigned: {newAnimator.name}");
            }
            else
            {
                LogDebug("[PlayerAnimationController] New skin has no animator");
            }
        }*/

        //Debug.Log("[PlayerAnimationController] OnSkinChanged called");
        RefreshAnimator();
    }

    #endregion

    #region Debug

    private void LogDebug(string message)
    {
        if (showDebug)
        {
            Debug.Log(message);
        }
    }

    private void LogAnimationDebug()
    {
        if (!showDebug || currentAnimator == null) return;

        if (Time.time - lastDebugLogTime > debugLogInterval)
        {
            lastDebugLogTime = Time.time;

            //LogDebug($"[PlayerAnimationController] Status - Speed: {currentSpeed:F2}, Grounded: {isGrounded}, Running: {isRunning}");
        }
    }

    #endregion

    #region Public Methods

    public Animator GetAnimator()
    {
        return currentAnimator;
    }

    public bool HasAnimator()
    {
        return currentAnimator != null;
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    /// <summary>
    /// Обновление Animator при смене скина
    /// </summary>
    public void RefreshAnimator()
    {
        //Debug.Log("[PlayerAnimationController] Refreshing Animator");

        // Ищем новый Animator
        FindAnimator();

        if (currentAnimator != null)
        {
            //Debug.Log($"[PlayerAnimationController] New Animator found: {currentAnimator.name}");

            // Сбрасываем все триггеры
            currentAnimator.ResetTrigger(jumpParameter);
            currentAnimator.SetBool(isDeadParameter, isDead);
            currentAnimator.SetBool(isGroundedParameter, isGrounded);
            currentAnimator.SetBool(isRunningParameter, isRunning);
            currentAnimator.SetFloat(speedParameter, currentSpeed);
        }
        else
        {
            //Debug.LogWarning("[PlayerAnimationController] No Animator found after skin change");
        }
    }

    #endregion
}