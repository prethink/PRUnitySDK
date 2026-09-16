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
    /// Значения по строковому ключу характеристики.
    /// </summary>
    /// <remarks>
    /// Сам <c>stats</c> словарём работать не может: ключ у него - <see cref="EnumerationReference{T}"/>,
    /// а он сравнивается по ссылке, и поиск сводился к перебору всех характеристик сущности
    /// на каждое чтение. Ассет меняется только в редакторе, поэтому таблица собирается один раз.
    /// </remarks>
    private Dictionary<string, float> lookup;

    /// <summary>
    /// Только чтение для внешнего мира.
    /// </summary>
    public IReadOnlyDictionary<EnumerationReference<TEnum>, float> Stats => stats;

    /// <summary>
    /// Получить значение.
    /// </summary>
    public override bool TryGet(Enumeration key, out float value)
    {
        value = default;

        if (key == null)
            return false;

        EnsureLookup();

        return lookup.TryGetValue(key.Value, out value);
    }

    private void OnEnable() => lookup = null;

    private void OnValidate() => lookup = null;

    private void EnsureLookup()
    {
        if (lookup != null)
            return;

        lookup = new Dictionary<string, float>(stats.Count, System.StringComparer.Ordinal);

        foreach (var kvp in stats)
        {
            // Первый выигрывает, как и при прежнем переборе: два одинаковых ключа
            // в ассете возможны, и менять победителя молча нельзя.
            if (!lookup.ContainsKey(kvp.Key.Value))
                lookup.Add(kvp.Key.Value, kvp.Value);
        }
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