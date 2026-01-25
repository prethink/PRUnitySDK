using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFade : PRMonoBehaviourSingletonBase<ScreenFade>
{
    [SerializeField] private Image fadeImage;
    [SerializeField] protected float fadeDuration = 1f;
    private Coroutine coroutine;

    public void FadeIn(Action onComplete = null)
    {
        StopAllCoroutines();
        coroutine = StartCoroutine(Fade(1, true, onComplete));
    }
    public void FadeOut(Action onComplete = null)
    {
        StopAllCoroutines();
        coroutine = StartCoroutine(Fade(0, false, onComplete));
    }

    private void SetAlpha(int value)
    {
        fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, Math.Clamp(value, 0, 255));
    }

    protected override void Start()
    {
        base.Start();
    }

    private IEnumerator Fade(float targetAlpha, bool blockRayCast, System.Action onComplete)
    {
        float startAlpha = fadeImage.color.a;
        float time = 0;

        fadeImage.raycastTarget = blockRayCast;
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, targetAlpha);
        onComplete?.Invoke();
    }
}
