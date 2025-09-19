using UnityEngine;

/// <summary>
/// ���������� ������ � ����������� ����� ��� �������� ����
/// ������������ �������� ������ ���� � ������� ����������
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
    /// ������������� ����������� ������
    /// </summary>
    private void Initialize()
    {
        if (target == null)
        {
            Debug.LogWarning($"[MouseCameraController] Target not assigned on {gameObject.name}");
        }

        // �������� � ��������� ������
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    #endregion

    #region Input Handling

    /// <summary>
    /// ��������� ����� � ����
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
    /// ���������� ������� ������
    /// </summary>
    private void UpdateCameraPosition()
    {
        // ��������� �������� ������� ������
        Vector3 targetPosition = CalculateCameraPosition();

        if (useSmoothing)
        {
            // ������� ���������� ������
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothSpeed * Time.deltaTime);
        }
        else
        {
            // ���������� �����������
            transform.position = targetPosition;
        }

        // ������� �� ����
        transform.LookAt(target.position + Vector3.up * height * 0.3f);
    }

    /// <summary>
    /// ���������� ������� ������ ������������ ����
    /// </summary>
    /// <returns>������� ������ � ������� �����������</returns>
    private Vector3 CalculateCameraPosition()
    {
        // ������� ������ �������
        Vector3 offset = new Vector3(0, height, -distance);

        // ��������� ��������
        Quaternion rotation = Quaternion.Euler(yRotation, xRotation, 0);
        Vector3 rotatedOffset = rotation * offset;

        // ������� ������������ ����
        return target.position + rotatedOffset;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// ��������� ����� ���� ��� ������
    /// </summary>
    /// <param name="newTarget">����� ���� ��������</param>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// ��������� ��������� ������
    /// </summary>
    /// <param name="newDistance">����� ���������</param>
    public void SetDistance(float newDistance)
    {
        distance = Mathf.Max(0.1f, newDistance);
    }

    /// <summary>
    /// ��������� ������ ������
    /// </summary>
    /// <param name="newHeight">����� ������</param>
    public void SetHeight(float newHeight)
    {
        height = newHeight;
    }

    /// <summary>
    /// ������������ ��������� �������
    /// </summary>
    /// <param name="visible">��������� �������</param>
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