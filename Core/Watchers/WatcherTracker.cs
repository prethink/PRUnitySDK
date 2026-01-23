using System;
using System.Linq;
using UnityEngine;

public class WatcherTracker : TrackerBase<GlobalWatcherProperty>, ISDKEvents
{
    private bool startedWatchers;

    private void StartWatchers()
    {
        foreach (GlobalWatcherProperty watcher in elements)
            PRMonoBehaviourHost.Instance.StartCoroutine(watcher.IEnumerator());

        startedWatchers = true;
    }

    public void OnInitialized()
    {
        StartWatchers();
    }

    public override bool Register(GlobalWatcherProperty element)
    {
        if (elements.Any(x => x.Key.Equals(element.Key, StringComparison.OrdinalIgnoreCase)))
        {
            Debug.Log($"WatcherProperty with key '{element.Key}' already registered");
            return false;
        }

        elements.Add(element);

        if (startedWatchers)
            PRMonoBehaviourHost.Instance.StartCoroutine(element.IEnumerator());

        return true;
    }

    public override bool Unregister(GlobalWatcherProperty element)
    {
        return elements.Remove(element);
    }

    public WatcherTracker()
    {
        EventBus.Subscribe(this);
    }

    ~WatcherTracker()
    {
        EventBus.Unsubscribe(this);
    }
}
