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
    private const int PRIORITY_L_LOCALIZE = 62;

    /// <summary>
    /// Текущий язык.
    /// </summary>
    public static string CurrentLang => PRUnitySDK.LanguageManager.GetCurrentLang();

    /// <summary>
    /// Язык по умолчанию.
    /// </summary>
    public static string DefaultLanguage { get; private set; }

    #endregion

    #region Методы

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
    [MethodHook(MethodHookStage.SDK, PRIORITY_L_LOCALIZE)]
    private static void InitializeLocalization()
    {
        L.InitTranslate(LanguageManager);
        DefaultLanguage = LocalizationUtils.GetLanguageCode(Database.LocalizationDatabase.DefaultLanguage);
        //if (PRUnitySDK.Settings.Project.ReleaseType == ReleaseType.Debug)
        //    LanguageManager.SwitchLang(DefaultLanguage);
    }

    #endregion
}
