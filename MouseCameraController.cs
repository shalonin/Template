using UnityEngine;

public class MouseCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Settings")]
    public float distance = 5f;
    public float height = 2f;
    public float rotationSpeed = 2f;

    [Header("Mouse Settings")]
    public bool invertY = false;

    private float mouseX;
    private float mouseY;
    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Получаем ввод мыши
        mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * (invertY ? -1 : 1);

        xRotation += mouseX;
        yRotation -= mouseY;
        yRotation = Mathf.Clamp(yRotation, -30f, 60f); // Ограничение по вертикали

        // Поворачиваем камеру
        Quaternion rotation = Quaternion.Euler(yRotation, xRotation, 0);
        Vector3 position = target.position - (rotation * Vector3.forward * distance);
        position.y = target.position.y + height;

        transform.position = position;
        transform.LookAt(target.position + Vector3.up * height * 0.5f);
    }
}