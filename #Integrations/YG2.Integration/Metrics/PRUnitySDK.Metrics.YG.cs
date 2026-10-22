#if !PRSDK_DISABLE_YG2
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(MetricBase), PrioritySDK.OVERRIDE_PROPERTY_YG_PRIORITY)]
    private static void InitializeMetricsOverrideYG()
    {
        Metric = new YGMetrics();
    }
}
#endif
