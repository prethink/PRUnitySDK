using System;
using UnityEngine;

/// <summary>
///Одноразовая задержка, использующая <see cref="PRTime.RealDeltaTime"/>.
///В отличие от игрового времени, скорость отсчёта не зависит от масштаба времени игры.
///</summary>
public class WaitRealSecondsCoroutine : WaitSecondsCoroutineBase
{
    protected override float GetTime()
    {
        return PRTime.Instance.RealDeltaTime;
    }

    public WaitRealSecondsCoroutine(Action callback, float duration) : base(callback, duration)
    {

    }

    public WaitRealSecondsCoroutine(Action callback, float duration, MonoBehaviour instance) : base(callback, duration, instance)
    {

    }
}
