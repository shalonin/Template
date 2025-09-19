using UnityEngine;
using System;

/// <summary>
/// Условия для активации телепортации
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
    public bool invertCondition = false; // Инвертировать условие (NOT)

    [Header("Player Level")]
    public int requiredLevel = 1;
    public ComparisonType levelComparison = ComparisonType.GreaterOrEqual;

    [Header("Currency")]
    public string currencyType = "Coins";
    public int requiredAmount = 0;
    public ComparisonType currencyComparison = ComparisonType.GreaterOrEqual;
    public bool consumeCurrency = false; // Потратить валюту при активации

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
    /// Типы сравнения
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
    /// Проверка выполнения условия
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

        // Инвертируем результат если нужно
        return invertCondition ? !result : result;
    }

    /// <summary>
    /// Проверка уровня игрока
    /// </summary>
    private bool CheckPlayerLevel()
    {
        // Здесь должна быть интеграция с вашей системой уровней
        int currentLevel = 1; // PlayerStats.Instance.Level;

        // Для примера используем PlayerPrefs
        currentLevel = PlayerPrefs.GetInt("PlayerLevel", 1);

        return CompareValues(currentLevel, requiredLevel, levelComparison);
    }

    /// <summary>
    /// Проверка валюты
    /// </summary>
    private bool CheckCurrency()
    {
        // Здесь должна быть интеграция с вашей системой валют
        int currentCurrency = 0; // CurrencyManager.Instance.GetCurrency(currencyType);

        // Для примера используем PlayerPrefs
        currentCurrency = PlayerPrefs.GetInt(currencyType, 0);

        return CompareValues(currentCurrency, requiredAmount, currencyComparison);
    }

    /// <summary>
    /// Проверка выполнения квеста
    /// </summary>
    private bool CheckQuest()
    {
        // Здесь должна быть интеграция с вашей системой квестов
        bool isQuestCompleted = false; // QuestManager.Instance.IsQuestCompleted(requiredQuest);

        // Для примера используем PlayerPrefs
        isQuestCompleted = PlayerPrefs.GetInt($"Quest_{requiredQuest}_Completed", 0) == 1;

        return questMustBeCompleted ? isQuestCompleted : !isQuestCompleted;
    }

    /// <summary>
    /// Проверка предмета в инвентаре
    /// </summary>
    private bool CheckInventoryItem()
    {
        // Здесь должна быть интеграция с вашей системой инвентаря
        int itemAmount = 0; // InventoryManager.Instance.GetItemAmount(requiredItem);

        // Для примера используем PlayerPrefs
        itemAmount = PlayerPrefs.GetInt($"Item_{requiredItem}", 0);

        return itemAmount >= itemCount;
    }

    /// <summary>
    /// Проверка здоровья
    /// </summary>
    private bool CheckHealth()
    {
        // Здесь должна быть интеграция с вашей системой здоровья
        float currentHealth = 100f; // HealthSystem.Instance.CurrentHealth;

        // Для примера используем PlayerPrefs
        currentHealth = PlayerPrefs.GetFloat("PlayerHealth", 100f);

        return CompareValues(currentHealth, requiredHealth, healthComparison);
    }

    /// <summary>
    /// Проверка кастомного условия
    /// </summary>
    private bool CheckCustomCondition()
    {
        // Здесь можно реализовать свою систему кастомных условий
        // Например, через события или специальный менеджер условий

        // Для примера используем PlayerPrefs
        string savedValue = PlayerPrefs.GetString($"CustomCondition_{customConditionKey}", "");
        return savedValue == customConditionValue;
    }

    /// <summary>
    /// Сравнение значений
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
    /// Выполнение действия при успешной проверке (например, списание валюты)
    /// </summary>
    public void ExecuteConditionAction()
    {
        if (conditionType == ConditionType.Currency && consumeCurrency)
        {
            // Списываем валюту
            int currentCurrency = PlayerPrefs.GetInt(currencyType, 0);
            if (currentCurrency >= requiredAmount)
            {
                PlayerPrefs.SetInt(currencyType, currentCurrency - requiredAmount);
                PlayerPrefs.Save();
            }
        }
        // Здесь можно добавить другие действия при выполнении условий
    }

    /// <summary>
    /// Получение описания условия для отображения игроку
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
    /// Получение символа сравнения для отображения
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