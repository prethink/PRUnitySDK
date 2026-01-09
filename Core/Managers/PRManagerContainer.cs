public class PRManagerContainer 
{
    /// <summary>
    /// Игровой менеджер.
    /// </summary>
    public static GameManager GameManager;

    /// <summary>
    /// Менеджер управления свойств.
    /// </summary>
    public static ProjectPropertiesManager ProjectPropertiesManager;

    public void Initialize()
    {
        PRUnitySDK.InitializeType<GameManager>(() => { GameManager = GameManager.Instance; });
        PRUnitySDK.InitializeType<ProjectPropertiesManager>(() => { ProjectPropertiesManager = ProjectPropertiesManager.Instance; });

        this.RunMethodHooks(MethodHookStage.SDK);
    }
}
