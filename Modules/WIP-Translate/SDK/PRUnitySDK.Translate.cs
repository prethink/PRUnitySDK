public partial class PRUnitySDK
{
    #region Поля и свойства

    /// <summary>
    /// Приоритет.
    /// </summary>
    private const int PRIORITY_TRANSLATE = 60;

    /// <summary>
    /// Сервис переводов.
    /// </summary>
    public static ILanguageManager LanguageManager;

    /// <summary>
    /// Приоритет.
    /// </summary>
    private const int PRIORITY_LOCALIZE = 61;

    /// <summary>
    /// Сервис переводов.
    /// </summary>
    public static ILocalizationService LocalizationService;

    /// <summary>
    /// Приоритет.
    /// </summary>
    private const int PRIORITY_L_LOCALIZE = 62;

    /// <summary>
    /// Текущий язык.
    /// </summary>
    public static string CurrentLang { get; private set; } = "ru";

    /// <summary>
    /// Язык по умолчанию.
    /// </summary>
    public static string DefaultLanguage { get; private set; }

    #endregion

    #region Методы

    /// <summary>
    /// Установить текущий язык.
    /// </summary>
    /// <param name="lang">Язык.</param>
    public static void SetCurrentLang(string lang)
    {
        CurrentLang = lang;
    }

    /// <summary>
    /// Инициализация модуля.
    /// </summary>
    [MethodHook(MethodHookStage.SDK, PRIORITY_TRANSLATE)]
    private static void InitializeLanguageManager()
    {
        InitializeModuleSDK(nameof(ILanguageManager), () =>
        {
            typeof(PRUnitySDK).TryOverrideStaticProperty(typeof(ILanguageManager));

            InitializeDefault(nameof(LanguageManager), () => LanguageManager, () => { LanguageManager = new LanguageManager(); return LanguageManager; });

            return LanguageManager;
        });
    }

    /// <summary>
    /// Инициализация модуля.
    /// </summary>
    [MethodHook(MethodHookStage.SDK, PRIORITY_LOCALIZE)]
    private static void InitializeLocalizationService()
    {
        InitializeModuleSDK(nameof(ILocalizationService), () =>
        {
            typeof(PRUnitySDK).TryOverrideStaticProperty(typeof(ILocalizationService));

            InitializeDefault(nameof(LocalizationService), () => LocalizationService, () => { LocalizationService = new LocalizationService(); return LocalizationService; });

            return LocalizationService;
        });
    }

    /// <summary>
    /// Инициализация модуля.
    /// </summary>
    [MethodHook(MethodHookStage.SDK, PRIORITY_L_LOCALIZE)]
    private static void InitializeLocalization()
    {
        L.InitTranslate(LanguageManager);
        L.InitLocalizationService(LocalizationService);

        var defaultLanguage = LocalizationUtils.GetLanguageCode(Databases.Core.LocalizationDatabase.DefaultLanguage);
        DefaultLanguage = defaultLanguage;
        SetCurrentLang(defaultLanguage);
    }

    #endregion
}
