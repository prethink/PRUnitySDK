using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Шторка на переходах: закрывает экран, пока меняется сцена или мир, и открывает его обратно.
/// </summary>
/// <remarks>
/// Вид — в общих настройках (<see cref="SceneTransitionSettings.Fade"/>): плавное затемнение или «диафрагма»
/// (круг сжимается к своему игроку и раскрывается), цвет или картинка, плывущий узор, звуки закрытия и открытия.
/// Настройки читаются на каждом переходе, так что правка в редакторе видна со следующего.
/// <para>
/// Время немасштабируемое: переход идёт и на паузе.
/// </para>
/// </remarks>
public class ScreenFade : PRMonoBehaviourSingletonBase<ScreenFade>
{
    private static readonly int IrisCenterId = Shader.PropertyToID("_IrisCenter");
    private static readonly int IrisRadiusId = Shader.PropertyToID("_IrisRadius");
    private static readonly int IrisSoftnessId = Shader.PropertyToID("_IrisSoftness");

    [SerializeField] private Image fadeImage;
    [SerializeField, Min(0f), Tooltip("Длительность без общих настроек; с ними — SceneTransition → Fade → Duration.")]
    protected float fadeDuration = 1f;
    [SerializeField, Tooltip("Шейдер выреза для режима Iris (PRUnitySDK/UI/ScreenFadeIris).")]
    private Shader irisShader;

    private Coroutine coroutine;
    private Coroutine scrolling;
    private CanvasGroup group;
    private RawImage pattern;
    private Material irisMaterial;
    // 0 — экран открыт, 1 — закрыт.
    private float progress = -1f;
    private Vector2 irisCenter;
    private Vector2 patternOffset;

    /// <summary>
    /// Закрывает экран и блокирует UI до конца перехода.
    /// </summary>
    /// <param name="onComplete">Действие, когда экран закрыт полностью.</param>
    public void FadeIn(Action onComplete = null)
    {
        StartFade(1f, true, onComplete);
    }

    /// <summary>
    /// Открывает экран; блокировка UI снимается, когда он открыт полностью.
    /// </summary>
    /// <param name="onComplete">Действие, когда экран открыт полностью.</param>
    public void FadeOut(Action onComplete = null)
    {
        StartFade(0f, false, onComplete);
    }

    private static ScreenFadeSettings Settings => PRUnitySDK.Settings?.SceneTransition?.Fade;

    private void StartFade(float target, bool blockRaycasts, Action onComplete)
    {
        if (fadeImage == null)
        {
            PRLog.WriteError(this, $"{nameof(ScreenFade)} requires a Fade Image reference.");
            onComplete?.Invoke();
            return;
        }

        if (coroutine != null)
            StopCoroutine(coroutine);

        ScreenFadeSettings settings = Settings;
        ApplyStyle(settings);
        // Пока шторка движется, UI закрыт в обе стороны: на открытии экран ещё тёмный, и нажатие ушло бы вслепую.
        fadeImage.raycastTarget = true;
        if (pattern != null) pattern.raycastTarget = false;
        // Центр круга берётся и на закрытии, и на открытии: после смены сцены игрок уже другой.
        irisCenter = FindIrisCenter(settings);

        if (Mathf.Approximately(progress, target))
        {
            SetProgress(target, settings);
            fadeImage.raycastTarget = blockRaycasts;
            coroutine = null;
            onComplete?.Invoke();
            return;
        }

        AudioClip sound = settings == null ? null : target > progress ? settings.CloseSound : settings.OpenSound;
        if (sound != null) PRUnitySDK.Managers?.Sound?.PlaySoundUIOneShot(sound);

        coroutine = StartCoroutine(Fade(target, blockRaycasts, settings, onComplete));
    }

    private IEnumerator Fade(float target, bool blockRaycasts, ScreenFadeSettings settings, Action onComplete)
    {
        float start = progress;
        float duration = Mathf.Max(0f, settings != null ? settings.Duration : fadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetProgress(Mathf.Lerp(start, target, elapsed / duration), settings);
            yield return null;
        }

        SetProgress(target, settings);
        fadeImage.raycastTarget = blockRaycasts;
        coroutine = null;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Ставит шторку в положение между «открыто» (0) и «закрыто» (1).
    /// </summary>
    private void SetProgress(float value, ScreenFadeSettings settings)
    {
        progress = Mathf.Clamp01(value);
        bool iris = irisMaterial != null && settings is { Mode: ScreenFadeMode.Iris };
        if (iris)
        {
            // Шторка видна целиком, а прозрачен круг: он сжимается от дальнего угла экрана до точки.
            group.alpha = progress > 0f ? 1f : 0f;
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            irisMaterial.SetVector(IrisCenterId, irisCenter);
            irisMaterial.SetFloat(IrisRadiusId, Mathf.Lerp(MaxIrisRadius(settings), 0f, eased));
        }
        else
        {
            group.alpha = progress;
        }

        bool visible = progress > 0f;
        if (visible && pattern != null && pattern.texture != null && scrolling == null)
            scrolling = StartCoroutine(ScrollPattern(settings));
    }

    /// <summary>
    /// Переносит вид из настроек на шторку: цвет, картинку, узор и материал выреза.
    /// </summary>
    private void ApplyStyle(ScreenFadeSettings settings)
    {
        if (group == null)
        {
            // Не через «??»: отсутствующий компонент Unity — не настоящий null, и AddComponent не вызвался бы.
            if (!fadeImage.TryGetComponent(out group))
                group = fadeImage.gameObject.AddComponent<CanvasGroup>();
            // До первого перехода положение берётся из префаба: шторка на старте может стоять закрытой.
            if (progress < 0f) progress = fadeImage.color.a;
            group.alpha = progress;
        }

        if (settings == null)
        {
            fadeImage.color = Color.black;
            return;
        }

        fadeImage.sprite = settings.Image;
        fadeImage.type = Image.Type.Simple;
        fadeImage.preserveAspect = false;
        fadeImage.color = settings.Color;

        if (settings.Pattern != null && pattern == null)
        {
            var patternObject = new GameObject("Pattern", typeof(RectTransform), typeof(RawImage));
            var rect = (RectTransform)patternObject.transform;
            rect.SetParent(fadeImage.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            pattern = patternObject.GetComponent<RawImage>();
        }

        if (pattern != null)
        {
            pattern.gameObject.SetActive(settings.Pattern != null);
            pattern.texture = settings.Pattern;
            pattern.color = settings.PatternColor;
        }

        bool iris = settings.Mode == ScreenFadeMode.Iris && irisShader != null && irisShader.isSupported;
        if (settings.Mode == ScreenFadeMode.Iris && !iris)
            PRLog.WriteWarning(this, $"{nameof(ScreenFade)}: шейдер выреза не задан или не поддерживается, вместо Iris — обычное затемнение.");
        if (iris && irisMaterial == null)
            irisMaterial = new Material(irisShader) { name = "ScreenFadeIris (runtime)" };
        if (irisMaterial != null)
            irisMaterial.SetFloat(IrisSoftnessId, settings.IrisSoftness);

        Material material = iris ? irisMaterial : null;
        fadeImage.material = material;
        if (pattern != null) pattern.material = material;
    }

    private static Vector2 FindIrisCenter(ScreenFadeSettings settings)
    {
        var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (settings is not { Mode: ScreenFadeMode.Iris, IrisOnPlayer: true }) return center;

        PlayerLocal player = PRUnitySDK.Trackers?.Players?.MainPlayer;
        Camera camera = Camera.main;
        if (player == null || camera == null) return center;

        // Чуть выше ног — круг сходится на персонаже, а не у него под ногами.
        Vector3 screen = camera.WorldToScreenPoint(player.Position + Vector3.up * 1.2f);
        bool onScreen = screen.z > 0f && screen.x >= 0f && screen.x <= Screen.width && screen.y >= 0f && screen.y <= Screen.height;
        return onScreen ? new Vector2(screen.x, screen.y) : center;
    }

    private float MaxIrisRadius(ScreenFadeSettings settings)
    {
        // До самого дальнего угла и ещё кромка: открытый круг не должен оставлять тень по углам.
        float far = 0f;
        foreach (Vector2 corner in new[] { Vector2.zero, new Vector2(Screen.width, 0f), new Vector2(0f, Screen.height), new Vector2(Screen.width, Screen.height) })
            far = Mathf.Max(far, Vector2.Distance(corner, irisCenter));
        return far + settings.IrisSoftness + 2f;
    }

    /// <summary>
    /// Двигает узор, пока шторка видна, как <c>UIPatternScroller</c> на фоне магазина.
    /// </summary>
    private IEnumerator ScrollPattern(ScreenFadeSettings settings)
    {
        while (progress > 0f && pattern != null && pattern.isActiveAndEnabled)
        {
            patternOffset += settings.PatternSpeed * Time.unscaledDeltaTime;
            patternOffset.x = Mathf.Repeat(patternOffset.x, 1f);
            patternOffset.y = Mathf.Repeat(patternOffset.y, 1f);
            Vector2 size = pattern.rectTransform.rect.size;
            if (size.x > 0f && size.y > 0f)
                pattern.uvRect = new Rect(patternOffset, size / Mathf.Max(settings.PatternTileSize, 1f));
            yield return null;
        }

        scrolling = null;
    }
}


public class ScreenFadeFactory : SingletonMonoBehaviourFactoryBase<ScreenFade>
{
    public override string ResourcePath => $"{PRUnitySDK.ResourcePaths.PrefabsPath}/ScreenFader";
}
