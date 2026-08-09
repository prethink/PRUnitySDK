using System;
using System.Collections.Generic;

/// <summary>
/// Итог голосования источников по флагу без применения значения по умолчанию.
/// </summary>
public enum FlagDecision
{
    Unspecified,
    Allow,
    Deny
}

/// <summary>
/// Объединяет независимые влияния на флаги. Deny имеет абсолютный приоритет,
/// затем Allow, а при отсутствии живых влияний возвращается Unspecified.
/// </summary>
public class FlagResolver
{
    private sealed class FlagInfluences
    {
        public readonly Dictionary<object, FlagDecision> Persistent = new();
        public readonly Dictionary<object, FlagDecision> Frame = new();

        public bool IsEmpty => Persistent.Count == 0 && Frame.Count == 0;
    }

    private readonly Dictionary<Enumeration, FlagInfluences> flags = new();

    /// <summary>
    /// Указывает, что контейнер менялся или может содержать уничтоженные Unity sources.
    /// Сбрасывается методом <see cref="Cleanup"/>.
    /// </summary>
    public bool IsDirty { get; private set; }

    /// <summary>
    /// Совместимое событие. Вызывается при изменении решения; true означает Allow,
    /// false означает Deny или Unspecified. Для нового кода предпочтительнее
    /// <see cref="OnChangeFlagDecision"/>.
    /// </summary>
    public event Action<Enumeration, bool> OnChangeFlagState;

    /// <summary>
    /// Вызывается только когда итоговое решение действительно изменилось.
    /// </summary>
    public event Action<Enumeration, FlagDecision> OnChangeFlagDecision;

    public void Allow(Enumeration key, object source) => Set(key, source, FlagDecision.Allow, false);

    public void Deny(Enumeration key, object source) => Set(key, source, FlagDecision.Deny, false);

    public void AllowFrame(Enumeration key, object source) => Set(key, source, FlagDecision.Allow, true);

    public void DenyFrame(Enumeration key, object source) => Set(key, source, FlagDecision.Deny, true);

    /// <summary>
    /// Совместимый API: true = Allow, false = Deny.
    /// </summary>
    public void Add(Enumeration key, object source, bool value) =>
        Set(key, source, ToDecision(value), false);

    /// <summary>
    /// Совместимый API с явным lifetime.
    /// </summary>
    public void Add(Enumeration key, object source, bool value, bool isFlagFrame) =>
        Set(key, source, ToDecision(value), isFlagFrame);

    public void AddFrame(Enumeration key, object source, bool value) =>
        Set(key, source, ToDecision(value), true);

    private void Set(Enumeration key, object source, FlagDecision decision, bool isFrame)
    {
        ValidateKeyAndSource(key, source);

        FlagDecision previous = Resolve(key);

        if (!flags.TryGetValue(key, out var influences))
        {
            influences = new FlagInfluences();
            flags.Add(key, influences);
        }

        Dictionary<object, FlagDecision> target = isFrame
            ? influences.Frame
            : influences.Persistent;

        if (target.TryGetValue(source, out var current) && current == decision)
            return;

        target[source] = decision;
        IsDirty = true;
        NotifyIfChanged(key, previous);
    }

    /// <summary>
    /// Удаляет и постоянное, и frame-влияние source на указанный флаг.
    /// </summary>
    public void Remove(Enumeration key, object source)
    {
        if (key == null || source == null || !flags.TryGetValue(key, out var influences))
            return;

        FlagDecision previous = Resolve(key);
        bool removed = influences.Persistent.Remove(source);
        removed |= influences.Frame.Remove(source);

        if (!removed)
            return;

        if (influences.IsEmpty)
            flags.Remove(key);

        IsDirty = true;
        NotifyIfChanged(key, previous);
    }

    /// <summary>
    /// Возвращает результат голосования без значения по умолчанию.
    /// </summary>
    public FlagDecision Resolve(Enumeration key)
    {
        if (key == null || !flags.TryGetValue(key, out var influences))
            return FlagDecision.Unspecified;

        bool hasAllow = false;

        Evaluate(influences.Persistent, ref hasAllow, out bool denied);
        if (denied)
            return FlagDecision.Deny;

        Evaluate(influences.Frame, ref hasAllow, out denied);
        if (denied)
            return FlagDecision.Deny;

        return hasAllow ? FlagDecision.Allow : FlagDecision.Unspecified;
    }

    public bool Get(Enumeration key, bool defaultValue = true)
    {
        return Resolve(key) switch
        {
            FlagDecision.Allow => true,
            FlagDecision.Deny => false,
            _ => defaultValue
        };
    }

    public bool HasAny(Enumeration key) => Resolve(key) != FlagDecision.Unspecified;

    /// <summary>
    /// Удаляет все влияния и сообщает об изменении каждого затронутого решения.
    /// </summary>
    public void Clear()
    {
        if (flags.Count == 0)
            return;

        var previous = new Dictionary<Enumeration, FlagDecision>(flags.Count);
        foreach (var item in flags)
            previous[item.Key] = Resolve(item.Key);

        flags.Clear();
        IsDirty = false;

        foreach (var item in previous)
            NotifyIfChanged(item.Key, item.Value);
    }

    /// <summary>
    /// Удаляет все влияния, добавленные через AddFrame/AllowFrame/DenyFrame.
    /// Постоянные влияния тех же sources сохраняются.
    /// </summary>
    public void ClearFrameFlags()
    {
        if (flags.Count == 0)
            return;

        var changed = new List<(Enumeration Key, FlagDecision Previous)>();
        var emptyKeys = new List<Enumeration>();

        foreach (var item in flags)
        {
            if (item.Value.Frame.Count == 0)
                continue;

            changed.Add((item.Key, Resolve(item.Key)));
            item.Value.Frame.Clear();

            if (item.Value.IsEmpty)
                emptyKeys.Add(item.Key);
        }

        foreach (var key in emptyKeys)
            flags.Remove(key);

        if (changed.Count == 0)
            return;

        IsDirty = true;
        foreach (var item in changed)
            NotifyIfChanged(item.Key, item.Previous);
    }

    public void SetDirty() => IsDirty = true;

    /// <summary>
    /// Удаляет все постоянные и frame-влияния указанного source.
    /// </summary>
    public void ClearSource(object source)
    {
        if (source == null || flags.Count == 0)
            return;

        var changed = new List<(Enumeration Key, FlagDecision Previous)>();
        var emptyKeys = new List<Enumeration>();

        foreach (var item in flags)
        {
            FlagDecision previous = Resolve(item.Key);
            bool removed = item.Value.Persistent.Remove(source);
            removed |= item.Value.Frame.Remove(source);

            if (!removed)
                continue;

            changed.Add((item.Key, previous));
            if (item.Value.IsEmpty)
                emptyKeys.Add(item.Key);
        }

        foreach (var key in emptyKeys)
            flags.Remove(key);

        if (changed.Count == 0)
            return;

        IsDirty = true;
        foreach (var item in changed)
            NotifyIfChanged(item.Key, item.Previous);
    }

    /// <summary>
    /// Удаляет уничтоженные Unity-объекты, использованные как source.
    /// Обычные CLR-объекты удаляются явно через Remove/ClearSource.
    /// </summary>
    public void Cleanup()
    {
        if (flags.Count == 0)
        {
            IsDirty = false;
            return;
        }

        var changed = new List<(Enumeration Key, FlagDecision Previous)>();
        var emptyKeys = new List<Enumeration>();

        foreach (var item in flags)
        {
            FlagDecision previous = Resolve(item.Key);
            bool removed = RemoveDeadSources(item.Value.Persistent);
            removed |= RemoveDeadSources(item.Value.Frame);

            if (!removed)
                continue;

            changed.Add((item.Key, previous));
            if (item.Value.IsEmpty)
                emptyKeys.Add(item.Key);
        }

        foreach (var key in emptyKeys)
            flags.Remove(key);

        IsDirty = false;
        foreach (var item in changed)
            NotifyIfChanged(item.Key, item.Previous);
    }

    private static void Evaluate(
        Dictionary<object, FlagDecision> influences,
        ref bool hasAllow,
        out bool denied)
    {
        denied = false;

        foreach (var item in influences)
        {
            if (IsDeadSource(item.Key))
                continue;

            if (item.Value == FlagDecision.Deny)
            {
                denied = true;
                return;
            }

            if (item.Value == FlagDecision.Allow)
                hasAllow = true;
        }
    }

    private static bool RemoveDeadSources(Dictionary<object, FlagDecision> influences)
    {
        List<object> deadSources = null;

        foreach (var source in influences.Keys)
        {
            if (!IsDeadSource(source))
                continue;

            deadSources ??= new List<object>();
            deadSources.Add(source);
        }

        if (deadSources == null)
            return false;

        foreach (var source in deadSources)
            influences.Remove(source);

        return true;
    }

    private void NotifyIfChanged(Enumeration key, FlagDecision previous)
    {
        FlagDecision current = Resolve(key);
        if (current == previous)
            return;

        OnChangeFlagDecision?.Invoke(key, current);
        OnChangeFlagState?.Invoke(key, current == FlagDecision.Allow);
    }

    private static FlagDecision ToDecision(bool value) =>
        value ? FlagDecision.Allow : FlagDecision.Deny;

    private static bool IsDeadSource(object source) => source == null || source.IsNull();

    private static void ValidateKeyAndSource(Enumeration key, object source)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        if (source == null || source.IsNull())
            throw new ArgumentNullException(nameof(source), "Flag source must be a live object.");
    }
}
