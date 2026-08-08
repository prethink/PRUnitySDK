using System;
using UnityEngine;

/// <summary>
///Одноразовая задержка в игровом времени. Для отсчёта использует
///<see cref="PRTime.GameDeltaTime"/>, поэтому следует логической паузе и настройкам
///времени PRUnitySDK.
///</summary>
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
