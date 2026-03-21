using System.Collections.Generic;

/// <summary>
/// Контейнер характеристик с поддержкой модификаторов.
/// </summary>
public abstract class PropertyContainerBase<T>
{
    public enum ModifierType
    {
        Add,
        Multiply,
        Override
    }

    public class Modifier
    {
        public T value;
        public Enumeration type;
        public int Priority = 100;
    }

    public class ModifierSourceContainer
    {
        public object source;
        public List<Modifier> modifiers = new();
    }

    protected readonly Dictionary<Enumeration, Dictionary<object, ModifierSourceContainer>> modifiers = new();
    protected readonly Dictionary<Enumeration, T> cache = new();

    /// <summary>
    /// Добавить модификатор.
    /// </summary>
    public void Add(Enumeration key, object source, T value, Enumeration type, int priority = 100)
    {
        if (!modifiers.TryGetValue(key, out var sources))
        {
            sources = new Dictionary<object, ModifierSourceContainer>();
            modifiers[key] = sources;
        }

        if (!sources.TryGetValue(source, out var container))
        {
            container = new ModifierSourceContainer
            {
                source = source
            };
            sources[source] = container;
        }

        container.modifiers.Add(new Modifier
        {
            value = value,
            type = type,
            Priority = priority
        });

        cache.Remove(key);
    }

    /// <summary>
    /// Удалить все модификаторы источника.
    /// </summary>
    public void Remove(Enumeration key, object source)
    {
        if (!modifiers.TryGetValue(key, out var sources))
            return;

        sources.Remove(source);

        if (sources.Count == 0)
            modifiers.Remove(key);

        cache.Remove(key);
    }

    /// <summary>
    /// Получить итоговое значение.
    /// </summary>
    public abstract T Get(Enumeration key, T defaultValue);

    /// <summary>
    /// Очистить всё.
    /// </summary>
    public void Clear()
    {
        modifiers.Clear();
        cache.Clear();
    }

    /// <summary>
    /// Очистить все модификаторы источника.
    /// </summary>
    public void ClearSource(object source)
    {
        foreach (var kvp in modifiers)
        {
            kvp.Value.Remove(source);
        }
    }

    protected void CleanupNullSources()
    {
        var deadSources = new List<(Enumeration key, object source)>();

        foreach (var kvp in modifiers)
        {
            var key = kvp.Key;
            var sources = kvp.Value;

            foreach (var source in sources.Keys)
            {
                if (source == null)
                {
                    deadSources.Add((key, source));
                }
            }
        }

        foreach (var dead in deadSources)
        {
            modifiers[dead.key].Remove(dead.source);
        }
    }
}