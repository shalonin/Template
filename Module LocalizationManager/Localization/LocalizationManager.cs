using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System;

/// <summary>
/// Менеджер локализации для мультиязычных игр
/// </summary>
public class LocalizationManager : Singleton<LocalizationManager>
{
    /// <summary>
    /// Вспомогательный класс для сериализации переводов
    /// </summary>
    [Serializable]
    public class TranslationEntry
    {
        public string key;
        public string value;

        public TranslationEntry(string k, string v)
        {
            key = k;
            value = v;
        }
    }

    /// <summary>
    /// Данные локализации для одного языка
    /// </summary>
    [Serializable]
    public class LanguageData
    {
        public string languageCode = "en";
        public string languageName = "English";
        public TranslationEntry[] translations; // Массив вместо Dictionary для сериализации

        // Внутренний словарь для быстрого доступа
        [NonSerialized]
        public Dictionary<string, string> translationDict;

        public LanguageData()
        {
            translationDict = new Dictionary<string, string>();
        }

        /// <summary>
        /// Инициализация словаря из массива переводов
        /// </summary>
        public void InitializeDictionary()
        {
            translationDict = new Dictionary<string, string>();
            if (translations != null)
            {
                foreach (var entry in translations)
                {
                    if (!string.IsNullOrEmpty(entry.key))
                    {
                        translationDict[entry.key] = entry.value ?? "";
                    }
                }
            }
        }
    }

    /// <summary>
    /// Вспомогательный класс для десериализации JSON
    /// </summary>
    [Serializable]
    private class LanguageDataWrapper
    {
        public string languageCode;
        public string languageName;
        public TranslationEntry[] translations;
    }

    /// <summary>
    /// Событие изменения языка
    /// </summary>
    public static event Action<string> OnLanguageChanged;

    [Header("Language Settings")]
    [SerializeField] private string defaultLanguage = "en";
    [SerializeField] private bool autoDetectSystemLanguage = true;
    [SerializeField] private bool saveLanguagePreference = true;
    [SerializeField] private string saveKey = "PlayerLanguage";

    [Header("Text Assets")]
    [SerializeField] private TextAsset[] languageFiles;

    private Dictionary<string, LanguageData> languages = new Dictionary<string, LanguageData>();
    private string currentLanguage = "en";
    private bool isInitialized = false;

    #region Unity Lifecycle

    protected override void Awake()
    {
        base.Awake();
        Initialize();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Инициализация менеджера локализации
    /// </summary>
    private void Initialize()
    {
        if (isInitialized) return;

        LoadLanguages();
        SetInitialLanguage();

        isInitialized = true;
        Debug.Log($"[LocalizationManager] Initialized with {languages.Count} languages");
    }

    /// <summary>
    /// Загрузка всех языковых файлов
    /// </summary>
    private void LoadLanguages()
    {
        languages.Clear();

        if (languageFiles != null && languageFiles.Length > 0)
        {
            foreach (var textAsset in languageFiles)
            {
                if (textAsset != null)
                {
                    try
                    {
                        var wrapper = JsonUtility.FromJson<LanguageDataWrapper>(textAsset.text);
                        if (wrapper != null && !string.IsNullOrEmpty(wrapper.languageCode))
                        {
                            var languageData = new LanguageData
                            {
                                languageCode = wrapper.languageCode,
                                languageName = wrapper.languageName ?? wrapper.languageCode,
                                translations = wrapper.translations
                            };

                            languageData.InitializeDictionary();
                            languages[languageData.languageCode] = languageData;

                            Debug.Log($"[LocalizationManager] Loaded language: {languageData.languageName} ({languageData.languageCode}) with {languageData.translationDict.Count} translations");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[LocalizationManager] Error loading language file {textAsset.name}: {e.Message}");
                    }
                }
            }
        }
        else
        {
            CreateDefaultLanguage();
        }
    }

    /// <summary>
    /// Создание дефолтного языка
    /// </summary>
    private void CreateDefaultLanguage()
    {
        var defaultLang = new LanguageData
        {
            languageCode = "en",
            languageName = "English",
            translations = new TranslationEntry[]
            {
                new TranslationEntry("welcome", "Welcome"),
                new TranslationEntry("start_game", "Start Game"),
                new TranslationEntry("settings", "Settings"),
                new TranslationEntry("quit", "Quit")
            }
        };

        defaultLang.InitializeDictionary();
        languages["en"] = defaultLang;
    }

    /// <summary>
    /// Установка начального языка
    /// </summary>
    private void SetInitialLanguage()
    {
        string languageToUse = defaultLanguage;

        if (saveLanguagePreference && PlayerPrefs.HasKey(saveKey))
        {
            languageToUse = PlayerPrefs.GetString(saveKey);
        }
        else if (autoDetectSystemLanguage)
        {
            languageToUse = GetSystemLanguage();
        }

        SetLanguage(languageToUse);
    }

    /// <summary>
    /// Получение языка системы
    /// </summary>
    private string GetSystemLanguage()
    {
        try
        {
            string systemLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();

            if (languages.ContainsKey(systemLang))
            {
                return systemLang;
            }

            switch (systemLang)
            {
                case "ru": return "ru";
                case "en": return "en";
                default: return defaultLanguage;
            }
        }
        catch
        {
            return defaultLanguage;
        }
    }

    #endregion

    #region Language Management

    /// <summary>
    /// Установка языка
    /// </summary>
    public void SetLanguage(string languageCode)
    {
        if (!languages.ContainsKey(languageCode))
        {
            Debug.LogWarning($"[LocalizationManager] Language {languageCode} not found, using default");
            languageCode = defaultLanguage;
        }

        currentLanguage = languageCode;

        if (saveLanguagePreference)
        {
            PlayerPrefs.SetString(saveKey, currentLanguage);
            PlayerPrefs.Save();
        }

        OnLanguageChanged?.Invoke(currentLanguage);
        Debug.Log($"[LocalizationManager] Language changed to: {GetLanguageName(currentLanguage)}");
    }

    /// <summary>
    /// Получение перевода по ключу
    /// </summary>
    public string GetTranslation(string key, params object[] args)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        if (!languages.ContainsKey(currentLanguage))
        {
            Debug.LogWarning($"[LocalizationManager] Current language {currentLanguage} not found");
            return key;
        }

        var currentLang = languages[currentLanguage];

        if (currentLang.translationDict == null)
        {
            Debug.LogWarning($"[LocalizationManager] Translation dictionary is null for language {currentLanguage}");
            return key;
        }

        if (currentLang.translationDict.ContainsKey(key))
        {
            string translation = currentLang.translationDict[key];

            if (args != null && args.Length > 0)
            {
                try
                {
                    translation = string.Format(translation, args);
                }
                catch (FormatException)
                {
                    Debug.LogWarning($"[LocalizationManager] Format error for key '{key}' with args");
                }
            }

            return translation;
        }

        Debug.LogWarning($"[LocalizationManager] Translation not found for key: {key} in language: {currentLanguage}");
        return key;
    }

    /// <summary>
    /// Получение текущего языка
    /// </summary>
    public string GetCurrentLanguage()
    {
        return currentLanguage;
    }

    /// <summary>
    /// Получение названия языка
    /// </summary>
    public string GetLanguageName(string languageCode)
    {
        if (languages.ContainsKey(languageCode))
        {
            return languages[languageCode].languageName;
        }
        return languageCode;
    }

    /// <summary>
    /// Получение списка доступных языков
    /// </summary>
    public List<string> GetAvailableLanguages()
    {
        return new List<string>(languages.Keys);
    }

    /// <summary>
    /// Проверка поддерживает ли язык игра
    /// </summary>
    public bool IsLanguageSupported(string languageCode)
    {
        return languages.ContainsKey(languageCode);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Добавление нового языка в runtime
    /// </summary>
    public void AddLanguage(LanguageData languageData)
    {
        if (languageData != null && !string.IsNullOrEmpty(languageData.languageCode))
        {
            languageData.InitializeDictionary();
            languages[languageData.languageCode] = languageData;
            Debug.Log($"[LocalizationManager] Added language: {languageData.languageName}");
        }
    }

    /// <summary>
    /// Обновление перевода для ключа
    /// </summary>
    public void UpdateTranslation(string languageCode, string key, string translation)
    {
        if (languages.ContainsKey(languageCode))
        {
            var lang = languages[languageCode];
            lang.translationDict[key] = translation;

            // Обновляем массив для сериализации
            var list = new List<TranslationEntry>(lang.translations ?? new TranslationEntry[0]);
            var existingEntry = list.Find(e => e.key == key);
            if (existingEntry != null)
            {
                existingEntry.value = translation;
            }
            else
            {
                list.Add(new TranslationEntry(key, translation));
            }
            lang.translations = list.ToArray();
        }
    }

    /// <summary>
    /// Получение количества доступных языков
    /// </summary>
    public int GetLanguagesCount()
    {
        return languages.Count;
    }

    #endregion
}