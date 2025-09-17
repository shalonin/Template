using UnityEngine;

/// <summary>
/// Контроллер камеры с управлением мышью для третьего лица
/// Поддерживает вращение вокруг цели и плавное следование
/// </summary>
public class MouseCameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    
    [Header("Camera Distance")]
    [SerializeField] private float distance = 5f;
    [SerializeField] private float height = 2f;
    
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float minYAngle = -30f;
    [SerializeField] private float maxYAngle = 60f;
    [SerializeField] private bool invertY = false;
    
    [Header("Smoothing")]
    [SerializeField] private bool useSmoothing = true;
    [SerializeField] private float smoothSpeed = 5f;
    
    private float mouseX;
    private float mouseY;
    private float xRotation = 0f;
    private float yRotation = 0f;
    private Vector3 velocity = Vector3.zero;

    #region Unity Lifecycle

    private void Start()
    {
        Initialize();
    }

    private void LateUpdate()
    {
        if (target == null) return;
        
        HandleMouseInput();
        UpdateCameraPosition();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Инициализация контроллера камеры
    /// </summary>
    private void Initialize()
    {
        if (target == null)
        {
            Debug.LogWarning($"[MouseCameraController] Target not assigned on {gameObject.name}");
        }
        
        // Скрываем и блокируем курсор
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    #endregion

    #region Input Handling

    /// <summary>
    /// Обработка ввода с мыши
    /// </summary>
    private void HandleMouseInput()
    {
        mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * (invertY ? -1 : 1);
        
        xRotation += mouseX;
        yRotation -= mouseY;
        yRotation = Mathf.Clamp(yRotation, minYAngle, maxYAngle);
    }

    #endregion

    #region Camera Logic

    /// <summary>
    /// Обновление позиции камеры
    /// </summary>
    private void UpdateCameraPosition()
    {
        // Вычисляем желаемую позицию камеры
        Vector3 targetPosition = CalculateCameraPosition();
        
        if (useSmoothing)
        {
            // Плавное следование камеры
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothSpeed * Time.deltaTime);
        }
        else
        {
            // Мгновенное перемещение
            transform.position = targetPosition;
        }
        
        // Смотрим на цель
        transform.LookAt(target.position + Vector3.up * height * 0.3f);
    }

    /// <summary>
    /// Вычисление позиции камеры относительно цели
    /// </summary>
    /// <returns>Позиция камеры в мировых координатах</returns>
    private Vector3 CalculateCameraPosition()
    {
        // Создаем вектор отступа
        Vector3 offset = new Vector3(0, height, -distance);
        
        // Применяем вращение
        Quaternion rotation = Quaternion.Euler(yRotation, xRotation, 0);
        Vector3 rotatedOffset = rotation * offset;
        
        // Позиция относительно цели
        return target.position + rotatedOffset;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Установка новой цели для камеры
    /// </summary>
    /// <param name="newTarget">Новая цель слежения</param>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// Установка дистанции камеры
    /// </summary>
    /// <param name="newDistance">Новая дистанция</param>
    public void SetDistance(float newDistance)
    {
        distance = Mathf.Max(0.1f, newDistance);
    }

    /// <summary>
    /// Установка высоты камеры
    /// </summary>
    /// <param name="newHeight">Новая высота</param>
    public void SetHeight(float newHeight)
    {
        height = newHeight;
    }

    /// <summary>
    /// Переключение видимости курсора
    /// </summary>
    /// <param name="visible">Видимость курсора</param>
    public void SetCursorVisible(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    #endregion

    #region Unity Events

    private void OnApplicationFocus(bool focus)
    {
        if (focus && !InputManager.HasInstance || (InputManager.HasInstance && !InputManager.Instance.IsMobile))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    #endregion
}
