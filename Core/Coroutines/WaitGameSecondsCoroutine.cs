using System;
using UnityEngine;

public class WaitGameSecondsCoroutine : WaitSecondsCoroutineBase
{
    protected override float GetTime()
    {
        return PRTime.Instance.GameDeltaTime;
    }

    public WaitGameSecondsCoroutine(Action callback, float duration) : base(callback, duration)
    {

    }

    public WaitGameSecondsCoroutine(Action callback, float duration, MonoBehaviour instance) : base(callback, duration, instance)
    {

    }
}
