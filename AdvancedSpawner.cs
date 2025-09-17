using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ����������� ������� � ���������� ��������� ����� ������ � ��������
/// </summary>
public class AdvancedSpawner : MonoBehaviour
{
    /// <summary>
    /// ���� ������
    /// </summary>
    public enum SpawnType
    {
        SinglePoint,
        MultiplePoints,
        Area,
        Path
    }

    /// <summary>
    /// ���� ����������
    /// </summary>
    public enum IntervalType
    {
        Fixed,
        RandomRange,
        RandomList
    }

    [System.Serializable]
    public class SpawnPointData
    {
        public Transform point;
        public float weight = 1f; // ��� ����� ��� ���������� ������
        public float spawnInterval = 1f; // �������������� �������� ��� �����
        public IntervalType intervalType = IntervalType.Fixed;
        public Vector2 randomRange = new Vector2(0.5f, 2f); // ��� RandomRange
        public float[] randomIntervals = new float[] { 0.5f, 1f, 1.5f, 2f }; // ��� RandomList
    }

    [Header("Spawn Settings")]
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private SpawnType spawnType = SpawnType.SinglePoint;
    [SerializeField] private bool spawnOnStart = false;

    [Header("Spawn Points")]
    [SerializeField] private Transform singleSpawnPoint;
    [SerializeField] private SpawnPointData[] spawnPoints;
    [SerializeField] private Bounds spawnArea = new Bounds(Vector3.zero, Vector3.one * 10f); // ��� Area ������

    [Header("Timing")]
    [SerializeField] private float globalSpawnInterval = 1f;
    [SerializeField] private IntervalType globalIntervalType = IntervalType.Fixed;
    [SerializeField] private Vector2 globalRandomRange = new Vector2(0.5f, 2f);
    [SerializeField] private float[] globalRandomIntervals = new float[] { 0.5f, 1f, 1.5f, 2f };

    [Header("Spawn Count")]
    [SerializeField] private int minSpawnCount = 1;
    [SerializeField] private int maxSpawnCount = 1;
    [SerializeField] private bool useRandomCount = false;

    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private bool poolExpandable = true;
    [SerializeField] private float poolRandomPercentage = 0f; // ������� ����������� ��� ����

    [Header("Drop System")]
    [SerializeField] private bool useDropSystem = false;
    [SerializeField] private ObjectPoolManager.DropItem[] dropItems; // �������� � ������� ���������

    [Header("Active Objects Limit")]
    [SerializeField] private bool useActiveLimit = false;
    [SerializeField] private int maxActiveObjects = 10;
    [SerializeField] private int minActiveObjects = 0;
    [SerializeField] private float checkInterval = 1f; // �������� �������� ���������� �������� ��������

    private List<GameObject> activeObjects; // ������ �������� ��������
    private bool isSpawningAllowed = true;
    private Coroutine limitCheckCoroutine;

    private Dictionary<Transform, float> pointTimers;
    private float globalTimer = 0f;
    private bool isInitialized = false;

    #region Unity Lifecycle

    private void Start()
    {
        Initialize();

        if (spawnOnStart)
        {
            SpawnObjects();
        }

        // ��������� �������� ������� ���� ��������
        if (useActiveLimit)
        {
            limitCheckCoroutine = StartCoroutine(CheckActiveObjectsLimit());
        }
    }

    private void Update()
    {
        UpdateTimers();
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnType == SpawnType.Area)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(spawnArea.center, spawnArea.size);
        }
        else if (spawnType == SpawnType.MultiplePoints && spawnPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (var pointData in spawnPoints)
            {
                if (pointData.point != null)
                {
                    Gizmos.DrawSphere(pointData.point.position, 0.2f);
                }
            }
        }
        else if (spawnType == SpawnType.SinglePoint && singleSpawnPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(singleSpawnPoint.position, 0.2f);
        }
    }

    private void OnDestroy()
    {
        // ������� ��� �������� ������� ��� ����������� ��������
        if (activeObjects != null)
        {
            foreach (var obj in activeObjects)
            {
                if (obj != null && ObjectPoolManager.HasInstance)
                {
                    ObjectPoolManager.Instance.ReturnObjectToPool(obj);
                }
            }
            activeObjects.Clear();
        }

        if (limitCheckCoroutine != null)
        {
            StopCoroutine(limitCheckCoroutine);
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// ������������� ��������
    /// </summary>
    private void Initialize()
    {
        if (isInitialized) return;

        InitializePool();
        InitializeTimers();

        // ������������� ������ �������� ��������
        if (useActiveLimit)
        {
            activeObjects = new List<GameObject>();
        }

        isInitialized = true;
    }

    /// <summary>
    /// ������������� ���� ��������
    /// </summary>
    private void InitializePool()
    {
        if (!useDropSystem && prefabToSpawn != null && ObjectPoolManager.HasInstance)
        {
            // ������������ ������ - ������� ������ �������� poolRandomPercentage
            ObjectPoolManager.Instance.CreatePool(prefabToSpawn, initialPoolSize, poolExpandable);
        }
        else if (useDropSystem && dropItems != null && dropItems.Length > 0 && ObjectPoolManager.HasInstance)
        {
            ObjectPoolManager.Instance.CreateDropPool(dropItems, initialPoolSize, poolExpandable);
        }
    }

    /// <summary>
    /// ������������� �������� ��� ����� ������
    /// </summary>
    private void InitializeTimers()
    {
        pointTimers = new Dictionary<Transform, float>();

        if (spawnType == SpawnType.MultiplePoints && spawnPoints != null)
        {
            foreach (var pointData in spawnPoints)
            {
                if (pointData.point != null)
                {
                    pointTimers[pointData.point] = 0f;
                }
            }
        }
    }

    #endregion

    #region Timer Management

    /// <summary>
    /// ���������� �������� ������
    /// </summary>
    private void UpdateTimers()
    {
        if (!isInitialized) return;

        switch (spawnType)
        {
            case SpawnType.SinglePoint:
                UpdateSinglePointTimer();
                break;
            case SpawnType.MultiplePoints:
                UpdateMultiplePointsTimers();
                break;
            case SpawnType.Area:
                UpdateGlobalTimer();
                break;
            case SpawnType.Path:
                UpdateGlobalTimer();
                break;
        }
    }

    /// <summary>
    /// ���������� ������� ��� ��������� �����
    /// </summary>
    private void UpdateSinglePointTimer()
    {
        if (singleSpawnPoint == null) return;

        globalTimer += Time.deltaTime;
        float interval = GetInterval(globalIntervalType, globalSpawnInterval, globalRandomRange, globalRandomIntervals);

        if (globalTimer >= interval)
        {
            SpawnAtPoint(singleSpawnPoint.position, singleSpawnPoint.rotation);
            globalTimer = 0f;
        }
    }

    /// <summary>
    /// ���������� �������� ��� ������������� �����
    /// </summary>
    private void UpdateMultiplePointsTimers()
    {
        if (spawnPoints == null) return;

        foreach (var pointData in spawnPoints)
        {
            if (pointData.point == null) continue;

            if (!pointTimers.ContainsKey(pointData.point))
            {
                pointTimers[pointData.point] = 0f;
            }

            pointTimers[pointData.point] += Time.deltaTime;
            float interval = GetInterval(pointData.intervalType, pointData.spawnInterval, pointData.randomRange, pointData.randomIntervals);

            if (pointTimers[pointData.point] >= interval)
            {
                SpawnAtPoint(pointData.point.position, pointData.point.rotation);
                pointTimers[pointData.point] = 0f;
            }
        }
    }

    /// <summary>
    /// ���������� ����������� �������
    /// </summary>
    private void UpdateGlobalTimer()
    {
        globalTimer += Time.deltaTime;
        float interval = GetInterval(globalIntervalType, globalSpawnInterval, globalRandomRange, globalRandomIntervals);

        if (globalTimer >= interval)
        {
            SpawnObjects();
            globalTimer = 0f;
        }
    }

    /// <summary>
    /// ��������� ��������� � ����������� �� ����
    /// </summary>
    private float GetInterval(IntervalType type, float baseInterval, Vector2 range, float[] intervals)
    {
        switch (type)
        {
            case IntervalType.Fixed:
                return baseInterval;
            case IntervalType.RandomRange:
                return Random.Range(range.x, range.y);
            case IntervalType.RandomList:
                if (intervals != null && intervals.Length > 0)
                {
                    return intervals[Random.Range(0, intervals.Length)];
                }
                return baseInterval;
            default:
                return baseInterval;
        }
    }

    #endregion

    #region Spawn Methods

    /// <summary>
    /// ����� �������� � ����������� �� ���� ������
    /// </summary>
    public void SpawnObjects()
    {
        if (!isInitialized) return;

        // ��������� ���������� �� �����
        if (useActiveLimit && !isSpawningAllowed)
        {
            return;
        }

        int spawnCount = useRandomCount ?
            Random.Range(minSpawnCount, maxSpawnCount + 1) :
            minSpawnCount;

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            switch (spawnType)
            {
                case SpawnType.SinglePoint:
                    if (singleSpawnPoint != null)
                    {
                        spawnPosition = singleSpawnPoint.position;
                        spawnRotation = singleSpawnPoint.rotation;
                    }
                    break;
                case SpawnType.MultiplePoints:
                    GetRandomSpawnPoint(out spawnPosition, out spawnRotation);
                    break;
                case SpawnType.Area:
                    spawnPosition = GetRandomPositionInArea();
                    spawnRotation = Quaternion.identity;
                    break;
                case SpawnType.Path:
                    spawnPosition = transform.position;
                    spawnRotation = Quaternion.identity;
                    break;
            }

            SpawnAtPoint(spawnPosition, spawnRotation);
        }
    }

    /// <summary>
    /// ��������� ��������� ����� ������ � ������ �����
    /// </summary>
    private void GetRandomSpawnPoint(out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (spawnPoints == null || spawnPoints.Length == 0) return;

        // ��������� ����� ����� �����
        float totalWeight = 0f;
        foreach (var pointData in spawnPoints)
        {
            if (pointData.point != null)
            {
                totalWeight += pointData.weight;
            }
        }

        // �������� ��������� ����� �� ����
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var pointData in spawnPoints)
        {
            if (pointData.point == null) continue;

            currentWeight += pointData.weight;
            if (randomValue <= currentWeight)
            {
                position = pointData.point.position;
                rotation = pointData.point.rotation;
                return;
            }
        }

        // ���� ���-�� ����� �� ���, ����� ������ �����
        if (spawnPoints[0].point != null)
        {
            position = spawnPoints[0].point.position;
            rotation = spawnPoints[0].point.rotation;
        }
    }

    /// <summary>
    /// ��������� ��������� ������� � �������
    /// </summary>
    private Vector3 GetRandomPositionInArea()
    {
        Vector3 randomPosition = new Vector3(
            Random.Range(-spawnArea.extents.x, spawnArea.extents.x),
            Random.Range(-spawnArea.extents.y, spawnArea.extents.y),
            Random.Range(-spawnArea.extents.z, spawnArea.extents.z)
        );

        return spawnArea.center + randomPosition;
    }

    /// <summary>
    /// ����� ������� � ��������� �����
    /// </summary>
    private void SpawnAtPoint(Vector3 position, Quaternion rotation)
    {
        if (!ObjectPoolManager.HasInstance) return;

        // ��������� ���������� �� �����
        if (useActiveLimit && !isSpawningAllowed)
        {
            return;
        }

        GameObject obj = null;

        if (useDropSystem && dropItems != null && dropItems.Length > 0)
        {
            // ���������� ������� ���������
            obj = ObjectPoolManager.Instance.GetDropObject(dropItems, position, rotation);
        }
        else if (prefabToSpawn != null)
        {
            // ������� �����
            obj = ObjectPoolManager.Instance.GetPooledObject(prefabToSpawn, position, rotation);
        }

        if (obj != null)
        {
            OnObjectSpawned(obj);
            AddActiveObject(obj); // ��������� � ������ ��������

            // ��������� ����� ����� ����� ������
            if (useActiveLimit && activeObjects != null && activeObjects.Count >= maxActiveObjects)
            {
                isSpawningAllowed = false;
            }
        }
    }

    /// <summary>
    /// ��������� ������� ������ �������
    /// </summary>
    protected virtual void OnObjectSpawned(GameObject obj)
    {
        // ���������������� � ����������� ��� �������������� ������
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// ���������� ����� ��������
    /// </summary>
    public void ForceSpawn()
    {
        // �������������� ����� ���������� ������
        bool wasSpawningAllowed = isSpawningAllowed;
        isSpawningAllowed = true;
        SpawnObjects();
        isSpawningAllowed = wasSpawningAllowed;
    }

    /// <summary>
    /// ��������� ������� ��� ������
    /// </summary>
    public void SetPrefab(GameObject prefab)
    {
        prefabToSpawn = prefab;
        InitializePool();
    }

    /// <summary>
    /// ��������� ���� ������
    /// </summary>
    public void SetSpawnType(SpawnType type)
    {
        spawnType = type;
    }

    /// <summary>
    /// ��������� ������� �������� ��������
    /// </summary>
    public void SetActiveObjectsLimit(int min, int max)
    {
        minActiveObjects = Mathf.Max(0, min);
        maxActiveObjects = Mathf.Max(minActiveObjects, max);
    }

    /// <summary>
    /// ���������/���������� ������� �������
    /// </summary>
    public void SetActiveLimitEnabled(bool enabled)
    {
        useActiveLimit = enabled;

        if (useActiveLimit && limitCheckCoroutine == null)
        {
            limitCheckCoroutine = StartCoroutine(CheckActiveObjectsLimit());
        }
        else if (!useActiveLimit && limitCheckCoroutine != null)
        {
            StopCoroutine(limitCheckCoroutine);
            limitCheckCoroutine = null;
        }
    }

    /// <summary>
    /// ���������� ����� ������
    /// </summary>
    public void AddSpawnPoint(Transform point, float weight = 1f)
    {
        var newPointData = new SpawnPointData
        {
            point = point,
            weight = weight
        };

        var list = new List<SpawnPointData>(spawnPoints ?? new SpawnPointData[0]);
        list.Add(newPointData);
        spawnPoints = list.ToArray();

        if (pointTimers != null && point != null)
        {
            pointTimers[point] = 0f;
        }
    }

    #endregion

    #region Active Objects Management

    /// <summary>
    /// �������� ������� �������� ��������
    /// </summary>
    private IEnumerator CheckActiveObjectsLimit()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);

            if (!useActiveLimit || activeObjects == null) continue;

            // ������� ������������ ������� �� ������
            activeObjects.RemoveAll(obj => obj == null);

            // ��������� ������
            if (activeObjects.Count >= maxActiveObjects)
            {
                isSpawningAllowed = false;
            }
            else if (activeObjects.Count <= minActiveObjects)
            {
                isSpawningAllowed = true;
            }
            else
            {
                isSpawningAllowed = true;
            }
        }
    }

    /// <summary>
    /// ���������� ������� � ������ ��������
    /// </summary>
    private void AddActiveObject(GameObject obj)
    {
        if (useActiveLimit && activeObjects != null && obj != null)
        {
            activeObjects.Add(obj);

            // ��������� callback ��� ��������������� �������� �� ������
            var pooledObj = obj.GetComponent<ObjectPoolManager.PooledObject>();
            if (pooledObj != null)
            {
                pooledObj.onReturnToPool += () => RemoveActiveObject(obj);
            }
        }
    }

    /// <summary>
    /// �������� ������� �� ������ ��������
    /// </summary>
    private void RemoveActiveObject(GameObject obj)
    {
        if (useActiveLimit && activeObjects != null && obj != null)
        {
            activeObjects.Remove(obj);
        }
    }

    /// <summary>
    /// �������������� ������� �������� ��������
    /// </summary>
    public void ClearActiveObjects()
    {
        if (activeObjects != null)
        {
            foreach (var obj in activeObjects)
            {
                if (obj != null && ObjectPoolManager.HasInstance)
                {
                    ObjectPoolManager.Instance.ReturnObjectToPool(obj);
                }
            }
            activeObjects.Clear();
        }
    }

    /// <summary>
    /// ��������� �������� ���������� �������� ��������
    /// </summary>
    public int GetActiveObjectsCount()
    {
        if (activeObjects != null)
        {
            // ������� ������������ ������� ����� ���������
            activeObjects.RemoveAll(obj => obj == null);
            return activeObjects.Count;
        }
        return 0;
    }

    #endregion
}