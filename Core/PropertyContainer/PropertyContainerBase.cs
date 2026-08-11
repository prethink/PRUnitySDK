using System;
using System.Collections.Generic;

/// <summary>
/// Базовый контейнер изменяемых характеристик, сгруппированных по ключу и источнику.
/// </summary>
public abstract class PropertyContainerBase<T>
{
    protected sealed class Modifier
    {
        /// <summary>
        /// Значение модификатора.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Операция применения модификатора.
        /// </summary>
        public Enumeration Type { get; }

        /// <summary>
        /// Приоритет модификатора типа Override.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Порядок добавления для разрешения одинаковых приоритетов.
        /// </summary>
        public long Order { get; }

        public Modifier(T value, Enumeration type, int priority, long order)
        {
            Value = value;
            Type = type;
            Priority = priority;
            Order = order;
        }
    }

    protected sealed class ModifierSourceContainer
    {
        /// <summary>
        /// Модификаторы, добавленные одним источником.
        /// </summary>
        public List<Modifier> Modifiers { get; } = new();
    }

    private sealed class CacheEntry
    {
        /// <summary>
        /// Базовое значение, для которого рассчитан кэш.
        /// </summary>
        public T BaseValue { get; }

        /// <summary>
        /// Рассчитанное итоговое значение.
        /// </summary>
        public T Value { get; }

        public CacheEntry(T baseValue, T value)
        {
            BaseValue = baseValue;
            Value = value;
        }
    }

    protected readonly Dictionary<Enumeration, Dictionary<object, ModifierSourceContainer>> modifiers = new();
    private readonly Dictionary<Enumeration, CacheEntry> cache = new();
    private long nextModifierOrder;

    /// <summary>
    /// Добавляет модификатор характеристики от указанного источника.
    /// </summary>
    public void Add(Enumeration key, object source, T value, Enumeration type, int priority = 100)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (source.IsNull())
            throw new ArgumentNullException(nameof(source));
        if (!IsSupportedModifierType(type))
            throw new ArgumentException("Неизвестный тип модификатора.", nameof(type));

        if (!modifiers.TryGetValue(key, out Dictionary<object, ModifierSourceContainer> sources))
        {
            sources = new Dictionary<object, ModifierSourceContainer>();
            modifiers[key] = sources;
        }

        if (!sources.TryGetValue(source, out ModifierSourceContainer container))
        {
            container = new ModifierSourceContainer();
            sources[source] = container;
        }

        container.Modifiers.Add(new Modifier(value, type, priority, nextModifierOrder++));
        Invalidate(key);
    }

    /// <summary>
    /// Удаляет все модификаторы указанной характеристики от источника.
    /// </summary>
    /// <returns>
    /// <see langword="true"/>, если источник был удалён.
    /// </returns>
    public bool Remove(Enumeration key, object source)
    {
        if (key == null || source.IsNull() ||
            !modifiers.TryGetValue(key, out Dictionary<object, ModifierSourceContainer> sources))
        {
            return false;
        }

        bool removed = sources.Remove(source);
        if (!removed)
            return false;

        if (sources.Count == 0)
            modifiers.Remove(key);

        Invalidate(key);
        return true;
    }

    /// <summary>
    /// Возвращает значение после применения персональных модификаторов.
    /// </summary>
    public T Get(Enumeration key, T defaultValue)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        CleanupDeadSources(key);

        if (cache.TryGetValue(key, out CacheEntry entry) &&
            EqualityComparer<T>.Default.Equals(entry.BaseValue, defaultValue))
        {
            return entry.Value;
        }

        T value = CalculateModifiers(key, defaultValue);
        cache[key] = new CacheEntry(defaultValue, value);
        return value;
    }

    /// <summary>
    /// Возвращает значение после персональных модификаторов и финальных <see cref="GameRules"/>.
    /// </summary>
    public T GetWithRules(Enumeration key, T defaultValue)
    {
        return ApplyGameRules(key, Get(key, defaultValue));
    }

    /// <summary>
    /// Проверяет наличие модификаторов характеристики.
    /// </summary>
    public bool HasModifiers(Enumeration key)
    {
        if (key == null)
            return false;

        CleanupDeadSources(key);
        return modifiers.TryGetValue(key, out Dictionary<object, ModifierSourceContainer> sources) && sources.Count > 0;
    }

    /// <summary>
    /// Удаляет все модификаторы и кэшированные значения.
    /// </summary>
    public void Clear()
    {
        modifiers.Clear();
        cache.Clear();
    }

    /// <summary>
    /// Удаляет все модификаторы, добавленные указанным источником.
    /// </summary>
    /// <returns>Количество затронутых характеристик.</returns>
    public int ClearSource(object source)
    {
        if (source.IsNull())
            return 0;

        var emptyKeys = new List<Enumeration>();
        int changedKeys = 0;

        foreach (KeyValuePair<Enumeration, Dictionary<object, ModifierSourceContainer>> pair in modifiers)
        {
            if (!pair.Value.Remove(source))
                continue;

            changedKeys++;
            Invalidate(pair.Key);
            if (pair.Value.Count == 0)
                emptyKeys.Add(pair.Key);
        }

        foreach (Enumeration key in emptyKeys)
            modifiers.Remove(key);

        return changedKeys;
    }

    protected abstract T CalculateModifiers(Enumeration key, T baseValue);

    protected abstract T ApplyGameRules(Enumeration key, T value);

    private static bool IsSupportedModifierType(Enumeration type)
    {
        return type == ModifierTypes.Add ||
               type == ModifierTypes.Multiply ||
               type == ModifierTypes.Override;
    }

    private void CleanupDeadSources(Enumeration key)
    {
        if (!modifiers.TryGetValue(key, out Dictionary<object, ModifierSourceContainer> sources))
            return;

        List<object> deadSources = null;
        foreach (object source in sources.Keys)
        {
            if (!source.IsNull())
                continue;

            deadSources ??= new List<object>();
            deadSources.Add(source);
        }

        if (deadSources == null)
            return;

        foreach (object source in deadSources)
            sources.Remove(source);

        if (sources.Count == 0)
            modifiers.Remove(key);

        Invalidate(key);
    }

    private void Invalidate(Enumeration key)
    {
        cache.Remove(key);
    }
}
