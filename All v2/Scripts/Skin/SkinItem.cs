using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Компонент для отображения скина в UI
/// </summary>
public class SkinItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image skinIcon;
    [SerializeField] private TextMeshProUGUI skinNameText;
    [SerializeField] private TextMeshProUGUI skinRarityText;
    [SerializeField] private TextMeshProUGUI skinPriceText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private GameObject equippedIndicator;
    [SerializeField] private GameObject lockedOverlay;

    [Header("Colors")]
    [SerializeField] private Color equippedColor = Color.green;
    [SerializeField] private Color defaultButtonColor = Color.white;

    private SkinManager.SkinData currentSkin;
    private Button currentButton;

    #region Unity Lifecycle

    private void Start()
    {
        Initialize();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Инициализация UI элемента
    /// </summary>
    private void Initialize()
    {
        currentButton = GetComponent<Button>();
        if (currentButton != null)
        {
            currentButton.onClick.AddListener(OnSkinSelected);
        }

        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnSelectButtonClicked);
        }

        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuyButtonClicked);
        }

        if (sellButton != null)
        {
            sellButton.onClick.AddListener(OnSellButtonClicked);
        }
    }

    #endregion

    #region Skin Display

    /// <summary>
    /// Настройка отображения скина
    /// </summary>
    public void SetupSkin(SkinManager.SkinData skin)
    {
        currentSkin = skin;
        UpdateDisplay();
        SubscribeToEvents();
    }

    /// <summary>
    /// Обновление отображения
    /// </summary>
    private void UpdateDisplay()
    {
        if (currentSkin == null) return;

        // Название скина
        if (skinNameText != null)
        {
            skinNameText.text = currentSkin.skinName;
        }

        // Редкость скина
        if (skinRarityText != null)
        {
            skinRarityText.text = SkinManager.Instance.GetRarityName(currentSkin.rarity);
            if (SkinManager.HasInstance)
            {
                skinRarityText.color = SkinManager.Instance.GetRarityColor(currentSkin.rarity);
            }
        }

        // Цена скина
        if (skinPriceText != null)
        {
            if (currentSkin.isUnlocked)
            {
                skinPriceText.text = "Owned";
                skinPriceText.color = Color.green;
            }
            else
            {
                skinPriceText.text = $"{currentSkin.price} {currentSkin.currencyType}";
                skinPriceText.color = Color.white;
            }
        }

        // Иконка скина
        if (skinIcon != null && !string.IsNullOrEmpty(currentSkin.iconName))
        {
            // Здесь можно загрузить иконку из Resources или AssetBundle
            // skinIcon.sprite = Resources.Load<Sprite>(currentSkin.iconName);
        }

        // Обновляем кнопки
        UpdateButtons();
    }

    /// <summary>
    /// Обновление состояния кнопок
    /// </summary>
    private void UpdateButtons()
    {
        if (currentSkin == null) return;

        // Кнопка выбора
        if (selectButton != null)
        {
            selectButton.gameObject.SetActive(currentSkin.isUnlocked);
            selectButton.interactable = !currentSkin.isEquipped;
        }

        // Кнопка покупки
        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(!currentSkin.isUnlocked &&
                currentSkin.acquisitionType != SkinManager.SkinAcquisitionType.IAP);
            buyButton.interactable = CanAffordSkin();
        }

        // Кнопка продажи
        if (sellButton != null)
        {
            sellButton.gameObject.SetActive(currentSkin.isUnlocked &&
                currentSkin.canBeSold &&
                !currentSkin.isEquipped);
        }

        // Индикатор экипировки
        if (equippedIndicator != null)
        {
            equippedIndicator.SetActive(currentSkin.isEquipped);
        }

        // Оверлей блокировки
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!currentSkin.isUnlocked);
        }

        // Основная кнопка
        if (currentButton != null)
        {
            var colors = currentButton.colors;
            if (currentSkin.isEquipped)
            {
                colors.normalColor = equippedColor;
            }
            else
            {
                colors.normalColor = defaultButtonColor;
            }
            currentButton.colors = colors;
        }
    }

    /// <summary>
    /// Проверка возможности покупки скина
    /// </summary>
    private bool CanAffordSkin()
    {
        // Здесь должна быть интеграция с валютной системой
        int currentCurrency = PlayerPrefs.GetInt(currentSkin.currencyType, 0);
        return currentCurrency >= currentSkin.price;
    }

    #endregion

    #region Event Handling

    /// <summary>
    /// Подписка на события
    /// </summary>
    private void SubscribeToEvents()
    {
        if (SkinManager.HasInstance)
        {
            SkinManager.OnSkinUnlocked += OnSkinStateChanged;
            SkinManager.OnSkinEquipped += OnSkinStateChanged;
            SkinManager.OnSkinUnequipped += OnSkinStateChanged;
            SkinManager.OnSkinBought += OnSkinBought;
            SkinManager.OnSkinSold += OnSkinSold;
        }
    }

    /// <summary>
    /// Отписка от событий
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (SkinManager.HasInstance)
        {
            SkinManager.OnSkinUnlocked -= OnSkinStateChanged;
            SkinManager.OnSkinEquipped -= OnSkinStateChanged;
            SkinManager.OnSkinUnequipped -= OnSkinStateChanged;
            SkinManager.OnSkinBought -= OnSkinBought;
            SkinManager.OnSkinSold -= OnSkinSold;
        }
    }

    /// <summary>
    /// Обработка изменения состояния скина
    /// </summary>
    private void OnSkinStateChanged(SkinManager.SkinData skin)
    {
        if (skin.skinId == currentSkin.skinId)
        {
            currentSkin = skin;
            UpdateDisplay();
        }
    }

    /// <summary>
    /// Обработка покупки скина
    /// </summary>
    private void OnSkinBought(SkinManager.SkinData skin, int price)
    {
        if (skin.skinId == currentSkin.skinId)
        {
            currentSkin = skin;
            UpdateDisplay();
        }
    }

    /// <summary>
    /// Обработка продажи скина
    /// </summary>
    private void OnSkinSold(SkinManager.SkinData skin, int price)
    {
        if (skin.skinId == currentSkin.skinId)
        {
            currentSkin = skin;
            UpdateDisplay();
        }
    }

    #endregion

    #region Button Handlers

    /// <summary>
    /// Обработка выбора скина (основная кнопка)
    /// </summary>
    private void OnSkinSelected()
    {
        if (currentSkin == null) return;

        if (currentSkin.isUnlocked)
        {
            if (!currentSkin.isEquipped)
            {
                SkinManager.Instance.EquipSkin(currentSkin.skinId);
            }
        }
        else
        {
            if (currentSkin.acquisitionType != SkinManager.SkinAcquisitionType.IAP)
            {
                SkinManager.Instance.BuySkin(currentSkin.skinId);
            }
        }
    }

    /// <summary>
    /// Обработка нажатия кнопки выбора
    /// </summary>
    private void OnSelectButtonClicked()
    {
        if (currentSkin != null && currentSkin.isUnlocked && !currentSkin.isEquipped)
        {
            SkinManager.Instance.EquipSkin(currentSkin.skinId);
        }
    }

    /// <summary>
    /// Обработка нажатия кнопки покупки
    /// </summary>
    private void OnBuyButtonClicked()
    {
        if (currentSkin != null && !currentSkin.isUnlocked)
        {
            SkinManager.Instance.BuySkin(currentSkin.skinId);
        }
    }

    /// <summary>
    /// Обработка нажатия кнопки продажи
    /// </summary>
    private void OnSellButtonClicked()
    {
        if (currentSkin != null && currentSkin.isUnlocked && currentSkin.canBeSold && !currentSkin.isEquipped)
        {
            SkinManager.Instance.SellSkin(currentSkin.skinId);
        }
    }

    #endregion

    #region Unity Events

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    #endregion
}