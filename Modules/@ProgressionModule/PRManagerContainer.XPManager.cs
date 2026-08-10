public partial class PRManagerContainer
{
    /// <summary>
    /// Менеджер прогресса опыта и уровней.
    /// </summary>
    public XPManager XPManager;

    [MethodHook(MethodHookStage.PostOperation, 110)]
    public void InitializeXPManager()
    {
        PRUnitySDK.InitializeType<XPManager>(() =>
        {
            XPManager = XPManager.Instance;
            XPManager.Init();
        });
    }
}

