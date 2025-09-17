using UnityEngine;

/// <summary>
/// Базовый класс для создания Singleton'ов в Unity
/// Автоматически сохраняется при переходе между сценами
/// </summary>
/// <typeparam name="T">Тип наследуемого класса</typeparam>
public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    /// <summary>
    /// Свойство для получения экземпляра Singleton'а
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
                    // Ищем существующий экземпляр
                    _instance = FindObjectOfType<T>();

                    if (_instance == null)
                    {
                        // Создаем новый GameObject с компонентом
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = $"({nameof(Singleton<T>)}) {typeof(T)}";

                        // Делаем DontDestroyOnLoad чтобы сохранялся между сценами
                        DontDestroyOnLoad(singletonObject);
                    }
                }

                return _instance;
            }
        }
    }

    /// <summary>
    /// Проверка существования экземпляра
    /// </summary>
    public static bool HasInstance => _instance != null;

    /// <summary>
    /// Метод Awake - вызывается при создании объекта
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
    /// Метод OnDestroy - вызывается при уничтожении объекта
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
    /// Метод OnApplicationQuit - вызывается при закрытии приложения
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }
}
