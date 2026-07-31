using System;
using System.Collections;
using UnityEngine;

public abstract class WaitSecondsCoroutineBase : PRCoroutineBase
{
    private Action callback;
    private float duration;

    protected override IEnumerator InternalExecute()
    {
        float fadeTime = duration;
        while (fadeTime > 0)
        {
            yield return WaitPause.Instance;
            fadeTime -= GetTime();
            yield return null;
        }

        callback?.Invoke();
    }

    public void SetDuration(float duration)
    {
        this.duration = duration;
    }

    public WaitSecondsCoroutineBase(Action callback, float duration) : base()
    {
        this.callback = callback;
        this.duration = duration;
    }

    public WaitSecondsCoroutineBase(Action callback, float duration, MonoBehaviour instance) : base(instance)
    {
        this.callback = callback;
        this.duration = duration;
    }

    protected abstract float GetTime();
}
