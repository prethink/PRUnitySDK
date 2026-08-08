using System;
using System.Collections;
using UnityEngine;

/// <summary>
///Основа одноразовой корутины задержки. Отсчитывает заданную длительность через
///источник delta time, определяемый наследником, учитывает <see cref="WaitPause"/>
///и по завершении один раз вызывает callback.
///</summary>
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

    /// <summary>
    ///Изменяет длительность, которая будет использована при следующем запуске.
    ///</summary>
    /// <param name="duration">Продолжительность ожидания в секундах.</param>
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

    /// <summary>
    ///Возвращает величину времени, вычитаемую на текущей итерации ожидания.
    ///</summary>
    protected abstract float GetTime();
}
