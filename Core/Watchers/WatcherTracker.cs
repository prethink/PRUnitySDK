using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Регистрирует глобальные watcher-свойства и управляет их корутинами после запуска SDK.
/// </summary>
public class WatcherTracker : TrackerBase<GlobalWatcherProperty>, ISDKEvents
{
    /// <summary>
    /// Указывает, разрешён ли немедленный запуск зарегистрированных watcher'ов.
    /// </summary>
    private bool startedWatchers;

    /// <summary>
    /// Активные корутины, связанные с зарегистрированными watcher'ами.
    /// </summary>
    private readonly Dictionary<GlobalWatcherProperty, Coroutine> runningWatchers = new();

    private void StartWatchers()
    {
        if (startedWatchers)
            return;

        foreach (GlobalWatcherProperty watcher in elements)
            StartWatcher(watcher);

        startedWatchers = true;
    }

    public void OnInitialized()
    {
        StartWatchers();
    }

    /// <summary>
    /// Регистрирует watcher с уникальным непустым ключом и сразу запускает его,
    /// если SDK уже инициализирован.
    /// </summary>
    public override bool Register(GlobalWatcherProperty element)
    {
        if (element == null || string.IsNullOrWhiteSpace(element.Key))
            return false;

        if (elements.Contains(element) || elements.Any(x => x.Key.Equals(element.Key, StringComparison.OrdinalIgnoreCase)))
        {
            Debug.Log($"WatcherProperty with key '{element.Key}' already registered");
            return false;
        }

        elements.Add(element);

        if (startedWatchers)
            StartWatcher(element);

        return true;
    }

    /// <summary>
    /// Удаляет watcher и останавливает связанную с ним корутину.
    /// </summary>
    public override bool Unregister(GlobalWatcherProperty element)
    {
        if (element == null || !elements.Remove(element))
            return false;

        if (runningWatchers.TryGetValue(element, out var coroutine) && coroutine != null)
            PRMonoBehaviourHost.Instance.StopCoroutine(coroutine);

        runningWatchers.Remove(element);
        return true;
    }

    private void StartWatcher(GlobalWatcherProperty watcher)
    {
        if (watcher == null || runningWatchers.ContainsKey(watcher))
            return;

        runningWatchers[watcher] = PRMonoBehaviourHost.Instance.StartCoroutine(watcher.IEnumerator());
    }

    public WatcherTracker()
    {
        EventBus.Subscribe(this);
    }

}
