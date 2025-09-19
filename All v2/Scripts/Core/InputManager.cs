using UnityEngine;

/// <summary>
/// �������� ����� ��� �������������� ���������� �� ������ ����������
/// ������������ ��� ���, ��� � ����������/����
/// </summary>
public class InputManager : Singleton<InputManager>
{
    [Header("Platform Settings")]
    [SerializeField] private bool isMobile = false;
    
    private VirtualJoystick virtualJoystick;
    private bool isInitialized = false;

    /// <summary>
    /// ������������� ��������� �����
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    /// <summary>
    /// ������������� ����������� �����
    /// </summary>
    private void Initialize()
    {
        if (isInitialized) return;
        
        if (isMobile)
        {
            virtualJoystick = FindObjectOfType<VirtualJoystick>();
        }
        
        isInitialized = true;
    }

    /// <summary>
    /// ��������� ��������������� �����
    /// </summary>
    /// <returns>�������� �� -1 �� 1</returns>
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
    /// ��������� ������������� �����
    /// </summary>
    /// <returns>�������� �� -1 �� 1</returns>
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
    /// �������� ������� ������ ������
    /// </summary>
    /// <returns>true ���� ������ ������ ������</returns>
    public bool GetJumpInput()
    {
        if (isMobile)
        {
            // ��� ��������� ������ ����� ������������ ��������� ������
            return false; // ����������� ����� UI ������
        }
        else
        {
            return Input.GetButtonDown("Jump");
        }
    }

    /// <summary>
    /// ��������� ���������
    /// </summary>
    /// <param name="mobile">true ��� ��������� ���������</param>
    public void SetPlatform(bool mobile)
    {
        isMobile = mobile;
        Initialize();
    }

    /// <summary>
    /// �������� �������� �� ������� ��������� ���������
    /// </summary>
    public bool IsMobile => isMobile;
}