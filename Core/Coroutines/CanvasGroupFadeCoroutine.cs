using System;
using System.Collections;
using UnityEngine;

/// <summary>
///Одноразово ожидает заданный интервал, затем плавно уменьшает
///<see cref="CanvasGroup.alpha"/> от единицы к нулю в игровом времени и вызывает
///callback после завершения. При перезапуске через <see cref="StopAndExecute"/>
///предварительно восстанавливает полную непрозрачность.
///</summary>
public class CanvasGroupFadeCoroutine : PRCoroutineBase
{
    private CanvasGroup canvasGroup;
    private float fadeDuration;

    /// <summary>
    /// Длительность затухания в секундах. Досрочное снятие гасит быстрее обычного ухода.
    /// </summary>
    public float FadeDuration
    {
        get => fadeDuration;
        set => fadeDuration = Mathf.Max(0.01f, value);
    }

    /// <summary>
    ///Задержка перед началом затухания в секундах.
    ///</summary>
    public float AwaitTime;
    private Action callback;

    /// <summary>
    ///Ждать и затухать по реальному времени, не дожидаясь снятия паузы.
    ///</summary>
    public bool IgnorePause;

    public CanvasGroupFadeCoroutine(CanvasGroup canvasGroup, float awaitTime, float fadeDuration, Action callback = null)
    {
        this.canvasGroup = canvasGroup;
        this.callback = callback;
        this.fadeDuration = fadeDuration;
        this.AwaitTime = awaitTime;
    }

    public override Coroutine StopAndExecute()
    {
        canvasGroup.alpha = 1;
        return base.StopAndExecute();
    }

    protected override IEnumerator InternalExecute()
    {
        if (IgnorePause)
            yield return new WaitForSecondsRealtime(AwaitTime);
        else
            yield return new WaitForSeconds(AwaitTime);

        float fadeTime = fadeDuration;
        while (fadeTime > 0)
        {
            // Паузу ждём, только когда она есть: yield WaitPause стоит кадра и без неё, и затухание шло бы вдвое дольше.
            while (!IgnorePause && WaitPause.Instance.keepWaiting)
                yield return null;

            // На паузе игровая дельта нулевая, поэтому мимо паузы идём по часам интерфейса.
            fadeTime -= IgnorePause ? PRTime.Instance.UiDeltaTime : PRTime.Instance.GetDeltaTime(TimeLayer.UI);
            canvasGroup.alpha = fadeTime / fadeDuration;
            yield return null;
        }

        callback?.Invoke();
    }
}
