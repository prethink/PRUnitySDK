#if !PRSDK_DISABLE_GAMEPUSH
public partial class PRUnitySDK
{
    [OverrideProperty(typeof(IReviewService), PrioritySDK.OVERRIDE_PROPERTY_GP_PRIORITY)]
    private static void InitializeReviewOverrideGP()
    {
        Review = new GamePushReviewService();
    }
}
#endif
