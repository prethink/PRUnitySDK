public partial class PRSettingsContainer : DataContainer
{
    public PRProjectSettings ProjectSettings { get; protected set; }
    public PRGameSettings GameSettings { get; protected set; }
    public GameStorageSettings GameStorageSettings { get; protected set; }

    public void Initialize()
    {
        this.RunMethodHooks(MethodHookStage.PreOperation);

        Initialize<PRProjectSettings>(() => ProjectSettings = ResourcesUtils.GetOrCreateResourceSO<PRProjectSettings>());
        Initialize<PRGameSettings>(() => GameSettings = ResourcesUtils.GetOrCreateResourceSO<PRGameSettings>());
        Initialize<GameStorageSettings>(() => GameStorageSettings = ResourcesUtils.GetOrCreateResourceSO<GameStorageSettings>());

        this.RunMethodHooks(MethodHookStage.PostOperation);
    }
}
