using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ObjectPoolManager;

/// <summary>
/// �������� ����� �������� ��� ����������� ������������������
/// ��������� �������� ������������ ������� ������ ����������� ��������/�����������
/// </summary>
public class ObjectPoolManager : Singleton<ObjectPoolManager>
{
    /// <summary>
    /// ��������� ��� �������� ���������� � ��������� ��������
    /// </summary>
    [System.Serializable]
    public class DropItem
    {
        public GameObject prefab;
        [Range(0f, 100f)] public float dropChance = 100f; // ���� ��������� � ���������

        public DropItem(GameObject prefab, float chance)
        {
            this.prefab = prefab;
            this.dropChance = chance;
        }
    }

    /// <summary>
    /// ��������� ��� �������� ���� �������� � �������� ���������
    /// </summary>
    private class DropPool
    {
        public Dictionary<string, Queue<GameObject>> pooledObjects; // ���� ��� ������� �������
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

            // ������� ���� ��� ������� �������
            foreach (var item in dropItems)
            {
                if (item.prefab != null)
                {
                    string key = item.prefab.name;
                    pooledObjects[key] = new Queue<GameObject>();

                    // ������� ��������� ��� ��������
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
        /// �������� ������ ������� ��� ����
        /// </summary>
        private GameObject CreatePooledObject(GameObject prefab, string poolKey)
        {
            GameObject obj = Instantiate(prefab, parent);
            PooledObject pooledComponent = obj.AddComponent<PooledObject>();
            pooledComponent.poolTag = poolKey;
            pooledComponent.dropPoolKey = GetDropPoolKey(dropItems);
            return obj;
        }

        /// <summary>
        /// ��������� ������� �� ���� �� ������� ���������
        /// </summary>
        public GameObject GetPooledObject()
        {
            // �������� ������� �� ������ ���������
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
        /// ������� ������� � ��������������� ���
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
        /// ����� �������� �� ������ ���������
        /// </summary>
        private GameObject SelectItemByDropChance()
        {
            // ����� ������� (Roulette Wheel Selection)
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

            // ���� ���-�� ����� �� ���, ���������� ������ ��������� ������
            return dropItems.FirstOrDefault(item => item.prefab != null)?.prefab;
        }

        /// <summary>
        /// ��������� ����� ��� ���� ���������
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
    /// ��������� ��� ������������ �������������� ������� � ����
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        public string poolTag; // ���� ����������� ������� � ����
        public string dropPoolKey; // ���� ���� ���������
        public System.Action onReturnToPool; // Callback ��� �������� � ���
    }

    [Header("Pool Settings")]
    [SerializeField] private bool createPoolParent = true;

    private Dictionary<string, DropPool> dropPools; // ���� ���������
    private Transform poolParent;

    #region Unity Lifecycle

    /// <summary>
    /// ������������� ��������� �����
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// ������������� ������� �����
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
    /// �������� ������ ���� ���������
    /// </summary>
    /// <param name="dropItems">������ ��������� � ������� ���������</param>
    /// <param name="size">��������� ������ ���� ��� ������� �������</param>
    /// <param name="expandable">����� �� ���� �����������</param>
    /// <param name="parent">������������ ������ ��� ����� (�����������)</param>
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
    /// �������� �������� ���� ��� ������ �������
    /// </summary>
    public void CreatePool(GameObject prefab, int size, bool expandable = true, Transform parent = null)
    {
        var dropItem = new DropItem(prefab, 100f);
        CreateDropPool(new DropItem[] { dropItem }, size, expandable, parent);
    }

    /// <summary>
    /// ������������ ���������� �����
    /// </summary>
    public void PreloadDropPools(DropPoolData[] poolData)
    {
        foreach (var data in poolData)
        {
            CreateDropPool(data.dropItems, data.size, data.expandable);
        }
    }

    /// <summary>
    /// ��������� ����� ��� ���� ���������
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
    /// ��������� ������� �� ���� ��������� �� ������� ������
    /// </summary>
    /// <param name="dropItems">������ ��������� � �������</param>
    /// <param name="position">������� ������</param>
    /// <param name="rotation">������� ������</param>
    /// <returns>������ �� ���� ��� null</returns>
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
    /// ��������� ������� �� �������� ����
    /// </summary>
    public GameObject GetPooledObject(GameObject prefab, Vector3 position = default, Quaternion rotation = default)
    {
        var dropItem = new DropItem(prefab, 100f);
        return GetDropObject(new DropItem[] { dropItem }, position, rotation);
    }

    /// <summary>
    /// ������� ������� � ���
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

        // �������� callback ���� ����
        pooledObj.onReturnToPool?.Invoke();
        pooledObj.onReturnToPool = null;

        string dropPoolKey = pooledObj.dropPoolKey;

        bool returned = false;
        foreach (var pool in dropPools.Values)
        {
            // ���� ���, �������� ����������� ������
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
    /// ����������� ������� (���������� � ��� ��� ����������)
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
    /// �������� ��� �������� ������� � ��� � ���������
    /// </summary>
    private System.Collections.IEnumerator ReturnObjectAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnObjectToPool(obj);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// ������� ���� �����
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
    /// ��������� ���������� � ����
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
/// ��������� ������ ��� ������������ ����� ���������
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