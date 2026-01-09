public partial class PRManagerContainer
{
    /// <summary>
    /// Менеджер управления свойств.
    /// </summary>
    public XPManager XPManager;

    [MethodHook(MethodHookStage.SDK)]
    public void InitializeXPManager()
    {
        PRUnitySDK.InitializeType<XPManager>(() =>
        {
            XPManager = XPManager.Instance;
            XPManager.Init();
        });
    }
}
