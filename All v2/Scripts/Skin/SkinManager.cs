using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;

/// <summary>
/// �������� ������ ��� ���������
/// </summary>
public class SkinManager : Singleton<SkinManager>
{
    /// <summary>
    /// ���� ������
    /// </summary>
    public enum SkinType
    {
        Material,  // �������� ��� ������������ ������
        Model      // ��������� ����� ������ (������)
    }

    /// <summary>
    /// �������� �����
    /// </summary>
    public enum SkinRarity
    {
        Common,      // �������
        Rare,        // ������
        Epic,        // ���������
        Legendary,   // �����������
        Mythic       // ����������
    }

    /// <summary>
    /// ��� ��������� �����
    /// </summary>
    public enum SkinAcquisitionType
    {
        Shop,        // ������� � ��������
        IAP,         // ������� �� �������� ������
        Reward,      // �������
        Starter,     // ��������� ����
        Event        // ����������
    }

    /// <summary>
    /// ������ �������� �� �����
    /// </summary>
    [System.Serializable]
    public class SkinBuff
    {
        public string buffType = "Speed"; // ��� ��������
        public float buffValue = 0f;      // �������� ��������
        public bool isPercentage = true;  // ���������� ��� ���������� ��������

        public SkinBuff(string type, float value, bool percentage = true)
        {
            buffType = type;
            buffValue = value;
            isPercentage = percentage;
        }
    }

    /// <summary>
    /// ������ �����
    /// </summary>
    [System.Serializable]
    public class SkinData
    {
        public string skinId = "";                    // ���������� ID �����
        public string skinName = "Default Skin";     // �������� �����
        public string skinDescription = "";          // �������� �����

        public SkinType skinType = SkinType.Material; // ��� �����
        public SkinRarity rarity = SkinRarity.Common; // ��������
        public SkinAcquisitionType acquisitionType = SkinAcquisitionType.Starter; // ��� ��������
        public int price = 100;                       // ���� �������
        public string currencyType = "Coins";         // ��� ������
        public bool canBeSold = true;                 // ����� �� �������
        public int sellPrice = 50;                    // ���� �������

        public bool providesBuff = false;             // ��� �� ��������
        public SkinBuff[] buffs;                      // ������ ��������

        public bool isUnlocked = false;               // ������������� �� ����
        public bool isEquipped = false;               // ���������� �� ����

        public string iconName = "";                  // ��� ������ ��� UI

        public Material material;                     // �������� (��� SkinType.Material)
        public GameObject modelPrefab;                // ������ ������ (��� SkinType.Model)

        public SkinData(string id)
        {
            skinId = id;
        }
    }

    /// <summary>
    /// ������� ������� ������
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
    /// ������������� ��������� ������
    /// </summary>
    private void Initialize()
    {
        LoadSkins();
        //Debug.Log($"[SkinManager] Initialized with {skins.Count} skins");
    }

    /// <summary>
    /// �������� ��������� ������
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

        // �������� ��� ��������� ���� ����������
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
    /// ����� ������ �� �����
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
    /// ��������� ��������� ������
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

        // ��������� ��������� ����
        EquipSkin(defaultSkinId);
    }

    #endregion

    #region Skin Management

    /// <summary>
    /// ������������� �����
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
    /// ���������� �����
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

        // ������� ���������� ����
        if (!string.IsNullOrEmpty(equippedSkinId) && skins.ContainsKey(equippedSkinId))
        {
            skins[equippedSkinId].isEquipped = false;
            OnSkinUnequipped?.Invoke(skins[equippedSkinId]);
        }

        // ��������� ����� ����
        skin.isEquipped = true;
        equippedSkinId = skinId;

        ApplySkin(skin);
        OnSkinEquipped?.Invoke(skin);

        //Debug.Log($"[SkinManager] Skin equipped: {skin.skinName}");
        SavePlayerSkins(); // ��������� ����� ����� ����������
        return true;
    }

    /// <summary>
    /// �������� ������������ ������ �� �����
    /// </summary>
    private void DestroyOriginalModelByName(string modelName = "Character_1")
    {
        Transform skinContainer = FindSkinContainer();

        // ���� � ������� ������������ ������ �� �����
        Transform originalModel = skinContainer.Find(modelName);
        if (originalModel != null)
        {
            //Debug.Log($"[SkinManager] Destroying original model by name: {originalModel.name}");
            Destroy(originalModel.gameObject);
        }

        // ����� ������� currentModelInstance ���� �� ����������
        if (currentModelInstance != null)
        {
            //Debug.Log($"[SkinManager] Destroying current model instance: {currentModelInstance.name}");
            Destroy(currentModelInstance);
            currentModelInstance = null;
        }
    }

    /// <summary>
    /// ������� ������������ ������ ������
    /// </summary>
    private void HideOriginalModel()
    {
        if (playerInstance == null) return;

        // �������� ��� ��������� � ������������ ������
        var renderers = playerInstance.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            // �� �������� ��������� � �������� �������� (����� ������)
            if (renderer.transform.parent == playerTransform)
            {
                renderer.enabled = false;
                //Debug.Log($"[SkinManager] Hiding original renderer: {renderer.name}");
            }
        }

        //Debug.Log("[SkinManager] Original model hidden");
    }

    /// <summary>
    /// ���������� ����� � ������
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
    /// ���������� ������������� �����
    /// </summary>
    private void ApplyMaterialSkin(SkinData skin)
    {
        //Debug.Log($"[SkinManager] Applying material skin: {skin.skinName}");

        // ������� ��������� ��� ������
        Transform skinContainer = FindSkinContainer();

        // ���� ������������ ������ � ����������
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
    /// ���������� ���������� �����
    /// </summary>
    private void ApplyModelSkin(SkinData skin)
    {
        //Debug.Log($"[SkinManager] Applying model skin: {skin.skinName}");

        if (skin.modelPrefab == null)
        {
            //Debug.LogWarning("[SkinManager] Model prefab is null");
            return;
        }

        // ������� ��������� ��� ������
        Transform skinContainer = FindSkinContainer();

        // ������� ������������ ������ ����� ��������� �����
        DestroyOriginalModelByName("Character_1");

        // ������� ����� ������ � ����������
        currentModelInstance = Instantiate(skin.modelPrefab, skinContainer.position, skinContainer.rotation, skinContainer);
        currentModelInstance.name = skin.skinName;

        //Debug.Log($"[SkinManager] New skin model created: {currentModelInstance.name}");

        // ��������� Animator � PlayerAnimationController
        UpdateAnimationController();
    }

    /// <summary>
    /// ���������� Animator � PlayerAnimationController
    /// </summary>
    private void UpdateAnimationController()
    {
        if (playerInstance == null) return;

        var animationController = playerInstance.GetComponent<PlayerAnimationController>();
        if (animationController != null)
        {
            // ���� ����� �� ������������� ����� ������
            playerInstance.GetComponent<MonoBehaviour>().StartCoroutine(UpdateAnimatorNextFrame(animationController));
        }
    }

    /// <summary>
    /// ���������� Animator � ��������� �����
    /// </summary>
    private System.Collections.IEnumerator UpdateAnimatorNextFrame(PlayerAnimationController animationController)
    {
        yield return null; // ���� ��������� ����

        if (currentModelInstance != null)
        {
            // �������� ����� ���������� Animator � PlayerAnimationController
            animationController.OnSkinChanged(null); // �������� null, ��� ��� ����� ��� ������ Animator
            //Debug.Log("[SkinManager] Animation controller updated with new Animator");
        }
    }

    /// <summary>
    /// ����� ���������� ��� ������
    /// </summary>
    private Transform FindSkinContainer()
    {
        // ���� PlayerModel ��� ���������
        Transform container = playerTransform.Find("PlayerModel");

        if (container == null)
        {
            // ���� ��� PlayerModel, ���������� �������� ������
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
    /// ������� ����������� ���������� �� ����� ������
    /// </summary>
    private void TransferComponents()
    {
        if (currentModelInstance == null || playerInstance == null) return;

        //Debug.Log("[SkinManager] Transferring components to new model");

        // ��������� ������� ����������
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
    /// ��������� �������� �������������� �����
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
    /// ��������� ����� �� ID
    /// </summary>
    public SkinData GetSkin(string skinId)
    {
        return skins.ContainsKey(skinId) ? skins[skinId] : null;
    }

    /// <summary>
    /// ��������� ���� ������
    /// </summary>
    public List<SkinData> GetAllSkins()
    {
        return new List<SkinData>(skins.Values);
    }

    /// <summary>
    /// ��������� ���������������� ������
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
    /// ��������� ������ �� ��������
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
    /// ������� �����
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

        // ��������� ����������� �������
        if (skin.acquisitionType == SkinAcquisitionType.IAP)
        {
            //Debug.LogWarning($"[SkinManager] Skin {skinId} can only be purchased via IAP");
            return false;
        }

        // ��������� ������� ������ (����� ������ ���� ���������� � ����� �������� ��������)
        if (!HasCurrency(skin.currencyType, skin.price))
        {
            //Debug.LogWarning($"[SkinManager] Not enough {skin.currencyType} to buy {skin.skinName}");
            return false;
        }

        // ��������� ������
        SpendCurrency(skin.currencyType, skin.price);

        // ������������ ����
        UnlockSkin(skinId);
        OnSkinBought?.Invoke(skin, skin.price);

        //Debug.Log($"[SkinManager] Skin bought: {skin.skinName} for {skin.price} {skin.currencyType}");
        return true;
    }

    /// <summary>
    /// ������� �����
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

        // ��������� ������ ������
        AddCurrency(skin.currencyType, skin.sellPrice);

        // ������� �������������
        skin.isUnlocked = false;
        OnSkinSold?.Invoke(skin, skin.sellPrice);

        //Debug.Log($"[SkinManager] Skin sold: {skin.skinName} for {skin.sellPrice} {skin.currencyType}");
        SavePlayerSkins();
        return true;
    }

    /// <summary>
    /// �������� ������� ������
    /// </summary>
    private bool HasCurrency(string currencyType, int amount)
    {
        // ����� ������ ���� ���������� � ����� �������� ��������
        // ��������: return CurrencyManager.Instance.GetCurrency(currencyType) >= amount;

        // ��� ������� ���������� PlayerPrefs
        int currentCurrency = PlayerPrefs.GetInt(currencyType, 0);
        return currentCurrency >= amount;
    }

    /// <summary>
    /// �������� ������
    /// </summary>
    private void SpendCurrency(string currencyType, int amount)
    {
        // ����� ������ ���� ���������� � ����� �������� ��������
        int currentCurrency = PlayerPrefs.GetInt(currencyType, 0);
        PlayerPrefs.SetInt(currencyType, currentCurrency - amount);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// ���������� ������
    /// </summary>
    private void AddCurrency(string currencyType, int amount)
    {
        // ����� ������ ���� ���������� � ����� �������� ��������
        int currentCurrency = PlayerPrefs.GetInt(currencyType, 0);
        PlayerPrefs.SetInt(currencyType, currentCurrency + amount);
        PlayerPrefs.Save();
    }

    #endregion

    #region Save/Load

    /// <summary>
    /// ���������� ������ ������
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
    /// �������� ������ ������
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
                // ��������������� ���������������� �����
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

                // ��������������� ������������� ����
                if (!string.IsNullOrEmpty(saveData.equippedSkin) && skins.ContainsKey(saveData.equippedSkin))
                {
                    skins[saveData.equippedSkin].isEquipped = true;
                    equippedSkinId = saveData.equippedSkin;
                    //Debug.Log($"[SkinManager] Loaded equipped skin: {saveData.equippedSkin}");

                    // ��������� ���� ����� ��������
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
    /// ���������� ����� ����� �������� (� ���������)
    /// </summary>
    private System.Collections.IEnumerator EquipSkinAfterLoad(string skinId)
    {
        yield return new WaitForSeconds(0.1f); // ��������� �������� ��� �������������

        FindPlayer(); // �������� ��� ����� ������ (void �����)

        if (playerInstance != null)
        {
            EquipSkin(skinId);
            //Debug.Log($"[SkinManager] Skin equipped after load: {skinId}");
        }

        yield return null;
    }

    /// <summary>
    /// ��������������� ����� ��� ���������� ������
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
    /// ��������� ����� �� ��������
    /// </summary>
    public Color GetRarityColor(SkinRarity rarity)
    {
        switch (rarity)
        {
            case SkinRarity.Common: return Color.white;
            case SkinRarity.Rare: return Color.blue;
            case SkinRarity.Epic: return new Color(0.5f, 0f, 1f); // ����������
            case SkinRarity.Legendary: return new Color(1f, 0.5f, 0f); // ���������
            case SkinRarity.Mythic: return Color.yellow;
            default: return Color.white;
        }
    }

    /// <summary>
    /// ��������� �������� ��������
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
    /// �������� �������� �� ���� ��������� ��� ����
    /// </summary>
    public bool IsEpicOrHigher(SkinData skin)
    {
        return skin.rarity >= SkinRarity.Epic;
    }

    #endregion
}