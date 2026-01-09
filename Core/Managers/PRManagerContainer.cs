public partial class PRManagerContainer 
{
    /// <summary>
    /// Игровой менеджер.
    /// </summary>
    public GameManager GameManager;

    /// <summary>
    /// Менеджер управления свойств.
    /// </summary>
    public ProjectPropertiesManager ProjectPropertiesManager;

    public void Initialize()
    {
        PRUnitySDK.InitializeType<GameManager>(() => { GameManager = GameManager.Instance; });
        PRUnitySDK.InitializeType<ProjectPropertiesManager>(() => { ProjectPropertiesManager = ProjectPropertiesManager.Instance; });

        this.RunMethodHooks(MethodHookStage.SDK);
    }
}
