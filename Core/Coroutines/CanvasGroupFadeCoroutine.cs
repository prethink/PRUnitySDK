using System;
using System.Collections;
using UnityEngine;

public class CanvasGroupFadeCoroutine : PRCoroutineBase
{
    private CanvasGroup canvasGroup;
    private float fadeDuration;
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
