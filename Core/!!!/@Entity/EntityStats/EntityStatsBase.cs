using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;

public class EntityStatsBase<TEnum> : EntityStatsBase
    where TEnum : IEnumerationProvider, new()
{
    [SerializedDictionary("Stat", "Value")]
    [SerializeField]
    private SerializedDictionary<EnumerationReference<TEnum>, float> stats = new();

    /// <summary>
    /// Только чтение для внешнего мира.
    /// </summary>
    public IReadOnlyDictionary<EnumerationReference<TEnum>, float> Stats => stats;

    /// <summary>
    /// Получить значение.
    /// </summary>
    public override bool TryGet(Enumeration key, out float value)
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
    public override float Get(Enumeration key, float defaultValue = default)
    {
        return TryGet(key, out var value) 
            ? value 
            : defaultValue;
    }
}

public abstract class EntityStatsBase : ScriptableObject
{
    /// <summary>
    /// Получить значение.
    /// </summary>
    public abstract bool TryGet(Enumeration key, out float value);

    /// <summary>
    /// Получить значение или дефолт.
    /// </summary>
    public abstract float Get(Enumeration key, float defaultValue = default);
}