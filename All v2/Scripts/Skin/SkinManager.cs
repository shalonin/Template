using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;

/// <summary>
/// Менеджер скинов для персонажа
/// </summary>
public class SkinManager : Singleton<SkinManager>
{
    /// <summary>
    /// Типы скинов
    /// </summary>
    public enum SkinType
    {
        Material,  // Материал для существующей модели
        Model      // Полностью новая модель (префаб)
    }

    /// <summary>
    /// Редкость скина
    /// </summary>
    public enum SkinRarity
    {
        Common,      // Обычный
        Rare,        // Редкий
        Epic,        // Эпический
        Legendary,   // Легендарный
        Mythic       // Мифический
    }

    /// <summary>
    /// Тип получения скина
    /// </summary>
    public enum SkinAcquisitionType
    {
        Shop,        // Покупка в магазине
        IAP,         // Покупка за реальные деньги
        Reward,      // Награда
        Starter,     // Стартовый скин
        Event        // Событийный
    }

    /// <summary>
    /// Данные усиления от скина
    /// </summary>
    [System.Serializable]
    public class SkinBuff
    {
        public string buffType = "Speed"; // Тип усиления
        public float buffValue = 0f;      // Значение усиления
        public bool isPercentage = true;  // Процентное или абсолютное значение

        public SkinBuff(string type, float value, bool percentage = true)
        {
            buffType = type;
            buffValue = value;
            isPercentage = percentage;
        }
    }

    /// <summary>
    /// Данные скина
    /// </summary>
    [System.Serializable]
    public class SkinData
    {
        public string skinId = "";                    // Уникальный ID скина
        public string skinName = "Default Skin";     // Название скина
        public string skinDescription = "";          // Описание скина

        public SkinType skinType = SkinType.Material; // Тип скина
        public SkinRarity rarity = SkinRarity.Common; // Редкость
        public SkinAcquisitionType acquisitionType = SkinAcquisitionType.Starter; // Как получить
        public int price = 100;                       // Цена покупки
        public string currencyType = "Coins";         // Тип валюты
        public bool canBeSold = true;                 // Можно ли продать
        public int sellPrice = 50;                    // Цена продажи

        public bool providesBuff = false;             // Даёт ли усиление
        public SkinBuff[] buffs;                      // Массив усилений

        public bool isUnlocked = false;               // Разблокирован ли скин
        public bool isEquipped = false;               // Экипирован ли скин

        public string iconName = "";                  // Имя иконки для UI

        public Material material;                     // Материал (для SkinType.Material)
        public GameObject modelPrefab;                // Префаб модели (для SkinType.Model)

        public SkinData(string id)
        {
            skinId = id;
        }
    }

    /// <summary>
    /// События системы скинов
    /// </summary>
    public static event Action<SkinData> OnSkinUnlocked;
    public static event Action<SkinData> OnSkinEquipped;
    public static event Action<SkinData> OnSkinUnequipped;
    public static event Action<SkinData, int> OnSkinSold;
    public static event Action<SkinData, int> OnSkinBought;

    [Header("Player Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string saveKey = "PlayerSkins";

    [Header("Skins Configuration")]
    [SerializeField] private SkinData[] availableSkins;

    [Header("Starter Settings")]
    [SerializeField] private string defaultSkinId = "default_material";
    [SerializeField] private bool autoUnlockStarterSkins = true;

    private Dictionary<string, SkinData> skins = new Dictionary<string, SkinData>();
    private string equippedSkinId = "";
    private GameObject playerInstance;
    private SkinnedMeshRenderer playerRenderer;
    private GameObject currentModelInstance;
    private Transform playerTransform;

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    private void Start()
    {
        FindPlayer();
        LoadPlayerSkins();
        SetupStarterSkins();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Инициализация менеджера скинов
    /// </summary>
    private void Initialize()
    {
        LoadSkins();
        //Debug.Log($"[SkinManager] Initialized with {skins.Count} skins");
    }

    /// <summary>
    /// Загрузка доступных скинов
    /// </summary>
    private void LoadSkins()
    {
        skins.Clear();

        if (availableSkins != null)
        {
            foreach (var skin in availableSkins)
            {
                if (!string.IsNullOrEmpty(skin.skinId))
                {
                    skins[skin.skinId] = skin;
                }
            }
        }

        // Убедимся что дефолтный скин существует
        if (!skins.ContainsKey(defaultSkinId))
        {
            var defaultSkin = new SkinData(defaultSkinId)
            {
                skinName = "Default",
                skinDescription = "Default player skin",
                skinType = SkinType.Material,
                rarity = SkinRarity.Common,
                acquisitionType = SkinAcquisitionType.Starter,
                isUnlocked = true,
                isEquipped = true
            };
            skins[defaultSkinId] = defaultSkin;
        }

        equippedSkinId = defaultSkinId;
    }

    /// <summary>
    /// Поиск игрока на сцене
    /// </summary>
    private void FindPlayer()
    {
        playerInstance = GameObject.FindGameObjectWithTag(playerTag);
        if (playerInstance != null)
        {
            playerTransform = playerInstance.transform;
            playerRenderer = playerInstance.GetComponentInChildren<SkinnedMeshRenderer>();
        }
    }

    /// <summary>
    /// Настройка стартовых скинов
    /// </summary>
    private void SetupStarterSkins()
    {
        if (autoUnlockStarterSkins)
        {
            foreach (var skin in skins.Values)
            {
                if (skin.acquisitionType == SkinAcquisitionType.Starter)
                {
                    UnlockSkin(skin.skinId);
                }
            }
        }

        // Экипируем дефолтный скин
        EquipSkin(defaultSkinId);
    }

    #endregion

    #region Skin Management

    /// <summary>
    /// Разблокировка скина
    /// </summary>
    public bool UnlockSkin(string skinId)
    {
        if (!skins.ContainsKey(skinId))
        {
            //Debug.LogWarning($"[SkinManager] Skin {skinId} not found");
            return false;
        }

        var skin = skins[skinId];
        if (skin.isUnlocked)
        {
            //Debug.Log($"[SkinManager] Skin {skinId} already unlocked");
            return true;
        }

        skin.isUnlocked = true;
        OnSkinUnlocked?.Invoke(skin);

        //Debug.Log($"[SkinManager] Skin unlocked: {skin.skinName}");
        SavePlayerSkins();
        return true;
    }

    /// <summary>
    /// Экипировка скина
    /// </summary>
    public bool EquipSkin(string skinId)
    {
        if (!skins.ContainsKey(skinId))
        {
            //Debug.LogWarning($"[SkinManager] Skin {skinId} not found");
            return false;
        }

        var skin = skins[skinId];
        if (!skin.isUnlocked)
        {
            //Debug.LogWarning($"[SkinManager] Skin {skinId} is not unlocked");
            return false;
        }

        // Снимаем предыдущий скин
        if (!string.IsNullOrEmpty(equippedSkinId) && skins.ContainsKey(equippedSkinId))
        {
            skins[equippedSkinId].isEquipped = false;
            OnSkinUnequipped?.Invoke(skins[equippedSkinId]);
        }

        // Экипируем новый скин
        skin.isEquipped = true;
        equippedSkinId = skinId;

        ApplySkin(skin);
        OnSkinEquipped?.Invoke(skin);

        //Debug.Log($"[SkinManager] Skin equipped: {skin.skinName}");
        SavePlayerSkins(); // Сохраняем сразу после экипировки
        return true;
    }

    /// <summary>
    /// Удаление оригинальной модели по имени
    /// </summary>
    private void DestroyOriginalModelByName(string modelName = "Character_1")
    {
        Transform skinContainer = FindSkinContainer();

        // Ищем и удаляем оригинальную модель по имени
        Transform originalModel = skinContainer.Find(modelName);
        if (originalModel != null)
        {
            //Debug.Log($"[SkinManager] Destroying original model by name: {originalModel.name}");
            Destroy(originalModel.gameObject);
        }

        // Также удаляем currentModelInstance если он существует
        if (currentModelInstance != null)
        {
            //Debug.Log($"[SkinManager] Destroying current model instance: {currentModelInstance.name}");
            Destroy(currentModelInstance);
            currentModelInstance = null;
        }
    }

    /// <summary>
    /// Скрытие оригинальной модели игрока
    /// </summary>
    private void HideOriginalModel()
    {
        if (playerInstance == null) return;

        // Скрываем все рендереры в оригинальной модели
        var renderers = playerInstance.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            // Не скрываем рендереры в дочерних объектах (новые модели)
            if (renderer.transform.parent == playerTransform)
            {
                renderer.enabled = false;
                //Debug.Log($"[SkinManager] Hiding original renderer: {renderer.name}");
            }
        }

        //Debug.Log("[SkinManager] Original model hidden");
    }

    /// <summary>
    /// Применение скина к игроку
    /// </summary>
    private void ApplySkin(SkinData skin)
    {
        if (playerInstance == null) return;

        switch (skin.skinType)
        {
            case SkinType.Material:
                ApplyMaterialSkin(skin);
                break;
            case SkinType.Model:
                ApplyModelSkin(skin);
                break;
        }
    }

    /// <summary>
    /// Применение материального скина
    /// </summary>
    private void ApplyMaterialSkin(SkinData skin)
    {
        //Debug.Log($"[SkinManager] Applying material skin: {skin.skinName}");

        // Находим контейнер для скинов
        Transform skinContainer = FindSkinContainer();

        // Ищем существующую модель в контейнере
        if (skinContainer.childCount > 0)
        {
            Transform existingModel = skinContainer.GetChild(0);
            var renderer = existingModel.GetComponent<Renderer>();
            if (renderer != null && skin.material != null)
            {
                renderer.material = skin.material;
                //Debug.Log($"[SkinManager] Material applied to existing model: {existingModel.name}");
                return;
            }
        }

        //Debug.LogWarning("[SkinManager] No existing model found for material skin");
    }

    /// <summary>
    /// Применение модельного скина
    /// </summary>
    private void ApplyModelSkin(SkinData skin)
    {
        //Debug.Log($"[SkinManager] Applying model skin: {skin.skinName}");

        if (skin.modelPrefab == null)
        {
            //Debug.LogWarning("[SkinManager] Model prefab is null");
            return;
        }

        // Находим контейнер для скинов
        Transform skinContainer = FindSkinContainer();

        // Удаляем оригинальную модель перед созданием новой
        DestroyOriginalModelByName("Character_1");

        // Создаем новую модель в контейнере
        currentModelInstance = Instantiate(skin.modelPrefab, skinContainer.position, skinContainer.rotation, skinContainer);
        currentModelInstance.name = skin.skinName;

        //Debug.Log($"[SkinManager] New skin model created: {currentModelInstance.name}");

        // Обновляем Animator в PlayerAnimationController
        UpdateAnimationController();
    }

    /// <summary>
    /// Обновление Animator в PlayerAnimationController
    /// </summary>
    private void UpdateAnimationController()
    {
        if (playerInstance == null) return;

        var animationController = playerInstance.GetComponent<PlayerAnimationController>();
        if (animationController != null)
        {
            // Даем время на инициализацию новой модели
            playerInstance.GetComponent<MonoBehaviour>().StartCoroutine(UpdateAnimatorNextFrame(animationController));
        }
    }

    /// <summary>
    /// Обновление Animator в следующем кадре
    /// </summary>
    private System.Collections.IEnumerator UpdateAnimatorNextFrame(PlayerAnimationController animationController)
    {
        yield return null; // Ждем следующий кадр

        if (currentModelInstance != null)
        {
            // Вызываем метод обновления Animator в PlayerAnimationController
            animationController.OnSkinChanged(null); // Передаем null, так как метод сам найдет Animator
            //Debug.Log("[SkinManager] Animation controller updated with new Animator");
        }
    }

    /// <summary>
    /// Поиск контейнера для скинов
    /// </summary>
    private Transform FindSkinContainer()
    {
        // Ищем PlayerModel как контейнер
        Transform container = playerTransform.Find("PlayerModel");

        if (container == null)
        {
            // Если нет PlayerModel, используем корневой объект
            container = playerTransform;
            //Debug.Log("[SkinManager] Using root player as skin container");
        }
        else
        {
            //Debug.Log($"[SkinManager] Using PlayerModel container: {container.name}");
        }

        return container;
    }

    /// <summary>
    /// Перенос компонентов управления на новую модель
    /// </summary>
    private void TransferComponents()
    {
        if (currentModelInstance == null || playerInstance == null) return;

        //Debug.Log("[SkinManager] Transferring components to new model");

        // Переносим скрипты управления
        var health = playerInstance.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.transform.SetParent(currentModelInstance.transform, false);
            //Debug.Log("[SkinManager] PlayerHealth transferred");
        }

        var deathHandler = playerInstance.GetComponent<PlayerDeathHandler>();
        if (deathHandler != null)
        {
            deathHandler.transform.SetParent(currentModelInstance.transform, false);
            //Debug.Log("[SkinManager] PlayerDeathHandler transferred");
        }

        var controller = playerInstance.GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            controller.transform.SetParent(currentModelInstance.transform, false);
            //Debug.Log("[SkinManager] ThirdPersonController transferred");
        }

        var animator = playerInstance.GetComponent<PlayerAnimationController>();
        if (animator != null)
        {
            animator.transform.SetParent(currentModelInstance.transform, false);
            //Debug.Log("[SkinManager] PlayerAnimationController transferred");
        }
    }

    /// <summary>
    /// Получение текущего экипированного скина
    /// </summary>
    public SkinData GetEquippedSkin()
    {
        if (!string.IsNullOrEmpty(equippedSkinId) && skins.ContainsKey(equippedSkinId))
        {
            return skins[equippedSkinId];
        }
        return null;
    }

    /// <summary>
    /// Получение скина по ID
    /// </summary>
    public SkinData GetSkin(string skinId)
    {
        return skins.ContainsKey(skinId) ? skins[skinId] : null;
    }

    /// <summary>
    /// Получение всех скинов
    /// </summary>
    public List<SkinData> GetAllSkins()
    {
        return new List<SkinData>(skins.Values);
    }

    /// <summary>
    /// Получение разблокированных скинов
    /// </summary>
    public List<SkinData> GetUnlockedSkins()
    {
        var unlockedSkins = new List<SkinData>();
        foreach (var skin in skins.Values)
        {
            if (skin.isUnlocked)
            {
                unlockedSkins.Add(skin);
            }
        }
        return unlockedSkins;
    }

    /// <summary>
    /// Получение скинов по редкости
    /// </summary>
    public List<SkinData> GetSkinsByRarity(SkinRarity rarity)
    {
        var raritySkins = new List<SkinData>();
        foreach (var skin in skins.Values)
        {
            if (skin.rarity == rarity)
            {
                raritySkins.Add(skin);
            }
        }
        return raritySkins;
    }

    #endregion

    #region Purchase Management

    /// <summary>
    /// Покупка скина
    /// </summary>
    public bool BuySkin(string skinId)
    {
        if (!skins.ContainsKey(skinId))
        {
            //Debug.LogWarning($"[SkinManager] Skin {skinId} not found");
            return false;
        }

        var skin = skins[skinId];
        if (skin.isUnlocked)
        {
            //Debug.LogWarning($"[SkinManager] Skin {skinId} already unlocked");
            return false;
        }

        // Проверяем возможность покупки
        if (skin.acquisitionType == SkinAcquisitionType.IAP)
        {
            //Debug.LogWarning($"[SkinManager] Skin {skinId} can only be purchased via IAP");
            return false;
        }

        // Проверяем наличие валюты (здесь должна быть интеграция с вашей валютной системой)
        if (!HasCurrency(skin.currencyType, skin.price))
        {
            //Debug.LogWarning($"[SkinManager] Not enough {skin.currencyType} to buy {skin.skinName}");
            return false;
        }

        // Списываем валюту
        SpendCurrency(skin.currencyType, skin.price);

        // Разблокируем скин
        UnlockSkin(skinId);
        OnSkinBought?.Invoke(skin, skin.price);

        //Debug.Log($"[SkinManager] Skin bought: {skin.skinName} for {skin.price} {skin.currencyType}");
        return true;
    }

    /// <summary>
    /// Продажа скина
    /// </summary>
    public bool SellSkin(string skinId)
    {
        if (!skins.ContainsKey(skinId))
        {
            //Debug.LogWarning($"[SkinManager] Skin {skinId} not found");
            return false;
        }

        var skin = skins[skinId];
        if (!skin.isUnlocked)
        {
            //Debug.LogWarning($"[SkinManager] Skin {skinId} is not unlocked");
            return false;
        }

        if (!skin.canBeSold)
        {
            //Debug.LogWarning($"[SkinManager] Skin {skinId} cannot be sold");
            return false;
        }

        if (skin.isEquipped)
        {
            //Debug.LogWarning($"[SkinManager] Cannot sell equipped skin {skinId}");
            return false;
        }

        // Добавляем валюту игроку
        AddCurrency(skin.currencyType, skin.sellPrice);

        // Снимаем разблокировку
        skin.isUnlocked = false;
        OnSkinSold?.Invoke(skin, skin.sellPrice);

        //Debug.Log($"[SkinManager] Skin sold: {skin.skinName} for {skin.sellPrice} {skin.currencyType}");
        SavePlayerSkins();
        return true;
    }

    /// <summary>
    /// Проверка наличия валюты
    /// </summary>
    private bool HasCurrency(string currencyType, int amount)
    {
        // Здесь должна быть интеграция с вашей валютной системой
        // Например: return CurrencyManager.Instance.GetCurrency(currencyType) >= amount;

        // Для примера используем PlayerPrefs
        int currentCurrency = PlayerPrefs.GetInt(currencyType, 0);
        return currentCurrency >= amount;
    }

    /// <summary>
    /// Списание валюты
    /// </summary>
    private void SpendCurrency(string currencyType, int amount)
    {
        // Здесь должна быть интеграция с вашей валютной системой
        int currentCurrency = PlayerPrefs.GetInt(currencyType, 0);
        PlayerPrefs.SetInt(currencyType, currentCurrency - amount);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Добавление валюты
    /// </summary>
    private void AddCurrency(string currencyType, int amount)
    {
        // Здесь должна быть интеграция с вашей валютной системой
        int currentCurrency = PlayerPrefs.GetInt(currencyType, 0);
        PlayerPrefs.SetInt(currencyType, currentCurrency + amount);
        PlayerPrefs.Save();
    }

    #endregion

    #region Save/Load

    /// <summary>
    /// Сохранение скинов игрока
    /// </summary>
    private void SavePlayerSkins()
    {
        var unlockedSkins = new List<string>();
        var equippedSkin = "";

        foreach (var skin in skins.Values)
        {
            if (skin.isUnlocked)
            {
                unlockedSkins.Add(skin.skinId);
            }
            if (skin.isEquipped)
            {
                equippedSkin = skin.skinId;
            }
        }

        var saveData = new SkinSaveData
        {
            unlockedSkins = unlockedSkins.ToArray(),
            equippedSkin = equippedSkin
        };

        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();

        //Debug.Log($"[SkinManager] Saved player skins. Equipped: {equippedSkin}, Unlocked: {unlockedSkins.Count}");
    }

    /// <summary>
    /// Загрузка скинов игрока
    /// </summary>
    private void LoadPlayerSkins()
    {
        if (!PlayerPrefs.HasKey(saveKey))
        {
            //Debug.Log("[SkinManager] No saved skins found");
            return;
        }

        try
        {
            string json = PlayerPrefs.GetString(saveKey);
            var saveData = JsonUtility.FromJson<SkinSaveData>(json);

            if (saveData != null)
            {
                // Восстанавливаем разблокированные скины
                if (saveData.unlockedSkins != null)
                {
                    foreach (string skinId in saveData.unlockedSkins)
                    {
                        if (skins.ContainsKey(skinId))
                        {
                            skins[skinId].isUnlocked = true;
                            //Debug.Log($"[SkinManager] Loaded unlocked skin: {skinId}");
                        }
                    }
                }

                // Восстанавливаем экипированный скин
                if (!string.IsNullOrEmpty(saveData.equippedSkin) && skins.ContainsKey(saveData.equippedSkin))
                {
                    skins[saveData.equippedSkin].isEquipped = true;
                    equippedSkinId = saveData.equippedSkin;
                    //Debug.Log($"[SkinManager] Loaded equipped skin: {saveData.equippedSkin}");

                    // Экипируем скин после загрузки
                    StartCoroutine(EquipSkinAfterLoad(saveData.equippedSkin));
                }
            }
        }
        catch (Exception e)
        {
            //Debug.LogError($"[SkinManager] Error loading player skins: {e.Message}");
        }
    }

    /// <summary>
    /// Экипировка скина после загрузки (с задержкой)
    /// </summary>
    private System.Collections.IEnumerator EquipSkinAfterLoad(string skinId)
    {
        yield return new WaitForSeconds(0.1f); // Небольшая задержка для инициализации

        FindPlayer(); // Убедимся что игрок найден (void метод)

        if (playerInstance != null)
        {
            EquipSkin(skinId);
            //Debug.Log($"[SkinManager] Skin equipped after load: {skinId}");
        }

        yield return null;
    }

    /// <summary>
    /// Вспомогательный класс для сохранения данных
    /// </summary>
    [System.Serializable]
    private class SkinSaveData
    {
        public string[] unlockedSkins;
        public string equippedSkin;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Получение цвета по редкости
    /// </summary>
    public Color GetRarityColor(SkinRarity rarity)
    {
        switch (rarity)
        {
            case SkinRarity.Common: return Color.white;
            case SkinRarity.Rare: return Color.blue;
            case SkinRarity.Epic: return new Color(0.5f, 0f, 1f); // Фиолетовый
            case SkinRarity.Legendary: return new Color(1f, 0.5f, 0f); // Оранжевый
            case SkinRarity.Mythic: return Color.yellow;
            default: return Color.white;
        }
    }

    /// <summary>
    /// Получение названия редкости
    /// </summary>
    public string GetRarityName(SkinRarity rarity)
    {
        switch (rarity)
        {
            case SkinRarity.Common: return "Common";
            case SkinRarity.Rare: return "Rare";
            case SkinRarity.Epic: return "Epic";
            case SkinRarity.Legendary: return "Legendary";
            case SkinRarity.Mythic: return "Mythic";
            default: return "Unknown";
        }
    }

    /// <summary>
    /// Проверка является ли скин эпическим или выше
    /// </summary>
    public bool IsEpicOrHigher(SkinData skin)
    {
        return skin.rarity >= SkinRarity.Epic;
    }

    #endregion
}