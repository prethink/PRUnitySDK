#if !PRSDK_DISABLE_YG2
#if Review_yg
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(IReviewService), PrioritySDK.OVERRIDE_PROPERTY_YG_PRIORITY)]
    private static void InitializeReviewOverrideYG()
    {
        Review = new YandexReviewService();
    }
}
#endif
#endif
