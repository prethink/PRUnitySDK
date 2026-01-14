public partial class PRSettingsContainer : DataContainer
{
    public PRProjectSettings ProjectSettings { get; protected set; }
    public PRGameSettings GameSettings { get; protected set; }
    public void Initialize()
    {
        this.RunMethodHooks(MethodHookStage.PreOperation);

        Initialize<PRGameSettings>(() => GameSettings = ResourcesUtils.GetOrCreateResourceSO<PRGameSettings>());

        this.RunMethodHooks(MethodHookStage.PostOperation);
    }
}
