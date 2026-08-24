public partial class PRManagerContainer
{
    /// <summary>
    /// Менеджер прогресса опыта и уровней.
    /// </summary>
    public XPManager XPManager;

    [MethodHook(MethodHookStage.PostOperation, 110)]
    public void InitializeXPManager()
    {
        PRUnitySDK.InitializeManager(() =>
        {
            XPManager = XPManager.Instance;
            XPManager.Init();
            return XPManager;
        });
    }
}

