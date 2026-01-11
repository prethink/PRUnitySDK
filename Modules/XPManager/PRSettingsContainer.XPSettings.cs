public partial class PRSettingsContainer
{
    public XPSettings XPSettings { get; protected set; }

    [MethodHook(MethodHookStage.PostOperation)]
    public void InitializeXPSettings()
    {
        Initialize<XPSettings>(() => XPSettings = ResourcesUtils.GetOrCreateResourceSO<XPSettings>());
    }
}
