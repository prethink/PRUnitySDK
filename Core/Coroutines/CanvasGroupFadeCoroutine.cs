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
    ///Задержка перед началом затухания в секундах.
    ///</summary>
    public float AwaitTime;
    private Action callback;

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
        yield return new WaitForSeconds(AwaitTime);
        float fadeTime = fadeDuration;
        while (fadeTime > 0)
        {
            yield return WaitPause.Instance;
            fadeTime -= PRTime.Instance.GameDeltaTime;
            canvasGroup.alpha = fadeTime / fadeDuration;
            yield return null;
        }

        callback?.Invoke();
    }
}
