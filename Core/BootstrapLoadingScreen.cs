#if UNITY_WEBGL && !UNITY_EDITOR
#define PR_PAGE_SPINNER
#endif

using System.Collections;
#if PR_PAGE_SPINNER
using System.Runtime.InteropServices;
#endif
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Экран загрузки на время сборки SDK и перехода на игровую сцену.
/// </summary>
/// <remarks>
/// Без него запуск шёл вслепую: сборка SDK и загрузка сцены занимают главный поток на секунды,
/// и всё это время на экране либо ничего, либо один чёрный кадр. Экран показывает, что игра
/// не зависла: подпись, пояснение, вращающийся значок и полоса прогресса.
/// <para>
/// Вид задают настройки проекта (<see cref="LoadingScreenSettings"/>), экран собирается из них кодом
/// (<see cref="Build"/>). Префаба нет намеренно: ядро не должно зависеть от картинок и шрифтов игры,
/// а окно настроек показывает превью тем же кодом, каким экран строится в игре.
/// </para>
/// <para>
/// Плавным он бывает настолько, насколько работа режется на куски. Сборку SDK загрузчик режет по
/// модулям, сцена грузится асинхронно — между кусками проходят кадры, полоса растёт. Но в WebGL
/// поток один: модуль, который сам длится секунду, и включение сцены (<c>Awake</c>, <c>Start</c>
/// всех её объектов) идут одним куском, и на это время экран замирает.
/// </para>
/// <para>
/// Значок от этого не зависит: в WebGL его крутит не игра, а страница (<see cref="ShowPageSpinner"/>).
/// Анимацию элемента страницы браузер ведёт в своём потоке, и она не останавливается, когда главный
/// поток занят игрой. На остальных платформах значок крутит сам экран.
/// </para>
/// </remarks>
public sealed class BootstrapLoadingScreen : MonoBehaviour
{
    /// <summary>
    /// Холст, под который задана разметка экрана.
    /// </summary>
    private static readonly Vector2 ReferenceResolution = new(1920f, 1080f);

    // Разметка по вертикали от центра экрана, в единицах холста.
    private const float SpinnerY = 190f;
    private const float TitleY = 10f;
    private const float DescriptionY = -120f;
    private const float ProgressY = -250f;

    /// <summary>
    /// Как быстро полоса догоняет настоящий прогресс, долей в секунду.
    /// </summary>
    /// <remarks>
    /// Прогресс приходит скачками; без догона полоса прыгала бы.
    /// </remarks>
    private const float ProgressSpeed = 0.9f;

    /// <summary>
    /// За сколько секунд экран растворяется, когда игровая сцена показана.
    /// </summary>
    private const float FadeSeconds = 0.3f;

    /// <summary>
    /// Доля полосы, занятая с самого начала: пустая полоса читается как «ничего не происходит».
    /// </summary>
    private const float StartProgress = 0.06f;

    /// <summary>
    /// Доля полосы, до которой её доводит сборка SDK.
    /// </summary>
    private const float InitializedProgress = 0.5f;

    /// <summary>
    /// Доля полосы, до которой её доводит загрузка сцены; остаток — включение сцены и первые кадры.
    /// </summary>
    private const float LoadedProgress = 0.9f;

    /// <summary>
    /// Самый длинный шаг времени, который экран принимает за кадр.
    /// </summary>
    /// <remarks>
    /// После тяжёлого куска кадр длится секунды. С настоящим шагом значок сделал бы за него
    /// несколько оборотов, а полоса перепрыгнула бы к цели.
    /// </remarks>
    private const float MaxFrameSeconds = 0.05f;

    /// <summary>
    /// Сторона картинки значка, которая уходит странице, в пикселях.
    /// </summary>
    private const int PageSpinnerSize = 256;

    private LoadingScreenSettings settings;
    private CanvasGroup group;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI descriptionText;
    private RectTransform spinner;
    private RectTransform progressFill;

    private float targetProgress;
    private float shownProgress;
    private bool shown;

    /// <summary>
    /// Значок сейчас рисует страница, а не экран.
    /// </summary>
    private bool pageSpinnerShown;

#if PR_PAGE_SPINNER
    [DllImport("__Internal")]
    private static extern void PRBootSpinner_Show(byte[] rgba, int width, int height,
        float centerX, float centerY, float size, float turnSeconds);

    [DllImport("__Internal")]
    private static extern void PRBootSpinner_Hide(float fadeSeconds);
#endif

    #region Создание

    /// <summary>
    /// Создаёт экран по настройкам проекта.
    /// </summary>
    /// <returns><see langword="null"/>, если настроек нет или экран в них выключен.</returns>
    public static BootstrapLoadingScreen TryCreate()
    {
        PRSDKSettings projectSettings = PRUnitySDK.Settings;
        LoadingScreenSettings screenSettings = projectSettings != null ? projectSettings.LoadingScreen : null;

        if (screenSettings == null || !screenSettings.Enabled)
            return null;

        BootstrapLoadingScreen screen = Build(screenSettings);
        DontDestroyOnLoad(screen.gameObject);
        return screen;
    }

    /// <summary>
    /// Собирает экран из настроек.
    /// </summary>
    /// <remarks>
    /// Тем же методом окно настроек строит превью, поэтому сборка не трогает ничего, что есть только
    /// в запущенной игре.
    /// </remarks>
    /// <param name="settings">Вид экрана.</param>
    /// <param name="scene">Сцена, в которую его положить; по умолчанию — активная.</param>
    public static BootstrapLoadingScreen Build(LoadingScreenSettings settings, Scene scene = default)
    {
        var root = new GameObject(nameof(BootstrapLoadingScreen), typeof(RectTransform));
        root.layer = LayerMask.NameToLayer("UI");

        // До детей: иначе превью на миг оказалось бы в открытой сцене.
        if (scene.IsValid())
            SceneManager.MoveGameObjectToScene(root, scene);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Поверх всего интерфейса игры: экран снимается последним.
        canvas.sortingOrder = 30000;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        // Экран перехватывает нажатия: под ним в последние мгновения уже лежит интерфейс игры.
        root.AddComponent<GraphicRaycaster>();

        var screen = root.AddComponent<BootstrapLoadingScreen>();
        screen.settings = settings;
        screen.group = root.AddComponent<CanvasGroup>();

        screen.BuildBackground(root.transform);

        if (settings.ShowSpinner && settings.SpinnerIcon != null)
        {
            screen.spinner = CreateRect("Spinner", root.transform);
            Place(screen.spinner, SpinnerY, settings.SpinnerSize, settings.SpinnerSize);
            AddImage(screen.spinner, settings.SpinnerIcon, Color.white, 1f).preserveAspect = true;
        }

        if (settings.ShowTitle)
        {
            RectTransform rect = CreateRect("Title", root.transform);
            Place(rect, TitleY, 1500f, settings.TitleSize * 1.25f);
            screen.titleText = AddText(rect, settings.Font, settings.TitleSize, settings.TitleColor, wrap: false);
        }

        if (settings.ShowDescription)
        {
            RectTransform rect = CreateRect("Description", root.transform);
            Place(rect, DescriptionY, 1300f, settings.DescriptionSize * 2.7f);
            screen.descriptionText = AddText(rect, settings.Font, settings.DescriptionSize, settings.DescriptionColor, wrap: true);
        }

        if (settings.ShowProgressBar)
            screen.BuildProgressBar(root.transform);

        return screen;
    }

    private void BuildBackground(Transform parent)
    {
        RectTransform color = CreateRect("Background", parent);
        Stretch(color);
        AddImage(color, null, settings.BackgroundColor, 1f).raycastTarget = true;

        if (settings.BackgroundImage == null)
            return;

        RectTransform picture = CreateRect("BackgroundImage", parent);
        Stretch(picture);
        Image image = AddImage(picture, settings.BackgroundImage, settings.BackgroundImageColor, 1f);

        if (settings.BackgroundImageMode == LoadingScreenImageMode.Stretch)
            return;

        Rect source = settings.BackgroundImage.rect;
        var fitter = picture.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectRatio = source.height > 0f ? source.width / source.height : 1f;
        fitter.aspectMode = settings.BackgroundImageMode == LoadingScreenImageMode.Cover
            ? AspectRatioFitter.AspectMode.EnvelopeParent
            : AspectRatioFitter.AspectMode.FitInParent;
        image.preserveAspect = false;
    }

    private void BuildProgressBar(Transform parent)
    {
        Vector2 size = settings.ProgressSize;

        RectTransform bar = CreateRect("ProgressBar", parent);
        Place(bar, ProgressY, size.x, size.y);
        AddImage(bar, settings.ProgressBackSprite, settings.ProgressBackColor, settings.ProgressPixelsPerUnit);

        // Поле между подложкой и заполнением — доля высоты, чтобы тонкая полоса не съедалась им.
        float inset = Mathf.Min(6f, size.y * 0.12f);

        RectTransform area = CreateRect("FillArea", bar);
        Stretch(area);
        area.offsetMin = new Vector2(inset, inset);
        area.offsetMax = new Vector2(-inset, -inset);

        progressFill = CreateRect("Fill", area);
        progressFill.anchorMin = Vector2.zero;
        progressFill.anchorMax = new Vector2(0f, 1f);
        progressFill.offsetMin = Vector2.zero;
        progressFill.offsetMax = Vector2.zero;
        AddImage(progressFill, settings.ProgressFillSprite, settings.ProgressFillColor, settings.ProgressPixelsPerUnit);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var child = new GameObject(name, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);
        return (RectTransform)child.transform;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // По центру экрана со сдвигом по вертикали.
    private static void Place(RectTransform rect, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    /// <remarks>
    /// Картинка с границами нарезки растягивается без искажения углов, без границ — как есть.
    /// </remarks>
    private static Image AddImage(RectTransform rect, Sprite sprite, Color color, float pixelsPerUnit)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        image.pixelsPerUnitMultiplier = pixelsPerUnit;
        return image;
    }

    private static TextMeshProUGUI AddText(RectTransform rect, TMP_FontAsset font, float size, Color color, bool wrap)
    {
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();

        if (font != null)
            text.font = font;

        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.enableWordWrapping = wrap;
        text.overflowMode = TextOverflowModes.Overflow;

        // Длинный перевод сжимается, а не вылезает за экран.
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Min(size, Mathf.Max(8f, size * 0.45f));
        text.fontSizeMax = size;
        return text;
    }

    #endregion

    #region Показ

    /// <summary>
    /// Показывает экран до готовности SDK.
    /// </summary>
    /// <remarks>
    /// Перевод берётся напрямую по коду языка: живой подписи (<c>SetLocalization</c>) нужен собранный
    /// SDK, а экран появляется раньше него. Когда SDK готов, подписи привязываются как обычно
    /// (<see cref="LoadScene"/>).
    /// </remarks>
    /// <param name="languageCode">Код языка площадки; неизвестный или пустой — английский.</param>
    public void Show(string languageCode)
    {
        SetLanguage(LocalizationUtils.GetLanguageEnum(languageCode));
        Begin();
    }

    /// <summary>
    /// Подписывает экран на указанном языке обычным текстом.
    /// </summary>
    public void SetLanguage(LangType language)
    {
        if (titleText != null)
            titleText.text = PRLocalization.GetTranslate(settings.Title, language);

        if (descriptionText != null)
            descriptionText.text = PRLocalization.GetTranslate(settings.Description, language);
    }

    /// <summary>
    /// Ставит полосу на указанную долю сразу, без догона. Для превью в окне настроек.
    /// </summary>
    public void SetProgressImmediately(float share)
    {
        targetProgress = shownProgress = Mathf.Clamp01(share);
        ApplyProgress();
    }

    /// <summary>
    /// Сообщает, какая доля сборки SDK позади.
    /// </summary>
    /// <param name="share">От нуля до единицы.</param>
    public void SetInitializationProgress(float share)
    {
        targetProgress = Mathf.Max(targetProgress, Mathf.Lerp(StartProgress, InitializedProgress, Mathf.Clamp01(share)));
    }

    /// <summary>
    /// Грузит сцену под экраном и убирает его, когда она показана.
    /// </summary>
    /// <remarks>
    /// Зовут, когда SDK готов: подписи с этого момента живые и следуют за языком игрока.
    /// </remarks>
    /// <param name="sceneIndex">Индекс сцены в Build Settings.</param>
    /// <param name="onShown">Вызывается, когда экран убран и игрок видит игру.</param>
    public void LoadScene(int sceneIndex, System.Action onShown = null)
    {
        LoadScene(() => SceneManager.LoadSceneAsync(sceneIndex), onShown);
    }

    /// <summary>
    /// Грузит сцену заданным способом, сохраняя экран и прогресс до завершения загрузки.
    /// </summary>
    public void LoadScene(System.Func<AsyncOperation> loadScene, System.Action onShown = null)
    {
        if (titleText != null)
            titleText.SetLocalization(settings.Title);

        if (descriptionText != null)
            descriptionText.SetLocalization(settings.Description);

        Begin();
        StartCoroutine(LoadRoutine(loadScene, onShown));
    }

    private void Begin()
    {
        if (shown)
            return;

        shown = true;
        targetProgress = StartProgress;
        shownProgress = 0f;
        ApplyProgress();

        if (group != null)
            group.alpha = 1f;

        ShowPageSpinner();
    }

    private IEnumerator LoadRoutine(System.Func<AsyncOperation> loadScene, System.Action onShown)
    {
        // Экран должен попасть в кадр раньше, чем загрузка займёт главный поток: иначе игрок
        // увидит его уже после того, как ждать стало нечего.
        yield return null;
        yield return null;

        float from = Mathf.Max(targetProgress, StartProgress);
        AsyncOperation load = loadScene();

        while (!load.isDone)
        {
            // Движок доводит progress до 0,9, остальное — включение сцены.
            float loaded = Mathf.Clamp01(load.progress / 0.9f);
            targetProgress = Mathf.Max(targetProgress, Mathf.Lerp(from, LoadedProgress, loaded));
            yield return null;
        }

        targetProgress = 1f;

        // Первый кадр новой сцены — самый тяжёлый: в нём Start всех её объектов. Экран уходит,
        // когда полоса дошла до конца, то есть не раньше, чем этот кадр уже показан.
        while (shownProgress < 0.999f)
            yield return null;

        HidePageSpinner(FadeSeconds);

        float elapsed = 0f;

        while (group != null && elapsed < FadeSeconds)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxFrameSeconds);
            group.alpha = 1f - Mathf.Clamp01(elapsed / FadeSeconds);
            yield return null;
        }

        // Только теперь игрок может играть. Нажатия экран отпускает сам: Destroy сработает
        // лишь в конце кадра, а сигнал уходит сейчас.
        if (group != null)
            group.blocksRaycasts = false;

        onShown?.Invoke();
        Destroy(gameObject);
    }

    private void Update()
    {
        float delta = Mathf.Min(Time.unscaledDeltaTime, MaxFrameSeconds);

        // Значок, отданный странице, крутит она.
        if (spinner != null && !pageSpinnerShown)
            spinner.Rotate(0f, 0f, -settings.SpinnerSpeed * delta);

        if (shownProgress < targetProgress)
        {
            shownProgress = Mathf.MoveTowards(shownProgress, targetProgress, ProgressSpeed * delta);
            ApplyProgress();
        }
    }

    private void ApplyProgress()
    {
        if (progressFill == null)
            return;

        progressFill.anchorMax = new Vector2(Mathf.Clamp01(shownProgress), progressFill.anchorMax.y);
    }

    private void OnDestroy()
    {
        HidePageSpinner(0f);
    }

    #endregion

    #region Значок на странице

    /// <summary>
    /// Отдаёт значок странице: дальше его рисует и крутит она.
    /// </summary>
    /// <remarks>
    /// Странице уходят пиксели значка и его место в долях экрана. Свой значок экран после этого
    /// прячет, иначе их было бы два. Вне WebGL страницы нет, и метод ничего не делает.
    /// </remarks>
    private void ShowPageSpinner()
    {
#if PR_PAGE_SPINNER
        if (pageSpinnerShown || spinner == null || settings.SpinnerSpeed <= 0f)
            return;

        var image = spinner.GetComponent<Image>();

        if (image == null || image.sprite == null)
            return;

        // До первого кадра разметка холста ещё не посчитана, а место значка нужно уже сейчас.
        Canvas.ForceUpdateCanvases();

        var corners = new Vector3[4];
        spinner.GetWorldCorners(corners);

        // У холста поверх экрана мировые координаты — это пиксели экрана, начало снизу слева.
        Vector3 center = (corners[0] + corners[2]) * 0.5f;
        float height = corners[1].y - corners[0].y;

        if (Screen.width <= 0 || Screen.height <= 0 || height <= 0f)
            return;

        byte[] pixels = CaptureSprite(image.sprite, PageSpinnerSize, out int width, out int pixelHeight);

        PRBootSpinner_Show(pixels, width, pixelHeight,
            center.x / Screen.width, 1f - center.y / Screen.height, height / Screen.height, 360f / settings.SpinnerSpeed);

        image.enabled = false;
        pageSpinnerShown = true;
#endif
    }

    private void HidePageSpinner(float fadeOutSeconds)
    {
#if PR_PAGE_SPINNER
        if (!pageSpinnerShown)
            return;

        pageSpinnerShown = false;
        PRBootSpinner_Hide(fadeOutSeconds);
#endif
    }

    /// <summary>
    /// Снимает пиксели спрайта: RGBA, строки снизу вверх.
    /// </summary>
    /// <remarks>
    /// Копированием через видеокарту: у иконок интерфейса выключен Read/Write, а часть сжата.
    /// </remarks>
    private static byte[] CaptureSprite(Sprite sprite, int size, out int width, out int height)
    {
        Texture2D source = sprite.texture;
        Rect rect = sprite.textureRect;

        // Большая сторона — size, вторая по пропорциям спрайта.
        float scale = size / Mathf.Max(rect.width, rect.height);
        width = Mathf.Max(1, Mathf.RoundToInt(rect.width * scale));
        height = Mathf.Max(1, Mathf.RoundToInt(rect.height * scale));

        RenderTexture previous = RenderTexture.active;
        RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);

        Graphics.Blit(source, target,
            new Vector2(rect.width / source.width, rect.height / source.height),
            new Vector2(rect.x / source.width, rect.y / source.height));

        RenderTexture.active = target;

        var copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        copy.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(target);

        byte[] pixels = copy.GetRawTextureData();
        Destroy(copy);
        return pixels;
    }

    #endregion
}
