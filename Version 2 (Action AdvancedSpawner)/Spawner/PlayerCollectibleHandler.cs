using UnityEngine;

/// <summary>
/// Обработчик сбора предметов игроком
/// </summary>
public class PlayerCollectibleHandler : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField] private string playerTag = "Player";

    private void OnEnable()
    {
        SpawnedObject.OnTriggerActivated += OnCollectibleTrigger;
    }

    private void OnDisable()
    {
        SpawnedObject.OnTriggerActivated -= OnCollectibleTrigger;
    }

    /// <summary>
    /// Обработка триггера от предмета
    /// </summary>
    private void OnCollectibleTrigger(SpawnedObject spawnedObject, SpawnedObject.TriggerType triggerType, object triggerData)
    {
        // Проверяем что это игрок собрал предмет
        if (triggerType == SpawnedObject.TriggerType.PlayerCollected)
        {
            var player = triggerData as GameObject;
            if (player != null && player.CompareTag(playerTag))
            {
                ProcessCollection(spawnedObject, player);
            }
        }
    }

    /// <summary>
    /// Обработка сбора предмета
    /// </summary>
    private void ProcessCollection(SpawnedObject spawnedObject, GameObject player)
    {
        switch (spawnedObject.ObjectType)
        {
            case "HealthPack":
                // PlayerStats.AddHealth(25);
                Debug.Log($"<Color=Green> >>> </Color> AddHealth");
                break;
            case "Ammo":
                // PlayerAmmo.AddAmmo(10);
                Debug.Log($"<Color=Green> >>> </Color> AddAmmo");
                break;
            case "ScoreBonus":
                // PlayerStats.AddScore(100);
                Debug.Log($"<Color=Green> >>> </Color> AddScore");
                break;
            default:
                Debug.Log($"Collected unknown item: {spawnedObject.ObjectType}");
                break;
        }
    }
}
