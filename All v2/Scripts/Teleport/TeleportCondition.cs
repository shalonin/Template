using UnityEngine;
using System;

/// <summary>
/// ������� ��� ��������� ������������
/// </summary>
[Serializable]
public class TeleportCondition
{
    public enum ConditionType
    {
        PlayerLevel,
        Currency,
        QuestCompleted,
        ItemInInventory,
        HealthAbove,
        Custom
    }

    [Header("Condition Settings")]
    public ConditionType conditionType;
    public bool invertCondition = false; // ������������� ������� (NOT)

    [Header("Player Level")]
    public int requiredLevel = 1;
    public ComparisonType levelComparison = ComparisonType.GreaterOrEqual;

    [Header("Currency")]
    public string currencyType = "Coins";
    public int requiredAmount = 0;
    public ComparisonType currencyComparison = ComparisonType.GreaterOrEqual;
    public bool consumeCurrency = false; // ��������� ������ ��� ���������

    [Header("Quest")]
    public string requiredQuest = "";
    public bool questMustBeCompleted = true;

    [Header("Item")]
    public string requiredItem = "";
    public int itemCount = 1;

    [Header("Health")]
    public float requiredHealth = 50f;
    public ComparisonType healthComparison = ComparisonType.GreaterOrEqual;

    [Header("Custom")]
    public string customConditionKey = "";
    public string customConditionValue = "";

    /// <summary>
    /// ���� ���������
    /// </summary>
    public enum ComparisonType
    {
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual
    }

    /// <summary>
    /// �������� ���������� �������
    /// </summary>
    public bool CheckCondition(GameObject player)
    {
        bool result = false;

        switch (conditionType)
        {
            case ConditionType.PlayerLevel:
                result = CheckPlayerLevel();
                break;
            case ConditionType.Currency:
                result = CheckCurrency();
                break;
            case ConditionType.QuestCompleted:
                result = CheckQuest();
                break;
            case ConditionType.ItemInInventory:
                result = CheckInventoryItem();
                break;
            case ConditionType.HealthAbove:
                result = CheckHealth();
                break;
            case ConditionType.Custom:
                result = CheckCustomCondition();
                break;
        }

        // ����������� ��������� ���� �����
        return invertCondition ? !result : result;
    }

    /// <summary>
    /// �������� ������ ������
    /// </summary>
    private bool CheckPlayerLevel()
    {
        // ����� ������ ���� ���������� � ����� �������� �������
        int currentLevel = 1; // PlayerStats.Instance.Level;

        // ��� ������� ���������� PlayerPrefs
        currentLevel = PlayerPrefs.GetInt("PlayerLevel", 1);

        return CompareValues(currentLevel, requiredLevel, levelComparison);
    }

    /// <summary>
    /// �������� ������
    /// </summary>
    private bool CheckCurrency()
    {
        // ����� ������ ���� ���������� � ����� �������� �����
        int currentCurrency = 0; // CurrencyManager.Instance.GetCurrency(currencyType);

        // ��� ������� ���������� PlayerPrefs
        currentCurrency = PlayerPrefs.GetInt(currencyType, 0);

        return CompareValues(currentCurrency, requiredAmount, currencyComparison);
    }

    /// <summary>
    /// �������� ���������� ������
    /// </summary>
    private bool CheckQuest()
    {
        // ����� ������ ���� ���������� � ����� �������� �������
        bool isQuestCompleted = false; // QuestManager.Instance.IsQuestCompleted(requiredQuest);

        // ��� ������� ���������� PlayerPrefs
        isQuestCompleted = PlayerPrefs.GetInt($"Quest_{requiredQuest}_Completed", 0) == 1;

        return questMustBeCompleted ? isQuestCompleted : !isQuestCompleted;
    }

    /// <summary>
    /// �������� �������� � ���������
    /// </summary>
    private bool CheckInventoryItem()
    {
        // ����� ������ ���� ���������� � ����� �������� ���������
        int itemAmount = 0; // InventoryManager.Instance.GetItemAmount(requiredItem);

        // ��� ������� ���������� PlayerPrefs
        itemAmount = PlayerPrefs.GetInt($"Item_{requiredItem}", 0);

        return itemAmount >= itemCount;
    }

    /// <summary>
    /// �������� ��������
    /// </summary>
    private bool CheckHealth()
    {
        // ����� ������ ���� ���������� � ����� �������� ��������
        float currentHealth = 100f; // HealthSystem.Instance.CurrentHealth;

        // ��� ������� ���������� PlayerPrefs
        currentHealth = PlayerPrefs.GetFloat("PlayerHealth", 100f);

        return CompareValues(currentHealth, requiredHealth, healthComparison);
    }

    /// <summary>
    /// �������� ���������� �������
    /// </summary>
    private bool CheckCustomCondition()
    {
        // ����� ����� ����������� ���� ������� ��������� �������
        // ��������, ����� ������� ��� ����������� �������� �������

        // ��� ������� ���������� PlayerPrefs
        string savedValue = PlayerPrefs.GetString($"CustomCondition_{customConditionKey}", "");
        return savedValue == customConditionValue;
    }

    /// <summary>
    /// ��������� ��������
    /// </summary>
    private bool CompareValues<T>(T currentValue, T requiredValue, ComparisonType comparison) where T : IComparable<T>
    {
        int comparisonResult = currentValue.CompareTo(requiredValue);

        switch (comparison)
        {
            case ComparisonType.Equal:
                return comparisonResult == 0;
            case ComparisonType.NotEqual:
                return comparisonResult != 0;
            case ComparisonType.Greater:
                return comparisonResult > 0;
            case ComparisonType.GreaterOrEqual:
                return comparisonResult >= 0;
            case ComparisonType.Less:
                return comparisonResult < 0;
            case ComparisonType.LessOrEqual:
                return comparisonResult <= 0;
            default:
                return false;
        }
    }

    /// <summary>
    /// ���������� �������� ��� �������� �������� (��������, �������� ������)
    /// </summary>
    public void ExecuteConditionAction()
    {
        if (conditionType == ConditionType.Currency && consumeCurrency)
        {
            // ��������� ������
            int currentCurrency = PlayerPrefs.GetInt(currencyType, 0);
            if (currentCurrency >= requiredAmount)
            {
                PlayerPrefs.SetInt(currencyType, currentCurrency - requiredAmount);
                PlayerPrefs.Save();
            }
        }
        // ����� ����� �������� ������ �������� ��� ���������� �������
    }

    /// <summary>
    /// ��������� �������� ������� ��� ����������� ������
    /// </summary>
    public string GetConditionDescription()
    {
        string description = "";
        string notPrefix = invertCondition ? "NOT " : "";

        switch (conditionType)
        {
            case ConditionType.PlayerLevel:
                description = $"{notPrefix}Player Level {GetComparisonSymbol(levelComparison)} {requiredLevel}";
                break;
            case ConditionType.Currency:
                string consumeText = consumeCurrency ? " (will be consumed)" : "";
                description = $"{notPrefix}{currencyType} {GetComparisonSymbol(currencyComparison)} {requiredAmount}{consumeText}";
                break;
            case ConditionType.QuestCompleted:
                string completedText = questMustBeCompleted ? "completed" : "not completed";
                description = $"{notPrefix}Quest '{requiredQuest}' {completedText}";
                break;
            case ConditionType.ItemInInventory:
                description = $"{notPrefix}Have {itemCount}x {requiredItem}";
                break;
            case ConditionType.HealthAbove:
                description = $"{notPrefix}Health {GetComparisonSymbol(healthComparison)} {requiredHealth}%";
                break;
            case ConditionType.Custom:
                description = $"{notPrefix}Custom condition: {customConditionKey} = {customConditionValue}";
                break;
        }

        return description;
    }

    /// <summary>
    /// ��������� ������� ��������� ��� �����������
    /// </summary>
    private string GetComparisonSymbol(ComparisonType comparison)
    {
        switch (comparison)
        {
            case ComparisonType.Equal: return "==";
            case ComparisonType.NotEqual: return "!=";
            case ComparisonType.Greater: return ">";
            case ComparisonType.GreaterOrEqual: return ">=";
            case ComparisonType.Less: return "<";
            case ComparisonType.LessOrEqual: return "<=";
            default: return "";
        }
    }
}