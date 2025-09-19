using UnityEngine;

/// <summary>
/// ������� ����� ��� �������� Singleton'�� � Unity
/// ������������� ����������� ��� �������� ����� �������
/// </summary>
/// <typeparam name="T">��� ������������ ������</typeparam>
public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    /// <summary>
    /// �������� ��� ��������� ���������� Singleton'�
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    // ���� ������������ ���������
                    _instance = FindObjectOfType<T>();

                    if (_instance == null)
                    {
                        // ������� ����� GameObject � �����������
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = $"({nameof(Singleton<T>)}) {typeof(T)}";

                        // ������ DontDestroyOnLoad ����� ���������� ����� �������
                        DontDestroyOnLoad(singletonObject);
                    }
                }

                return _instance;
            }
        }
    }

    /// <summary>
    /// �������� ������������� ����������
    /// </summary>
    public static bool HasInstance => _instance != null;

    /// <summary>
    /// ����� Awake - ���������� ��� �������� �������
    /// </summary>
    protected virtual void Awake()
    {
        lock (_lock)
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[Singleton] Another instance of '{typeof(T)}' already exists. Destroying this one.");
                Destroy(gameObject);
            }
            else
            {
                _instance = (T)this;
                DontDestroyOnLoad(gameObject);
            }
        }
    }

    /// <summary>
    /// ����� OnDestroy - ���������� ��� ����������� �������
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _applicationIsQuitting = true;
            _instance = null;
        }
    }

    /// <summary>
    /// ����� OnApplicationQuit - ���������� ��� �������� ����������
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }
}