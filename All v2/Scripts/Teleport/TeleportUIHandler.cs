using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Обработчик UI для отображения условий телепортации
/// </summary>
public class TeleportUIHandler : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject conditionPanel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text conditionListText;
    [SerializeField] private Text costText;
    [SerializeField] private Button activateButton;
    [SerializeField] private Button closeButton;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 1f;

    private TeleportTrigger currentTrigger;
    private GameObject currentPlayer;
    private Coroutine updateCoroutine;

    #region Unity Lifecycle

    private void Start()
    {
        InitializeUI();
    }

    private void OnDestroy()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
    }

    #endregion

    #region UI Initialization

    /// <summary>
    /// Инициализация UI элементов
    /// </summary>
    private void InitializeUI()
    {
        if (activateButton != null)
        {
            activateButton.onClick.AddListener(OnActivateButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
        }

        HidePanel();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Показ панели условий для телепорта
    /// </summary>
    public void ShowTeleportConditions(TeleportTrigger trigger, GameObject player)
    {
        if (trigger == null || player == null) return;

        currentTrigger = trigger;
        currentPlayer = player;

        UpdateConditionDisplay();
        ShowPanel();

        // Запускаем обновление условий
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
        updateCoroutine = StartCoroutine(UpdateConditionsCoroutine());
    }

    /// <summary>
    /// Скрытие панели
    /// </summary>
    public void HidePanel()
    {
        if (conditionPanel != null)
        {
            conditionPanel.SetActive(false);
        }

        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            updateCoroutine = null;
        }

        currentTrigger = null;
        currentPlayer = null;
    }

    /// <summary>
    /// Показ панели
    /// </summary>
    public void ShowPanel()
    {
        if (conditionPanel != null)
        {
            conditionPanel.SetActive(true);
        }
    }

    #endregion

    #region UI Updates

    /// <summary>
    /// Обновление отображения условий
    /// </summary>
    private void UpdateConditionDisplay()
    {
        if (currentTrigger == null || currentPlayer == null) return;

        // Здесь должна быть интеграция с вашей UI системой
        // Для примера используем базовое отображение

        if (titleText != null)
        {
            titleText.text = "Teleport Requirements";
        }

        // Обновляем список условий
        if (conditionListText != null)
        {
            // В реальной реализации здесь будет вызов метода из TeleportTrigger
            // для получения форматированного списка условий
            conditionListText.text = "Conditions will be shown here";
        }

        // Обновляем стоимость
        if (costText != null)
        {
            costText.text = "Cost: 0 coins";
        }

        // Обновляем кнопку активации
        if (activateButton != null)
        {
            // activateButton.interactable = currentTrigger.CanActivate(currentPlayer);
        }
    }

    /// <summary>
    /// Корутина для периодического обновления условий
    /// </summary>
    private IEnumerator UpdateConditionsCoroutine()
    {
        while (currentTrigger != null && currentPlayer != null)
        {
            UpdateConditionDisplay();
            yield return new WaitForSeconds(updateInterval);
        }
    }

    #endregion

    #region Button Handlers

    /// <summary>
    /// Обработка нажатия кнопки активации
    /// </summary>
    private void OnActivateButtonClicked()
    {
        if (currentTrigger != null && currentPlayer != null)
        {
            bool success = currentTrigger.TryActivateTeleport(currentPlayer);
            if (success)
            {
                HidePanel();
            }
        }
    }

    #endregion
}