using UnityEngine;
using UnityEngine.InputSystem.XR;
using static UnityEngine.AudioSettings;

/// <summary>
/// Контроллер персонажа от третьего лица
/// Работает с InputManager для универсального ввода
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    [Header("Animation Integration")]
    [SerializeField] private bool useAnimationController = true;

    private PlayerAnimationController animationController;

    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;

    private Vector3 lastPosition; // Добавим для вычисления скорости
    private float currentSpeed = 0f; // Текущая скорость

    #region Unity Lifecycle

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    private void Start()
    {
        Initialize();

        // Инициализация анимационного контроллера
        if (useAnimationController)
        {
            animationController = GetComponent<PlayerAnimationController>();
        }
    }

    private void Update()
    {
        if (!InputManager.Instance.IsMobile && Input.GetButtonDown("Jump"))
        {
            Jump();
        }
        // Обновление анимаций
        UpdateAnimations();
        // Обновляем последнюю позицию
        lastPosition = transform.position;

        HandleMovement();
        HandleGravity();
    }


    /// <summary>
    /// Обновление анимаций
    /// </summary>
    private void UpdateAnimations()
    {
        if (animationController == null || !useAnimationController) return;

        // Вычисляем скорость на основе изменения позиции
        Vector3 deltaPosition = transform.position - lastPosition;
        Vector3 horizontalDelta = new Vector3(deltaPosition.x, 0, deltaPosition.z);
        currentSpeed = horizontalDelta.magnitude / Time.deltaTime;

        // Защита от артефактов
        if (currentSpeed > 50f || Time.deltaTime <= 0f)
        {
            currentSpeed = 0f;
        }

        // Отладка
        if (currentSpeed > 0.1f)
        {
            //Debug.Log($"[ThirdPersonController] Calculated Speed: {currentSpeed:F2}");
        }

        // Передаем скорость в анимационный контроллер
        animationController.SetSpeed(currentSpeed);
        animationController.SetGrounded(isGrounded);
    }

    /// <summary>
    /// Проигрывание анимации прыжка
    /// </summary>
    public void PlayJumpAnimation()
    {
        if (animationController != null)
        {
            animationController.PlayJumpAnimation();
        }
    }

    /// <summary>
    /// Проигрывание анимации смерти
    /// </summary>
    public void PlayDeathAnimation()
    {
        if (animationController != null)
        {
            animationController.PlayDeathAnimation();
        }
    }
    #endregion

    #region Initialization

    /// <summary>
    /// Инициализация компонентов контроллера
    /// </summary>
    private void Initialize()
    {
        characterController = GetComponent<CharacterController>();

        if (characterController == null)
        {
            Debug.LogError($"[ThirdPersonController] CharacterController component is required on {gameObject.name}");
        }

        if (groundCheck == null)
        {
            Debug.LogWarning($"[ThirdPersonController] GroundCheck is not assigned on {gameObject.name}");
        }
    }

    #endregion

    #region Movement Logic

    /// <summary>
    /// Обработка движения персонажа
    /// </summary>
    private void HandleMovement()
    {
        if (characterController == null) return;

        CheckGrounded();

        float horizontal = InputManager.HasInstance ? InputManager.Instance.GetHorizontalInput() : 0f;
        float vertical = InputManager.HasInstance ? InputManager.Instance.GetVerticalInput() : 0f;

        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical);

        if (moveDirection.magnitude > 0.1f)
        {
            // Преобразование направления относительно камеры
            Vector3 cameraForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
            Vector3 cameraRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 move = cameraForward * vertical + cameraRight * horizontal;
            move.Normalize();

            // Перемещение
            characterController.Move(move * moveSpeed * Time.deltaTime);

            // Поворот персонажа
            if (move != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// Обработка гравитации и прыжков
    /// </summary>
    private void HandleGravity()
    {
        if (characterController == null) return;

        CheckGrounded();

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Прижимаем к земле
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Проверка контакта с землей
    /// </summary>
    private void CheckGrounded()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Выполнение прыжка
    /// </summary>
    public void Jump()
    {
        if (isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            // Проигрываем анимацию прыжка
            if (animationController != null)
            {
                animationController.PlayJumpAnimation();
            }

        }
    }

    public void Die()
    {
        // Проигрываем анимацию смерти
        if (animationController != null)
        {
            animationController.PlayDeathAnimation();
        }
    }

    /// <summary>
    /// Установка скорости движения
    /// </summary>
    /// <param name="speed">Новая скорость движения</param>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    #endregion
}