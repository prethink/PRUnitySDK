using System.Collections.Generic;

/// <summary>
/// Контейнер влияний на флаги.
/// Поддерживает Allow / Deny / Default.
/// Один source может иметь только одно влияние.
/// </summary>
public class FlagContainer
{
    private enum Influence
    {
        Allow,
        Deny
    }

    public bool IsDirty { get; protected set; }

    // key → (source → influence)
    private readonly Dictionary<Enumeration, Dictionary<object, Influence>> flags = new();

    /// <summary>
    /// Добавить или обновить влияние на флаг.
    /// </summary>
    public void Add(Enumeration key, object source, bool value)
    {
        if (!flags.TryGetValue(key, out var sources))
        {
            sources = new Dictionary<object, Influence>();
            flags[key] = sources;
        }

        sources[source] = value ? Influence.Allow : Influence.Deny;
    }

    /// <summary>
    /// Удалить влияние источника.
    /// </summary>
    public void Remove(Enumeration key, object source)
    {
        if (!flags.TryGetValue(key, out var sources))
            return;

        sources.Remove(source);

        if (sources.Count == 0)
            flags.Remove(key);
    }

    /// <summary>
    /// Получить итоговое значение флага.
    /// </summary>
    public bool Get(Enumeration key, bool defaultValue = true)
    {
        if (!flags.TryGetValue(key, out var sources))
            return defaultValue;

        bool hasAllow = false;

        // Список удаляемых источников
        var deadSources = new List<object>();

        foreach (var kvp in sources)
        {
            var source = kvp.Key;

            // ❗ Проверка на уничтоженные Unity-объекты
            if (source.IsNull())
            {
                deadSources.Add(source);
                continue;
            }

            var influence = kvp.Value;

            if (influence == Influence.Deny)
                return false;

            if (influence == Influence.Allow)
                hasAllow = true;
        }

        // Удаляем "мёртвые" источники
        foreach (var dead in deadSources)
            sources.Remove(dead);

        return hasAllow ? true : defaultValue;
    }

    /// <summary>
    /// Проверка наличия влияний.
    /// </summary>
    public bool HasAny(Enumeration key)
    {
        return flags.ContainsKey(key);
    }

    /// <summary>
    /// Очистить всё.
    /// </summary>
    public void Clear()
    {
        flags.Clear();
    }

    public void SetDirty()
    {
        IsDirty = true;
    }

    /// <summary>
    /// Очистить все влияния конкретного источника.
    /// </summary>
    public void ClearSource(object source)
    {
        var keysToRemove = new List<Enumeration>();

        foreach (var kvp in flags)
        {
            kvp.Value.Remove(source);

            if (kvp.Value.Count == 0)
                keysToRemove.Add(kvp.Key);
        }

        foreach (var key in keysToRemove)
            flags.Remove(key);
    }

    public void Cleanup()
    {
        var keysToRemove = new List<Enumeration>();

        foreach (var kvp in flags)
        {
            var sources = kvp.Value;

            var deadSources = new List<object>();

            foreach (var source in sources.Keys)
            {
                // Unity null-safe проверка
                if (source.IsNull())
                {
                    deadSources.Add(source);
                }
            }

            // удаляем мёртвые источники
            foreach (var dead in deadSources)
                sources.Remove(dead);

            // если больше нет влияний — удаляем ключ
            if (sources.Count == 0)
                keysToRemove.Add(kvp.Key);
        }

        foreach (var key in keysToRemove)
            flags.Remove(key);

        IsDirty = false;
    }
}