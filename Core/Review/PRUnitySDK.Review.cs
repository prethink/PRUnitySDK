public partial class PRUnitySDK
{
    #region Поля и свойства

    /// <summary>
    /// Приоритет.
    /// </summary>
    private const int PRIORITY_REVIEW = 10;

    /// <summary>
    /// Оценка игры на площадке.
    /// </summary>
    public static IReviewService Review;

    #endregion

    #region Методы

    /// <summary>
    /// Инициализация модуля.
    /// </summary>
    [MethodHook(MethodHookStage.SDK, PRIORITY_REVIEW)]
    private static void InitializeReview()
    {
        InitializeModuleSDK(nameof(IReviewService), () =>
        {
            typeof(PRUnitySDK).TryOverrideStaticProperty(typeof(IReviewService));

            InitializeDefault(nameof(IReviewService), () => Review, () => { Review = new UnavailableReviewService(); return Review; });

            return Review;
        });
    }

    #endregion
}
