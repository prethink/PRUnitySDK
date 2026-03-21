using AYellowpaper.SerializedCollections;
using UnityEngine;
using System.Collections.Generic;

public class EntityStatsBase<TEnum, TValueType> : EntityStatsBase
    where TEnum : IEnumerationProvider, new()
{
    [SerializedDictionary("Stat", "Value")]
    [SerializeField]
    private SerializedDictionary<EnumerationReference<TEnum>, TValueType> stats = new();

    /// <summary>
    /// Только чтение для внешнего мира.
    /// </summary>
    public IReadOnlyDictionary<EnumerationReference<TEnum>, TValueType> Stats => stats;

    /// <summary>
    /// Получить значение.
    /// </summary>
    public bool TryGet(Enumeration key, out TValueType value)
    {
        foreach (var kvp in stats)
        {
            if (kvp.Key.ToEnumeration() == key)
            {
                value = kvp.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Получить значение или дефолт.
    /// </summary>
    public TValueType Get(Enumeration key, TValueType defaultValue = default)
    {
        return TryGet(key, out var value) 
            ? value 
            : defaultValue;
    }
}

public class EntityStatsBase : ScriptableObject
{

}