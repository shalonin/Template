using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Менеджер магазина скинов
/// </summary>
public class SkinShopManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform skinContainer;
    [SerializeField] private GameObject skinItemPrefab;
    [SerializeField] private GameObject shopPanel;

    [Header("Filter Settings")]
    [SerializeField] private bool showOnlyUnlocked = false;
    [SerializeField] private bool showOnlyLocked = false;
    [SerializeField] private SkinManager.SkinRarity rarityFilter = SkinManager.SkinRarity.Common;

    private List<SkinItem> skinItems = new List<SkinItem>();

    #region Unity Lifecycle

    private void Start()
    {
        Initialize();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Инициализация магазина
    /// </summary>
    private void Initialize()
    {
        if (SkinManager.HasInstance)
        {
            RefreshShop();
        }
    }

    #endregion

    #region Shop Management

    /// <summary>
    /// Обновление магазина
    /// </summary>
    public void RefreshShop()
    {
        ClearShop();
        CreateSkinItems();
    }

    /// <summary>
    /// Создание элементов скинов
    /// </summary>
    private void CreateSkinItems()
    {
        if (!SkinManager.HasInstance || skinContainer == null || skinItemPrefab == null) return;

        var allSkins = SkinManager.Instance.GetAllSkins();

        foreach (var skin in allSkins)
        {
            // Применяем фильтры
            if (ShouldShowSkin(skin))
            {
                CreateSkinItem(skin);
            }
        }
    }

    /// <summary>
    /// Проверка должен ли скин отображаться
    /// </summary>
    private bool ShouldShowSkin(SkinManager.SkinData skin)
    {
        // Фильтр по разблокировке
        if (showOnlyUnlocked && !skin.isUnlocked) return false;
        if (showOnlyLocked && skin.isUnlocked) return false;

        // Фильтр по редкости
        if (rarityFilter != SkinManager.SkinRarity.Common && skin.rarity != rarityFilter) return false;

        return true;
    }

    /// <summary>
    /// Создание UI элемента скина
    /// </summary>
    private void CreateSkinItem(SkinManager.SkinData skin)
    {
        var itemGO = Instantiate(skinItemPrefab, skinContainer);
        var skinItem = itemGO.GetComponent<SkinItem>();

        if (skinItem != null)
        {
            skinItem.SetupSkin(skin);
            skinItems.Add(skinItem);
        }
    }

    /// <summary>
    /// Очистка магазина
    /// </summary>
    private void ClearShop()
    {
        foreach (var item in skinItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
        }
        skinItems.Clear();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Показ магазина
    /// </summary>
    public void ShowShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            RefreshShop();
        }
    }

    /// <summary>
    /// Скрытие магазина
    /// </summary>
    public void HideShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Установка фильтра редкости
    /// </summary>
    public void SetRarityFilter(SkinManager.SkinRarity rarity)
    {
        rarityFilter = rarity;
        RefreshShop();
    }

    /// <summary>
    /// Установка фильтра разблокированных скинов
    /// </summary>
    public void SetUnlockedFilter(bool showUnlocked)
    {
        showOnlyUnlocked = showUnlocked;
        showOnlyLocked = !showUnlocked;
        RefreshShop();
    }

    #endregion
}