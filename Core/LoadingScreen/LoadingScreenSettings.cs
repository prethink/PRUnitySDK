using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Как картинка фона вписывается в экран.
/// </summary>
public enum LoadingScreenImageMode
{
    /// <summary>
    /// Растянуть на весь экран, не сохраняя пропорций.
    /// </summary>
    Stretch,

    /// <summary>
    /// Закрыть экран целиком, сохранив пропорции: лишнее уходит за края.
    /// </summary>
    Cover,

    /// <summary>
    /// Вписать целиком, сохранив пропорции: по краям остаётся цвет фона.
    /// </summary>
    Fit
}

/// <summary>
/// Вид экрана загрузки между запуском игры и игровой сценой.
/// </summary>
/// <remarks>
/// Экран (<see cref="BootstrapLoadingScreen"/>) собирается из этих настроек целиком, без префаба:
/// так ядро не зависит от картинок и шрифтов конкретной игры, а игра меняет вид, не трогая код.
/// Правится в окне <c>PRUnitySDK/Windows/Loading Screen</c> — там же превью.
/// <para>
/// Лежит в настройках проекта: у каждой игры свой экран загрузки, и переключение проекта меняет
/// его вместе с остальным.
/// </para>
/// </remarks>
[Serializable]
[SettingsDescription("Экран загрузки при запуске игры: фон, подписи, полоса прогресса, вращающийся значок. Настройка и превью — PRUnitySDK → Windows → Loading Screen.")]
[DatabaseExternalEditor(MenuPath, WindowName = "Экран загрузки")]
public sealed class LoadingScreenSettings
{
    /// <summary>
    /// Пункт меню окна настройки.
    /// </summary>
    /// <remarks>
    /// Лежит в рантайме, а не рядом с окном: на путь ссылается атрибут настроек, а сборка
    /// редактора из рантайма не видна.
    /// </remarks>
    public const string MenuPath = "PRUnitySDK/Windows/Loading Screen";

    [SerializeField]
    [Tooltip("Показывать экран загрузки. Выключено — запуск идёт по-старому: SDK собирается одним вызовом, сцена меняется под затемнением.")]
    private bool enabled = true;

    [Header("Фон")]
    [SerializeField]
    [Tooltip("Цвет фона. Виден целиком, пока картинки нет, и по краям, когда она вписана.")]
    private Color backgroundColor = new(0.086f, 0.188f, 0.478f, 1f);

    [SerializeField]
    [Tooltip("Картинка фона. Пусто — один цвет.")]
    private Sprite backgroundImage;

    [SerializeField]
    [Tooltip("Как картинка вписывается в экран.")]
    private LoadingScreenImageMode backgroundImageMode = LoadingScreenImageMode.Cover;

    [SerializeField]
    [Tooltip("Оттенок картинки фона. Тёмный серый приглушает её, чтобы текст читался.")]
    private Color backgroundImageColor = Color.white;

    [Header("Текст")]
    [SerializeField]
    [Tooltip("Шрифт подписей. Пусто — шрифт TextMeshPro по умолчанию.")]
    private TMP_FontAsset font;

    [SerializeField]
    [Tooltip("Показывать заголовок.")]
    private bool showTitle = true;

    [SerializeField]
    [Tooltip("Заголовок. Язык без текста показывает стандартный: «Загрузка...».")]
    private LocalizationControl title = new();

    [SerializeField] private Color titleColor = Color.white;
    [SerializeField, Min(8f)] private float titleSize = 110f;

    [SerializeField]
    [Tooltip("Показывать пояснение под заголовком.")]
    private bool showDescription = true;

    [SerializeField]
    [Tooltip("Пояснение. Язык без текста показывает стандартное.")]
    private LocalizationControl description = new();

    [SerializeField] private Color descriptionColor = new(0.749f, 0.827f, 1f, 1f);
    [SerializeField, Min(8f)] private float descriptionSize = 46f;

    [Header("Полоса прогресса")]
    [SerializeField]
    [Tooltip("Показывать полосу прогресса.")]
    private bool showProgressBar = true;

    [SerializeField]
    [Tooltip("Ширина и высота полосы в единицах холста 1920×1080.")]
    private Vector2 progressSize = new(900f, 50f);

    [SerializeField] private Color progressBackColor = new(0f, 0f, 0f, 0.45f);
    [SerializeField] private Color progressFillColor = new(0.239f, 0.863f, 0.384f, 1f);

    [SerializeField]
    [Tooltip("Картинка подложки полосы. Пусто — прямоугольник. Картинка с границами нарезки растягивается без искажения углов.")]
    private Sprite progressBackSprite;

    [SerializeField]
    [Tooltip("Картинка заполнения полосы. Пусто — прямоугольник.")]
    private Sprite progressFillSprite;

    [SerializeField, Min(0.1f)]
    [Tooltip("Множитель плотности картинок полосы: больше — углы нарезанной картинки меньше.")]
    private float progressPixelsPerUnit = 1f;

    [Header("Значок")]
    [SerializeField]
    [Tooltip("Показывать вращающийся значок.")]
    private bool showSpinner = true;

    [SerializeField]
    [Tooltip("Картинка значка. Пусто — значка нет.")]
    private Sprite spinnerIcon;

    [SerializeField, Min(8f)]
    [Tooltip("Сторона значка в единицах холста 1920×1080.")]
    private float spinnerSize = 220f;

    [SerializeField]
    [Tooltip("Скорость вращения, градусов в секунду. Ноль — значок стоит.")]
    private float spinnerSpeed = 300f;

    #region Свойства

    /// <summary>
    /// Экран загрузки показывается.
    /// </summary>
    public bool Enabled { get => enabled; set => enabled = value; }

    public Color BackgroundColor { get => backgroundColor; set => backgroundColor = value; }
    public Sprite BackgroundImage { get => backgroundImage; set => backgroundImage = value; }
    public LoadingScreenImageMode BackgroundImageMode { get => backgroundImageMode; set => backgroundImageMode = value; }
    public Color BackgroundImageColor { get => backgroundImageColor; set => backgroundImageColor = value; }

    public TMP_FontAsset Font { get => font; set => font = value; }
    public bool ShowTitle { get => showTitle; set => showTitle = value; }
    public Color TitleColor { get => titleColor; set => titleColor = value; }
    public float TitleSize { get => Mathf.Max(8f, titleSize); set => titleSize = Mathf.Max(8f, value); }
    public bool ShowDescription { get => showDescription; set => showDescription = value; }
    public Color DescriptionColor { get => descriptionColor; set => descriptionColor = value; }
    public float DescriptionSize { get => Mathf.Max(8f, descriptionSize); set => descriptionSize = Mathf.Max(8f, value); }

    public bool ShowProgressBar { get => showProgressBar; set => showProgressBar = value; }

    public Vector2 ProgressSize
    {
        get => new(Mathf.Max(8f, progressSize.x), Mathf.Max(4f, progressSize.y));
        set => progressSize = new Vector2(Mathf.Max(8f, value.x), Mathf.Max(4f, value.y));
    }

    public Color ProgressBackColor { get => progressBackColor; set => progressBackColor = value; }
    public Color ProgressFillColor { get => progressFillColor; set => progressFillColor = value; }
    public Sprite ProgressBackSprite { get => progressBackSprite; set => progressBackSprite = value; }
    public Sprite ProgressFillSprite { get => progressFillSprite; set => progressFillSprite = value; }

    public float ProgressPixelsPerUnit
    {
        get => Mathf.Max(0.1f, progressPixelsPerUnit);
        set => progressPixelsPerUnit = Mathf.Max(0.1f, value);
    }

    public bool ShowSpinner { get => showSpinner; set => showSpinner = value; }
    public Sprite SpinnerIcon { get => spinnerIcon; set => spinnerIcon = value; }
    public float SpinnerSize { get => Mathf.Max(8f, spinnerSize); set => spinnerSize = Mathf.Max(8f, value); }
    public float SpinnerSpeed { get => spinnerSpeed; set => spinnerSpeed = value; }

    #endregion

    #region Подписи

    /// <summary>
    /// Заголовок, вписанный в настройках на этом языке. Пустая строка — не вписан.
    /// </summary>
    public string GetTitle(LangType language) => Get(title, language);

    /// <summary>
    /// Вписывает заголовок на языке; пустое значение возвращает стандартный текст.
    /// </summary>
    public void SetTitle(LangType language, string value) => title = Set(title, "loading_title", language, value);

    /// <summary>
    /// Пояснение, вписанное в настройках на этом языке. Пустая строка — не вписано.
    /// </summary>
    public string GetDescription(LangType language) => Get(description, language);

    /// <summary>
    /// Вписывает пояснение на языке; пустое значение возвращает стандартный текст.
    /// </summary>
    public void SetDescription(LangType language, string value) =>
        description = Set(description, "loading_description", language, value);

    /// <summary>
    /// Заголовок, который увидит игрок: свой текст, а на языке без него — стандартный.
    /// </summary>
    public ILocalizationProvider Title => Resolve(title, LoadingScreenLabels.Title);

    /// <summary>
    /// Пояснение, которое увидит игрок: своё, а на языке без него — стандартное.
    /// </summary>
    public ILocalizationProvider Description => Resolve(description, LoadingScreenLabels.Description);

    private static string Get(LocalizationControl control, LangType language)
    {
        return control != null && control.LocalizationValues.TryGetValue(language, out string value)
            ? value ?? string.Empty
            : string.Empty;
    }

    /// <remarks>
    /// Пустое значение убирает язык из словаря, а не оставляет в нём пустую строку: по словарю
    /// решается, вписан ли свой текст, и пустая строка сошла бы за него.
    /// </remarks>
    private static LocalizationControl Set(LocalizationControl control, string key, LangType language, string value)
    {
        var values = new Dictionary<LangType, string>();

        if (control != null)
        {
            foreach (KeyValuePair<LangType, string> pair in control.LocalizationValues)
                values[pair.Key] = pair.Value;
        }

        if (string.IsNullOrWhiteSpace(value))
            values.Remove(language);
        else
            values[language] = value;

        return new LocalizationControl(key, values);
    }

    /// <summary>
    /// Собирает подпись по языкам: свой текст там, где он вписан, стандартный — где нет.
    /// </summary>
    /// <remarks>
    /// Без этого язык, на который текст не перевели, показал бы служебную пометку «перевод не найден».
    /// </remarks>
    private static ILocalizationProvider Resolve(LocalizationControl own, ILocalizationProvider fallback)
    {
        var values = new Dictionary<LangType, string>();

        foreach (KeyValuePair<LangType, string> pair in fallback.LocalizationValues)
            values[pair.Key] = pair.Value;

        if (own != null)
        {
            foreach (KeyValuePair<LangType, string> pair in own.LocalizationValues)
            {
                if (!string.IsNullOrWhiteSpace(pair.Value))
                    values[pair.Key] = pair.Value;
            }
        }

        return new LocalizationProvider(fallback.LocalizationKey, values);
    }

    #endregion
}
