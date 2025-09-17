using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    [Header("Joystick Elements")]
    public Image joystickBackground;
    public Image joystickHandle;

    [Header("Settings")]
    public float joystickRange = 50f;

    private Vector2 inputVector = Vector2.zero;
    private RectTransform backgroundRect;
    private RectTransform handleRect;

    void Start()
    {
        if (joystickBackground != null)
            backgroundRect = joystickBackground.GetComponent<RectTransform>();

        if (joystickHandle != null)
            handleRect = joystickHandle.GetComponent<RectTransform>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (backgroundRect == null || handleRect == null) return;

        Vector2 position = Vector2.zero;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                backgroundRect,
                eventData.position,
                eventData.pressEventCamera,
                out position))
        {
            position.x = position.x / backgroundRect.sizeDelta.x;
            position.y = position.y / backgroundRect.sizeDelta.y;

            inputVector = new Vector2(position.x * 2, position.y * 2);
            inputVector = (inputVector.magnitude > 1) ? inputVector.normalized : inputVector;

            // Двигаем ручку
            handleRect.anchoredPosition = new Vector2(
                inputVector.x * joystickRange,
                inputVector.y * joystickRange
            );
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        if (handleRect != null)
            handleRect.anchoredPosition = Vector2.zero;
    }

    public float Horizontal() { return inputVector.x; }
    public float Vertical() { return inputVector.y; }
    public Vector2 GetInputVector() { return inputVector; }
}