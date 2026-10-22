#if !PRSDK_DISABLE_GAMEPUSH
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(MetricBase), PrioritySDK.OVERRIDE_PROPERTY_GP_PRIORITY)]
    private static void InitializeMetricsOverrideGP()
    {
        Metric = new GamePushMetrics();
    }
}
#endif
