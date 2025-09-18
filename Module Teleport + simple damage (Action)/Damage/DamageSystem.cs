using UnityEngine;

/// <summary>
/// Данные события урона
/// </summary>
public struct DamageEventData
{
    public GameObject target;
    public GameObject attacker;
    public float damageAmount;
    public string damageType;
    public Vector3 position;
}

/// <summary>
/// Статическая система урона
/// </summary>
public static class DamageSystem
{
    public static System.Action<DamageEventData> DamageEvent;
}
