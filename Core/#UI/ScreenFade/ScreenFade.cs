using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFade : PRMonoBehaviourSingletonBase<ScreenFade>
{
    [SerializeField] private Image fadeImage;
    [SerializeField, Min(0f)] protected float fadeDuration = 1f;
    private Coroutine coroutine;

    /// <summary>
    /// Затемняет экран и блокирует UI до завершения перехода.
    /// </summary>
    /// <param name="onComplete">Действие после полного затемнения.</param>
    public void FadeIn(Action onComplete = null)
    {
        StartFade(1f, true, onComplete);
    }

    /// <summary>
    /// Показывает сцену и снимает блокировку UI.
    /// </summary>
    /// <param name="onComplete">Действие после полного проявления сцены.</param>
    public void FadeOut(Action onComplete = null)
    {
        StartFade(0f, false, onComplete);
    }

    private void StartFade(float targetAlpha, bool blockRaycasts, Action onComplete)
    {
        if (fadeImage == null)
        {
            PRLog.WriteError(this, $"{nameof(ScreenFade)} requires a Fade Image reference.");
            onComplete?.Invoke();
            return;
        }

        if (coroutine != null)
            StopCoroutine(coroutine);

        fadeImage.raycastTarget = blockRaycasts;
        if (Mathf.Approximately(fadeImage.color.a, targetAlpha))
        {
            SetAlpha(targetAlpha);
            coroutine = null;
            onComplete?.Invoke();
            return;
        }

        coroutine = StartCoroutine(Fade(targetAlpha, blockRaycasts, onComplete));
    }

    private IEnumerator Fade(float targetAlpha, bool blockRaycasts, Action onComplete)
    {
        float startAlpha = fadeImage.color.a;
        float duration = Mathf.Max(0f, fadeDuration);
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration));
            yield return null;
        }

        SetAlpha(targetAlpha);
        coroutine = null;
        onComplete?.Invoke();
    }

    private void SetAlpha(float value)
    {
        Color color = fadeImage.color;
        color.a = Mathf.Clamp01(value);
        fadeImage.color = color;
    }
}


public class ScreenFadeFactory : SingletonMonoBehaviourFactoryBase<ScreenFade>
{
    public override string ResourcePath => $"{PRUnitySDK.ResourcePaths.PrefabsPath}/ScreenFader";
}
