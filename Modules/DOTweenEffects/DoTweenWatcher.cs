using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DoTweenWatcher : MonoBehaviour, IPauseStateListener
{
    private Dictionary<Guid, Tween> tweens = new Dictionary<Guid, Tween>();
    private Dictionary<Guid, bool> pauseData = new Dictionary<Guid, bool>();

    private void OnEnable()
    {
        EventBus.Subscribe(this);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(this);
    }

    public Guid Register(Tween tween, bool reactionOnPause = true)
    {
        if (tween == null)
            throw new ArgumentNullException(nameof(tween));

        Guid guid = Guid.NewGuid();
        tweens[guid] = tween;
        pauseData[guid] = reactionOnPause;
        return guid;
    }

    public void Kill(Guid guid)
    {
        if (tweens.TryGetValue(guid, out Tween tween))
        {
            tween?.Kill();
            tweens.Remove(guid);
            pauseData.Remove(guid);
        }
    }

    public void OnPauseStateChanged(PauseEventArgs args)
    {
        List<Guid> toRemove = new List<Guid>();

        foreach (var kvp in tweens)
        {
            if (pauseData.TryGetValue(kvp.Key, out var pauseRequired) && pauseRequired)
            {
                if (kvp.Value == null)
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }

                if (PRUnitySDK.PauseManager.IsLogicPaused)
                    kvp.Value.Pause();
                else
                    kvp.Value.Play();
            }
        }

        foreach (var guid in toRemove)
        {
            tweens.Remove(guid);
        }
    }
}
