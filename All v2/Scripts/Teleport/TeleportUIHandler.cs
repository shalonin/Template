using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// ���������� UI ��� ����������� ������� ������������
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
    /// ������������� UI ���������
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
    /// ����� ������ ������� ��� ���������
    /// </summary>
    public void ShowTeleportConditions(TeleportTrigger trigger, GameObject player)
    {
        if (trigger == null || player == null) return;

        currentTrigger = trigger;
        currentPlayer = player;

        UpdateConditionDisplay();
        ShowPanel();

        // ��������� ���������� �������
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
        updateCoroutine = StartCoroutine(UpdateConditionsCoroutine());
    }

    /// <summary>
    /// ������� ������
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
    /// ����� ������
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
    /// ���������� ����������� �������
    /// </summary>
    private void UpdateConditionDisplay()
    {
        if (currentTrigger == null || currentPlayer == null) return;

        // ����� ������ ���� ���������� � ����� UI ��������
        // ��� ������� ���������� ������� �����������

        if (titleText != null)
        {
            titleText.text = "Teleport Requirements";
        }

        // ��������� ������ �������
        if (conditionListText != null)
        {
            // � �������� ���������� ����� ����� ����� ������ �� TeleportTrigger
            // ��� ��������� ���������������� ������ �������
            conditionListText.text = "Conditions will be shown here";
        }

        // ��������� ���������
        if (costText != null)
        {
            costText.text = "Cost: 0 coins";
        }

        // ��������� ������ ���������
        if (activateButton != null)
        {
            // activateButton.interactable = currentTrigger.CanActivate(currentPlayer);
        }
    }

    /// <summary>
    /// �������� ��� �������������� ���������� �������
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
    /// ��������� ������� ������ ���������
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