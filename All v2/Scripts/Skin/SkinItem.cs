using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ��������� ��� ����������� ����� � UI
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
    /// ������������� UI ��������
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
    /// ��������� ����������� �����
    /// </summary>
    public void SetupSkin(SkinManager.SkinData skin)
    {
        currentSkin = skin;
        UpdateDisplay();
        SubscribeToEvents();
    }

    /// <summary>
    /// ���������� �����������
    /// </summary>
    private void UpdateDisplay()
    {
        if (currentSkin == null) return;

        // �������� �����
        if (skinNameText != null)
        {
            skinNameText.text = currentSkin.skinName;
        }

        // �������� �����
        if (skinRarityText != null)
        {
            skinRarityText.text = SkinManager.Instance.GetRarityName(currentSkin.rarity);
            if (SkinManager.HasInstance)
            {
                skinRarityText.color = SkinManager.Instance.GetRarityColor(currentSkin.rarity);
            }
        }

        // ���� �����
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

        // ������ �����
        if (skinIcon != null && !string.IsNullOrEmpty(currentSkin.iconName))
        {
            // ����� ����� ��������� ������ �� Resources ��� AssetBundle
            // skinIcon.sprite = Resources.Load<Sprite>(currentSkin.iconName);
        }

        // ��������� ������
        UpdateButtons();
    }

    /// <summary>
    /// ���������� ��������� ������
    /// </summary>
    private void UpdateButtons()
    {
        if (currentSkin == null) return;

        // ������ ������
        if (selectButton != null)
        {
            selectButton.gameObject.SetActive(currentSkin.isUnlocked);
            selectButton.interactable = !currentSkin.isEquipped;
        }

        // ������ �������
        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(!currentSkin.isUnlocked &&
                currentSkin.acquisitionType != SkinManager.SkinAcquisitionType.IAP);
            buyButton.interactable = CanAffordSkin();
        }

        // ������ �������
        if (sellButton != null)
        {
            sellButton.gameObject.SetActive(currentSkin.isUnlocked &&
                currentSkin.canBeSold &&
                !currentSkin.isEquipped);
        }

        // ��������� ����������
        if (equippedIndicator != null)
        {
            equippedIndicator.SetActive(currentSkin.isEquipped);
        }

        // ������� ����������
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!currentSkin.isUnlocked);
        }

        // �������� ������
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
    /// �������� ����������� ������� �����
    /// </summary>
    private bool CanAffordSkin()
    {
        // ����� ������ ���� ���������� � �������� ��������
        int currentCurrency = PlayerPrefs.GetInt(currentSkin.currencyType, 0);
        return currentCurrency >= currentSkin.price;
    }

    #endregion

    #region Event Handling

    /// <summary>
    /// �������� �� �������
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
    /// ������� �� �������
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
    /// ��������� ��������� ��������� �����
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
    /// ��������� ������� �����
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
    /// ��������� ������� �����
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
    /// ��������� ������ ����� (�������� ������)
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
    /// ��������� ������� ������ ������
    /// </summary>
    private void OnSelectButtonClicked()
    {
        if (currentSkin != null && currentSkin.isUnlocked && !currentSkin.isEquipped)
        {
            SkinManager.Instance.EquipSkin(currentSkin.skinId);
        }
    }

    /// <summary>
    /// ��������� ������� ������ �������
    /// </summary>
    private void OnBuyButtonClicked()
    {
        if (currentSkin != null && !currentSkin.isUnlocked)
        {
            SkinManager.Instance.BuySkin(currentSkin.skinId);
        }
    }

    /// <summary>
    /// ��������� ������� ������ �������
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