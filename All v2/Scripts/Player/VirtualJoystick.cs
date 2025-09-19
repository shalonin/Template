using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// ����������� �������� ��� ���������� ����������
/// �������� � EventSystem � ������������ drag-����������
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
    /// ������������� ����������� ���������
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
    /// ��������� �������������� ���������
    /// </summary>
    /// <param name="eventData">������ ������� ��������������</param>
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
            // ����������� ������� ������������ ������� ����
            position.x = position.x / backgroundRect.sizeDelta.x;
            position.y = position.y / backgroundRect.sizeDelta.y;

            // ����������� � �������� [-1, 1]
            inputVector = new Vector2(position.x * 2, position.y * 2);

            // ������������ ����� ������� �������� (�������� �������)
            inputVector = (inputVector.magnitude > 1) ? inputVector.normalized : inputVector;

            // ������� ����� ���������
            handleRect.anchoredPosition = new Vector2(
                inputVector.x * joystickRange,
                inputVector.y * joystickRange
            );
        }
    }

    /// <summary>
    /// ��������� ������� �� ��������
    /// </summary>
    /// <param name="eventData">������ ������� �������</param>
    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    /// <summary>
    /// ��������� ���������� ���������
    /// </summary>
    /// <param name="eventData">������ ������� ����������</param>
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
    /// ��������� ��������������� �������� ���������
    /// </summary>
    /// <returns>�������� �� -1 �� 1</returns>
    public float Horizontal()
    {
        return inputVector.x;
    }

    /// <summary>
    /// ��������� ������������� �������� ���������
    /// </summary>
    /// <returns>�������� �� -1 �� 1</returns>
    public float Vertical()
    {
        return inputVector.y;
    }

    /// <summary>
    /// ��������� ������� ������� �����
    /// </summary>
    /// <returns>��������������� ������ �����������</returns>
    public Vector2 GetInputVector()
    {
        return inputVector;
    }

    #endregion
}