using UnityEngine;
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

    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;

    #region Unity Lifecycle

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        if (!InputManager.Instance.IsMobile && Input.GetButtonDown("Jump"))
        {
            Jump();
        }

        HandleMovement();
        HandleGravity();
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