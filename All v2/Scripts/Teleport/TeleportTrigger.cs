using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ��������� ��� ��������� ������������ � ���������� �������
/// </summary>
public class TeleportTrigger : MonoBehaviour
{
    [Header("Teleport Settings")]
    [SerializeField] private TeleportManager.TeleportType teleportType = TeleportManager.TeleportType.Position;

    [Header("Position Teleport")]
    [SerializeField] private Transform targetPosition;
    [SerializeField] private Vector3 directPosition = Vector3.zero;

    [Header("Scene Teleport")]
    [SerializeField] private string targetScene = "";
    [SerializeField] private Vector3 spawnPosition = Vector3.zero;

    [Header("Respawn Settings")]
    [SerializeField] private Transform respawnPoint;

    [Header("Conditions")]
    [SerializeField] private bool requireConditions = false;
    [SerializeField] private TeleportCondition[] conditions;
    [SerializeField] private bool requireAllConditions = true; // true = AND, false = OR

    [Header("Cost Settings")]
    [SerializeField] private bool hasCost = false;
    [SerializeField] private int teleportCost = 0;
    [SerializeField] private string costCurrency = "Coins";
    [SerializeField] private bool consumeCost = true;

    [Header("Feedback")]
    [SerializeField] private bool showConditionMessages = true;
    [SerializeField] private string failMessage = "You don't meet the requirements to use this teleport!";
    [SerializeField] private string successMessage = "Teleporting...";

    [Header("Fade Settings")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Trigger Settings")]
    [SerializeField] private string requiredTag = "Player";
    [SerializeField] private bool oneTimeUse = false;
    [SerializeField] private bool disableAfterUse = false;

    private bool isUsed = false;
    private Collider triggerCollider;
    private List<GameObject> playersInTrigger = new List<GameObject>();

    #region Unity Lifecycle

    private void Start()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isUsed && oneTimeUse) return;
        if (string.IsNullOrEmpty(requiredTag) || !other.CompareTag(requiredTag)) return;

        // ��������� ������ � ������
        if (!playersInTrigger.Contains(other.gameObject))
        {
            playersInTrigger.Add(other.gameObject);
        }

        // ������������� ��������� ������� � ���������� ��������
        TryActivateTeleport(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(requiredTag))
        {
            playersInTrigger.Remove(other.gameObject);
        }
    }

    #endregion

    #region Condition Checking

    /// <summary>
    /// ������� ��������� ������������ � ��������� �������
    /// </summary>
    public bool TryActivateTeleport(GameObject player)
    {
        if (isUsed && oneTimeUse) return false;
        if (player == null) return false;

        // ��������� �������
        if (requireConditions && !CheckConditions(player))
        {
            OnConditionFailed(player);
            return false;
        }

        // ��������� ���������
        if (hasCost && !CheckAndConsumeCost(player))
        {
            OnCostFailed(player);
            return false;
        }

        // ��� ������� ��������� - ���������� ��������
        OnConditionsPassed(player);
        ActivateTeleport(player);

        return true;
    }

    /// <summary>
    /// �������� ���� �������
    /// </summary>
    private bool CheckConditions(GameObject player)
    {
        if (conditions == null || conditions.Length == 0) return true;

        if (requireAllConditions)
        {
            // AND ������ - ��� ������� ������ ���� ���������
            foreach (var condition in conditions)
            {
                if (!condition.CheckCondition(player))
                {
                    return false;
                }
            }
            return true;
        }
        else
        {
            // OR ������ - ���� �� ���� ������� ������ ���� ���������
            foreach (var condition in conditions)
            {
                if (condition.CheckCondition(player))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// �������� � �������� ���������
    /// </summary>
    private bool CheckAndConsumeCost(GameObject player)
    {
        if (!hasCost || teleportCost <= 0) return true;

        // ��������� ������� ������
        int currentCurrency = PlayerPrefs.GetInt(costCurrency, 0);
        if (currentCurrency < teleportCost)
        {
            return false;
        }

        // ��������� ������ ���� �����
        if (consumeCost)
        {
            PlayerPrefs.SetInt(costCurrency, currentCurrency - teleportCost);
            PlayerPrefs.Save();
        }

        return true;
    }

    /// <summary>
    /// ��������� ��������� ���������� �������
    /// </summary>
    private void OnConditionsPassed(GameObject player)
    {
        // ��������� �������� �� ���������� �������
        if (requireConditions && conditions != null)
        {
            foreach (var condition in conditions)
            {
                condition.ExecuteConditionAction();
            }
        }

        if (showConditionMessages)
        {
            Debug.Log($"[TeleportTrigger] {successMessage}");
            // ����� ����� �������� UI ���������
        }
    }

    /// <summary>
    /// ��������� ������� �������
    /// </summary>
    private void OnConditionFailed(GameObject player)
    {
        if (showConditionMessages)
        {
            Debug.Log($"[TeleportTrigger] {failMessage}");
            ShowConditionsList(player);
            // ����� ����� �������� UI ��������� � ��������
        }
    }

    /// <summary>
    /// ��������� ������� �� ���������
    /// </summary>
    private void OnCostFailed(GameObject player)
    {
        if (showConditionMessages)
        {
            Debug.Log($"[TeleportTrigger] Not enough {costCurrency}! Need {teleportCost}");
            // ����� ����� �������� UI ���������
        }
    }

    /// <summary>
    /// ����� ������ ������� ������
    /// </summary>
    private void ShowConditionsList(GameObject player)
    {
        if (conditions == null || conditions.Length == 0) return;

        string conditionsList = "Required conditions:\n";
        foreach (var condition in conditions)
        {
            conditionsList += $"- {condition.GetConditionDescription()}\n";
        }

        if (hasCost && teleportCost > 0)
        {
            string consumeText = consumeCost ? " (will be consumed)" : "";
            conditionsList += $"- {costCurrency}: {teleportCost}{consumeText}\n";
        }

        Debug.Log(conditionsList);
        // ����� ����� �������� ������ � UI �������
    }

    #endregion

    #region Teleport Activation

    /// <summary>
    /// ��������� ������������
    /// </summary>
    private void ActivateTeleport(GameObject player)
    {
        if (!TeleportManager.HasInstance) return;

        switch (teleportType)
        {
            case TeleportManager.TeleportType.Position:
                Vector3 targetPos = targetPosition != null ? targetPosition.position : directPosition;
                TeleportManager.Instance.TeleportToPosition(targetPos, useFade, fadeDuration);
                break;

            case TeleportManager.TeleportType.Scene:
                if (!string.IsNullOrEmpty(targetScene))
                {
                    TeleportManager.Instance.TeleportToScene(targetScene, spawnPosition, useFade, fadeDuration);
                }
                break;

            case TeleportManager.TeleportType.Respawn:
                TeleportManager.Instance.RespawnPlayer(respawnPoint, useFade, fadeDuration);
                break;

            case TeleportManager.TeleportType.Custom:
                HandleCustomTeleport(player);
                break;
        }

        // ��������� ������������ �������������
        if (oneTimeUse)
        {
            isUsed = true;
        }

        if (disableAfterUse && triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }
    }

    /// <summary>
    /// ��������� ��������� ������������
    /// </summary>
    protected virtual void HandleCustomTeleport(GameObject player)
    {
        Debug.Log($"[TeleportTrigger] Custom teleport triggered by {player.name}");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// ������ ��������� ��������
    /// </summary>
    public bool TriggerManually(GameObject player = null)
    {
        if (player == null && playersInTrigger.Count > 0)
        {
            player = playersInTrigger[0];
        }

        return TryActivateTeleport(player);
    }

    /// <summary>
    /// ����� ��������� ��������
    /// </summary>
    public void ResetTrigger()
    {
        isUsed = false;
        playersInTrigger.Clear();
        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }
    }

    /// <summary>
    /// ��������� ���������� ������� � ��������
    /// </summary>
    public int GetPlayersInTriggerCount()
    {
        return playersInTrigger.Count;
    }

    /// <summary>
    /// �������� ����������� ������� (��� UI)
    /// </summary>
    public bool CheckSpecificCondition(int conditionIndex, GameObject player)
    {
        if (conditions == null || conditionIndex >= conditions.Length) return false;
        return conditions[conditionIndex].CheckCondition(player);
    }

    #endregion
}