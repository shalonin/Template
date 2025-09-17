using UnityEngine;

/// <summary>
/// Менеджер ввода для универсального управления на разных платформах
/// Поддерживает как тач, так и клавиатуру/мышь
/// </summary>
public class InputManager : Singleton<InputManager>
{
    [Header("Platform Settings")]
    [SerializeField] private bool isMobile = false;
    
    private VirtualJoystick virtualJoystick;
    private bool isInitialized = false;

    /// <summary>
    /// Инициализация менеджера ввода
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    /// <summary>
    /// Инициализация компонентов ввода
    /// </summary>
    private void Initialize()
    {
        if (isInitialized) return;
        
        if (isMobile)
        {
            virtualJoystick = FindAnyObjectByType<VirtualJoystick>();
        }
        
        isInitialized = true;
    }

    /// <summary>
    /// Получение горизонтального ввода
    /// </summary>
    /// <returns>Значение от -1 до 1</returns>
    public float GetHorizontalInput()
    {
        if (isMobile && virtualJoystick != null)
        {
            return virtualJoystick.Horizontal();
        }
        else if (!isMobile)
        {
            return Input.GetAxis("Horizontal");
        }
        return 0f;
    }

    /// <summary>
    /// Получение вертикального ввода
    /// </summary>
    /// <returns>Значение от -1 до 1</returns>
    public float GetVerticalInput()
    {
        if (isMobile && virtualJoystick != null)
        {
            return virtualJoystick.Vertical();
        }
        else if (!isMobile)
        {
            return Input.GetAxis("Vertical");
        }
        return 0f;
    }

    /// <summary>
    /// Проверка нажатия кнопки прыжка
    /// </summary>
    /// <returns>true если нажата кнопка прыжка</returns>
    public bool GetJumpInput()
    {
        if (isMobile)
        {
            // Для мобильной версии можно использовать отдельную кнопку
            return false; // Реализуется через UI кнопки
        }
        else
        {
            return Input.GetButtonDown("Jump");
        }
    }

    /// <summary>
    /// Установка платформы
    /// </summary>
    /// <param name="mobile">true для мобильной платформы</param>
    public void SetPlatform(bool mobile)
    {
        isMobile = mobile;
        Initialize();
    }

    /// <summary>
    /// Проверка является ли текущая платформа мобильной
    /// </summary>
    public bool IsMobile => isMobile;

}
