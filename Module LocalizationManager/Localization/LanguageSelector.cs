using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Компонент для выбора языка в UI
/// </summary>
public class LanguageSelector : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Dropdown languageDropdown;
    [SerializeField] private Button applyButton;
    [SerializeField] private bool autoApply = true;

    [Header("Display Settings")]
    [SerializeField] private bool showLanguageCodes = false;
    [SerializeField] private bool showNativeNames = true;

    private List<string> availableLanguages;
    private int selectedLanguageIndex = 0;

    #region Unity Lifecycle

    private void Start()
    {
        Initialize();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Инициализация селектора языков
    /// </summary>
    private void Initialize()
    {
        if (!LocalizationManager.HasInstance) return;

        SetupDropdown();
        SetupButtons();
        LoadCurrentLanguage();
    }

    /// <summary>
    /// Настройка выпадающего списка
    /// </summary>
    private void SetupDropdown()
    {
        if (languageDropdown == null) return;

        availableLanguages = LocalizationManager.Instance.GetAvailableLanguages();
        var dropdownOptions = new List<Dropdown.OptionData>();

        foreach (string langCode in availableLanguages)
        {
            string displayName = GetLanguageDisplayName(langCode);
            dropdownOptions.Add(new Dropdown.OptionData(displayName));
        }

        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(dropdownOptions);
        languageDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    /// <summary>
    /// Настройка кнопок
    /// </summary>
    private void SetupButtons()
    {
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(ApplyLanguage);
        }
    }

    /// <summary>
    /// Загрузка текущего языка
    /// </summary>
    private void LoadCurrentLanguage()
    {
        if (!LocalizationManager.HasInstance) return;

        string currentLang = LocalizationManager.Instance.GetCurrentLanguage();
        int index = availableLanguages.IndexOf(currentLang);

        if (index >= 0)
        {
            selectedLanguageIndex = index;
            languageDropdown.value = index;
        }
    }

    #endregion

    #region Event Handling

    /// <summary>
    /// Обработка изменения значения в выпадающем списке
    /// </summary>
    private void OnDropdownValueChanged(int index)
    {
        selectedLanguageIndex = index;

        if (autoApply)
        {
            ApplyLanguage();
        }
    }

    /// <summary>
    /// Применение выбранного языка
    /// </summary>
    public void ApplyLanguage()
    {
        if (selectedLanguageIndex >= 0 && selectedLanguageIndex < availableLanguages.Count)
        {
            string selectedLanguage = availableLanguages[selectedLanguageIndex];
            if (LocalizationManager.HasInstance)
            {
                LocalizationManager.Instance.SetLanguage(selectedLanguage);
            }
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Получение отображаемого имени языка
    /// </summary>
    private string GetLanguageDisplayName(string languageCode)
    {
        if (!LocalizationManager.HasInstance) return languageCode;

        string languageName = LocalizationManager.Instance.GetLanguageName(languageCode);

        if (showNativeNames)
        {
            // Здесь можно добавить нативные названия языков
            switch (languageCode.ToLower())
            {
                case "en": return showLanguageCodes ? "English (EN)" : "English";
                case "ru": return showLanguageCodes ? "Русский (RU)" : "Русский";
                case "es": return showLanguageCodes ? "Español (ES)" : "Español";
                case "fr": return showLanguageCodes ? "Français (FR)" : "Français";
                case "de": return showLanguageCodes ? "Deutsch (DE)" : "Deutsch";
                default: return showLanguageCodes ? $"{languageName} ({languageCode.ToUpper()})" : languageName;
            }
        }

        return showLanguageCodes ? $"{languageName} ({languageCode.ToUpper()})" : languageName;
    }

    /// <summary>
    /// Обновление списка языков
    /// </summary>
    public void RefreshLanguages()
    {
        if (LocalizationManager.HasInstance)
        {
            SetupDropdown();
            LoadCurrentLanguage();
        }
    }

    #endregion
}