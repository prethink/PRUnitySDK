public partial class PRUnitySDK
{
    #region Поля и свойства

    /// <summary>
    /// Приоритет.
    /// </summary>
    private const int PRIORITY_METRICS = 40;

    /// <summary>
    /// Метрики.
    /// </summary>
    public static MetricBase Metric;

    #endregion

    #region Методы

    /// <summary>
    /// Инициализация модуля.
    /// </summary>
    [MethodHook(MethodHookStage.SDK, PRIORITY_METRICS)]
    private static void InitializeMetrics()
    {
        InitializeModuleSDK(nameof(MetricBase), () =>
        {
            typeof(PRUnitySDK).TryOverrideStaticProperty(typeof(MetricBase));

            InitializeDefault(nameof(Metric), () => Metric, () => { Metric = new DummyMetric(); return Metric; });

            return Metric;
        });
    }

    #endregion
}
