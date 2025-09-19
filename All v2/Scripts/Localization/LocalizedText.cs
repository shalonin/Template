using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ��������� ��� �������������� ����������� UI �������
/// </summary>
//[RequireComponent(typeof(TMP_Text))] // ����� ���� Text, TextMeshProUGUI ��� ������
public class LocalizedText : MonoBehaviour
{
    [Header("Localization Settings")]
    [SerializeField] private string localizationKey = "";
    [SerializeField] private bool autoUpdate = true;
    [SerializeField] private object[] formatArguments; // ��������� ��� string.Format

    private Text uiText;
    [SerializeField] private TMP_Text tmpText;
    private bool isInitialized = false;

    #region Unity Lifecycle

    private void Start()
    {
        Initialize();
        UpdateText();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// ������������� �����������
    /// </summary>
    private void Initialize()
    {
        if (isInitialized) return;

        uiText = GetComponent<Text>();
        tmpText = GetComponent<TMP_Text>();

        isInitialized = true;
    }

    /// <summary>
    /// �������� �� ������� �����������
    /// </summary>
    private void SubscribeToEvents()
    {
        if (autoUpdate && LocalizationManager.HasInstance)
        {
            LocalizationManager.OnLanguageChanged += OnLanguageChanged;
        }
    }

    /// <summary>
    /// ������� �� ������� �����������
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (LocalizationManager.HasInstance)
        {
            LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    #endregion

    #region Event Handling

    /// <summary>
    /// ��������� ��������� �����
    /// </summary>
    private void OnLanguageChanged(string languageCode)
    {
        if (autoUpdate)
        {
            UpdateText();
        }
    }

    #endregion

    #region Text Management

    /// <summary>
    /// ���������� ������
    /// </summary>
    public void UpdateText()
    {
        if (!isInitialized) Initialize();

        if (string.IsNullOrEmpty(localizationKey))
        {
            Debug.LogWarning($"[LocalizedText] Localization key is empty on {gameObject.name}");
            return;
        }

        if (!LocalizationManager.HasInstance) return;

        string translatedText = LocalizationManager.Instance.GetTranslation(localizationKey, formatArguments);

        if (uiText != null)
        {
            uiText.text = translatedText;
        }
        else if (tmpText != null)
        {
            tmpText.text = translatedText;
        }
        else
        {
            Debug.LogWarning($"[LocalizedText] No text component found on {gameObject.name}");
        }
    }

    /// <summary>
    /// ��������� ����� �����������
    /// </summary>
    public void SetLocalizationKey(string key)
    {
        localizationKey = key;
        UpdateText();
    }

    /// <summary>
    /// ��������� ���������� ��������������
    /// </summary>
    public void SetFormatArguments(params object[] args)
    {
        formatArguments = args;
        UpdateText();
    }

    #endregion

    #region Editor Methods

    /// <summary>
    /// ������ ���������� ������ � ���������
    /// </summary>
    [ContextMenu("Update Text")]
    public void EditorUpdateText()
    {
        UpdateText();
    }

    #endregion
}