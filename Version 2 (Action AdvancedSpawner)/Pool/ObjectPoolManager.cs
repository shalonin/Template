using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ObjectPoolManager;

/// <summary>
/// Менеджер пулов объектов для оптимизации производительности
/// Позволяет повторно использовать объекты вместо постоянного создания/уничтожения
/// </summary>
public class ObjectPoolManager : Singleton<ObjectPoolManager>
{
    /// <summary>
    /// Структура для хранения информации о выпадении предмета
    /// </summary>
    [System.Serializable]
    public class DropItem
    {
        public GameObject prefab;
        [Range(0f, 100f)] public float dropChance = 100f; // Шанс выпадения в процентах

        public DropItem(GameObject prefab, float chance)
        {
            this.prefab = prefab;
            this.dropChance = chance;
        }
    }

    /// <summary>
    /// Структура для хранения пула объектов с системой выпадения
    /// </summary>
    private class DropPool
    {
        public Dictionary<string, Queue<GameObject>> pooledObjects; // Пулы для каждого префаба
        public DropItem[] dropItems;
        public Transform parent;
        public int defaultSize;
        public bool canExpand;

        public DropPool(DropItem[] items, int size, bool expandable, Transform parentTransform)
        {
            this.dropItems = items;
            this.defaultSize = size;
            this.canExpand = expandable;
            this.parent = parentTransform;
            this.pooledObjects = new Dictionary<string, Queue<GameObject>>();

            // Создаем пулы для каждого префаба
            foreach (var item in dropItems)
            {
                if (item.prefab != null)
                {
                    string key = item.prefab.name;
                    pooledObjects[key] = new Queue<GameObject>();

                    // Создаем начальный пул объектов
                    for (int i = 0; i < size; i++)
                    {
                        GameObject obj = CreatePooledObject(item.prefab, key);
                        obj.SetActive(false);
                        pooledObjects[key].Enqueue(obj);
                    }
                }
            }
        }

        /// <summary>
        /// Создание нового объекта для пула
        /// </summary>
        private GameObject CreatePooledObject(GameObject prefab, string poolKey)
        {
            GameObject obj = Instantiate(prefab, parent);
            PooledObject pooledComponent = obj.AddComponent<PooledObject>();
            pooledComponent.poolTag = poolKey;
            pooledComponent.dropPoolKey = GetDropPoolKey(dropItems);

            // Убеждаемся что SpawnedObject компонент правильно инициализирован
            var spawnedComponent = obj.GetComponent<SpawnedObject>();
            if (spawnedComponent != null)
            {
                // Awake уже вызван, но можно добавить дополнительную инициализацию если нужно
            }

            return obj;
        }

        /// <summary>
        /// Получение объекта из пула по системе выпадения
        /// </summary>
        public GameObject GetPooledObject()
        {
            // Выбираем предмет по шансам выпадения
            GameObject selectedPrefab = SelectItemByDropChance();

            if (selectedPrefab == null) return null;

            string key = selectedPrefab.name;

            if (pooledObjects.ContainsKey(key) && pooledObjects[key].Count > 0)
            {
                GameObject obj = pooledObjects[key].Dequeue();
                obj.SetActive(true);
                return obj;
            }
            else if (canExpand)
            {
                GameObject obj = CreatePooledObject(selectedPrefab, key);
                obj.SetActive(true);
                return obj;
            }

            return null;
        }

        /// <summary>
        /// Возврат объекта в соответствующий пул
        /// </summary>
        public void ReturnObjectToPool(GameObject obj)
        {
            obj.SetActive(false);

            PooledObject pooledObj = obj.GetComponent<PooledObject>();
            if (pooledObj != null && pooledObjects.ContainsKey(pooledObj.poolTag))
            {
                pooledObjects[pooledObj.poolTag].Enqueue(obj);
            }
        }

        /// <summary>
        /// Выбор предмета по шансам выпадения
        /// </summary>
        private GameObject SelectItemByDropChance()
        {
            // Метод рулетки (Roulette Wheel Selection)
            float totalChance = dropItems.Sum(item => item.dropChance);
            float randomValue = Random.Range(0f, totalChance);

            float currentChance = 0f;
            foreach (var item in dropItems)
            {
                currentChance += item.dropChance;
                if (randomValue <= currentChance && item.prefab != null)
                {
                    return item.prefab;
                }
            }

            // Если что-то пошло не так, возвращаем первый доступный префаб
            return dropItems.FirstOrDefault(item => item.prefab != null)?.prefab;
        }

        /// <summary>
        /// Получение ключа для пула выпадения
        /// </summary>
        private string GetDropPoolKey(DropItem[] items)
        {
            if (items == null || items.Length == 0) return "DropPool";

            var prefabNames = items.Where(item => item.prefab != null)
                                  .Select(item => item.prefab.name);
            return string.Join("_", prefabNames);
        }
    }

    /// <summary>
    /// Компонент для отслеживания принадлежности объекта к пулу
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        public string poolTag; // Ключ конкретного префаба в пуле
        public string dropPoolKey; // Ключ пула выпадения
        public System.Action onReturnToPool; // Callback при возврате в пул
    }

    [Header("Pool Settings")]
    [SerializeField] private bool createPoolParent = true;

    private Dictionary<string, DropPool> dropPools; // Пулы выпадения
    private Transform poolParent;

    #region Unity Lifecycle

    /// <summary>
    /// Инициализация менеджера пулов
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Инициализация системы пулов
    /// </summary>
    private void Initialize()
    {
        dropPools = new Dictionary<string, DropPool>();

        if (createPoolParent)
        {
            poolParent = new GameObject("PooledObjects").transform;
            DontDestroyOnLoad(poolParent.gameObject);
        }
    }

    #endregion

    #region Pool Management

    /// <summary>
    /// Создание нового пула выпадения
    /// </summary>
    /// <param name="dropItems">Массив предметов с шансами выпадения</param>
    /// <param name="size">Начальный размер пула для каждого префаба</param>
    /// <param name="expandable">Могут ли пулы расширяться</param>
    /// <param name="parent">Родительский объект для пулов (опционально)</param>
    public void CreateDropPool(DropItem[] dropItems, int size, bool expandable = true, Transform parent = null)
    {
        if (dropItems == null || dropItems.Length == 0)
        {
            Debug.LogWarning("[ObjectPoolManager] Drop items array is empty");
            return;
        }

        string poolKey = GenerateDropPoolKey(dropItems);

        if (dropPools.ContainsKey(poolKey))
        {
            Debug.LogWarning($"[ObjectPoolManager] Drop pool {poolKey} already exists");
            return;
        }

        Transform poolTransform = parent ?? (poolParent != null ? poolParent : null);
        dropPools.Add(poolKey, new DropPool(dropItems, size, expandable, poolTransform));
    }

    /// <summary>
    /// Создание простого пула для одного префаба
    /// </summary>
    public void CreatePool(GameObject prefab, int size, bool expandable = true, Transform parent = null)
    {
        var dropItem = new DropItem(prefab, 100f);
        CreateDropPool(new DropItem[] { dropItem }, size, expandable, parent);
    }

    /// <summary>
    /// Предзагрузка нескольких пулов
    /// </summary>
    public void PreloadDropPools(DropPoolData[] poolData)
    {
        foreach (var data in poolData)
        {
            CreateDropPool(data.dropItems, data.size, data.expandable);
        }
    }

    /// <summary>
    /// Генерация ключа для пула выпадения
    /// </summary>
    private string GenerateDropPoolKey(DropItem[] items)
    {
        if (items == null || items.Length == 0) return "EmptyDropPool";

        var prefabNames = items.Where(item => item.prefab != null)
                              .Select(item => item.prefab.name);
        return string.Join("_", prefabNames);
    }

    #endregion

    #region Object Management

    /// <summary>
    /// Получение объекта из пула выпадения по системе шансов
    /// </summary>
    /// <param name="dropItems">Массив предметов с шансами</param>
    /// <param name="position">Позиция спавна</param>
    /// <param name="rotation">Поворот спавна</param>
    /// <returns>Объект из пула или null</returns>
    public GameObject GetDropObject(DropItem[] dropItems, Vector3 position = default, Quaternion rotation = default)
    {
        string poolKey = GenerateDropPoolKey(dropItems);

        if (!dropPools.ContainsKey(poolKey))
        {
            Debug.Log($"[ObjectPoolManager] Creating new drop pool: {poolKey}");
            CreateDropPool(dropItems, 5, true);
        }

        GameObject obj = dropPools[poolKey].GetPooledObject();

        if (obj != null)
        {
            obj.transform.position = position;
            obj.transform.rotation = rotation;
        }

        return obj;
    }

    /// <summary>
    /// Получение объекта из простого пула
    /// </summary>
    public GameObject GetPooledObject(GameObject prefab, Vector3 position = default, Quaternion rotation = default)
    {
        var dropItem = new DropItem(prefab, 100f);
        return GetDropObject(new DropItem[] { dropItem }, position, rotation);
    }

    /// <summary>
    /// Возврат объекта в пул
    /// </summary>
    public void ReturnObjectToPool(GameObject obj)
    {
        PooledObject pooledObj = obj.GetComponent<PooledObject>();

        if (pooledObj == null)
        {
            Debug.LogWarning($"[ObjectPoolManager] Object {obj.name} is not from pool. Destroying instead.");
            Destroy(obj);
            return;
        }

        // Вызываем callback если есть
        pooledObj.onReturnToPool?.Invoke();
        pooledObj.onReturnToPool = null;

        string dropPoolKey = pooledObj.dropPoolKey;

        bool returned = false;
        foreach (var pool in dropPools.Values)
        {
            // Ищем пул, которому принадлежит объект
            if (pool.pooledObjects.ContainsKey(pooledObj.poolTag))
            {
                pool.ReturnObjectToPool(obj);
                returned = true;
                break;
            }
        }

        if (!returned)
        {
            Debug.LogWarning($"[ObjectPoolManager] No pool found for {pooledObj.poolTag}. Destroying object.");
            Destroy(obj);
        }
    }

    /// <summary>
    /// Уничтожение объекта (возвращает в пул или уничтожает)
    /// </summary>
    public void DestroyObject(GameObject obj, float delay = 0f)
    {
        if (delay > 0f)
        {
            StartCoroutine(ReturnObjectAfterDelay(obj, delay));
        }
        else
        {
            ReturnObjectToPool(obj);
        }
    }

    /// <summary>
    /// Корутина для возврата объекта в пул с задержкой
    /// </summary>
    private System.Collections.IEnumerator ReturnObjectAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnObjectToPool(obj);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Очистка всех пулов
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var pool in dropPools.Values)
        {
            foreach (var objectQueue in pool.pooledObjects.Values)
            {
                while (objectQueue.Count > 0)
                {
                    GameObject obj = objectQueue.Dequeue();
                    Destroy(obj);
                }
            }
        }
        dropPools.Clear();
    }

    /// <summary>
    /// Получение информации о пуле
    /// </summary>
    public int GetPoolCount(string prefabName)
    {
        foreach (var pool in dropPools.Values)
        {
            if (pool.pooledObjects.ContainsKey(prefabName))
            {
                return pool.pooledObjects[prefabName].Count;
            }
        }
        return 0;
    }

    #endregion
}

/// <summary>
/// Структура данных для предзагрузки пулов выпадения
/// </summary>
[System.Serializable]
public struct DropPoolData
{
    public DropItem[] dropItems;
    public int size;
    public bool expandable;

    public DropPoolData(DropItem[] dropItems, int size, bool expandable = true)
    {
        this.dropItems = dropItems;
        this.size = size;
        this.expandable = expandable;
    }
}
