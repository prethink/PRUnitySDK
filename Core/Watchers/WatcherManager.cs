using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WatcherManager : IReadyGameEvent
{
    private List<GlobalWatcherProperty> WatcherProperties = new List<GlobalWatcherProperty>();

    private bool startedWatchers;

    public void RegisterWatcher(GlobalWatcherProperty watcher)
    {
        if(WatcherProperties.Any(x => x.Key.Equals(watcher.Key, StringComparison.OrdinalIgnoreCase)))
        {
            Debug.Log($"WatcherProperty with key '{watcher.Key}' already registered");
            return;
        }

        WatcherProperties.Add(watcher);

        if(startedWatchers)
            PRMonoBehaviourHost.Instance.StartCoroutine(watcher.IEnumerator());
    }

    private void StartWatchers()
    {
        foreach (GlobalWatcherProperty watcher in WatcherProperties)
            PRMonoBehaviourHost.Instance.StartCoroutine(watcher.IEnumerator());

        startedWatchers = true;
    }

    public void OnReadyGame()
    {
        StartWatchers();
    }

    public WatcherManager()
    {
        EventBus.Subscribe(this);
    }

    ~WatcherManager()
    {
        EventBus.Unsubscribe(this);
    }
}
