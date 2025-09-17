using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Виртуальный джойстик для мобильного управления
/// Работает с EventSystem и поддерживает drag-управление
/// </summary>
public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    [Header("Joystick Elements")]
    [SerializeField] private Image joystickBackground;
    [SerializeField] private Image joystickHandle;
    
    [Header("Joystick Settings")]
    [SerializeField] private float joystickRange = 50f;
    
    private Vector2 inputVector = Vector2.zero;
    private RectTransform backgroundRect;
    private RectTransform handleRect;
    private bool isInitialized = false;

    #region Unity Lifecycle

    private void Start()
    {
        Initialize();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Инициализация компонентов джойстика
    /// </summary>
    private void Initialize()
    {
        if (isInitialized) return;
        
        if (joystickBackground != null)
        {
            backgroundRect = joystickBackground.GetComponent<RectTransform>();
        }
        
        if (joystickHandle != null)
        {
            handleRect = joystickHandle.GetComponent<RectTransform>();
        }
        
        if (backgroundRect == null || handleRect == null)
        {
            Debug.LogWarning($"[VirtualJoystick] Joystick components not properly assigned on {gameObject.name}");
        }
        
        isInitialized = true;
    }

    #endregion

    #region Event System Implementation

    /// <summary>
    /// Обработка перетаскивания джойстика
    /// </summary>
    /// <param name="eventData">Данные события перетаскивания</param>
    public void OnDrag(PointerEventData eventData)
    {
        if (!isInitialized || backgroundRect == null || handleRect == null) return;
        
        Vector2 position = Vector2.zero;
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                backgroundRect, 
                eventData.position, 
                eventData.pressEventCamera, 
                out position))
        {
            // Нормализуем позицию относительно размера фона
            position.x = position.x / backgroundRect.sizeDelta.x;
            position.y = position.y / backgroundRect.sizeDelta.y;

            // Преобразуем в диапазон [-1, 1]
            inputVector = new Vector2(position.x * 2, position.y * 2);
            
            // Ограничиваем длину вектора единицей (круговая область)
            inputVector = (inputVector.magnitude > 1) ? inputVector.normalized : inputVector;

            // Двигаем ручку джойстика
            handleRect.anchoredPosition = new Vector2(
                inputVector.x * joystickRange,
                inputVector.y * joystickRange
            );
        }
    }

    /// <summary>
    /// Обработка нажатия на джойстик
    /// </summary>
    /// <param name="eventData">Данные события нажатия</param>
    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    /// <summary>
    /// Обработка отпускания джойстика
    /// </summary>
    /// <param name="eventData">Данные события отпускания</param>
    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        
        if (handleRect != null)
        {
            handleRect.anchoredPosition = Vector2.zero;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Получение горизонтального значения джойстика
    /// </summary>
    /// <returns>Значение от -1 до 1</returns>
    public float Horizontal()
    {
        return inputVector.x;
    }

    /// <summary>
    /// Получение вертикального значения джойстика
    /// </summary>
    /// <returns>Значение от -1 до 1</returns>
    public float Vertical()
    {
        return inputVector.y;
    }

    /// <summary>
    /// Получение полного вектора ввода
    /// </summary>
    /// <returns>Нормализованный вектор направления</returns>
    public Vector2 GetInputVector()
    {
        return inputVector;
    }

    #endregion
}
