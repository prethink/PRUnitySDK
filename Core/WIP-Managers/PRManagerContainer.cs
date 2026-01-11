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

    /// <summary>
    /// Контейнер для менеджеров.   
    /// </summary>
    public PRContainer Container;

    public void Initialize()
    {
        this.RunMethodHooks(MethodHookStage.PreOperation);

        Container = MonoBehaviourUtils.CreateContainer("Managers");

        this.RunMethodHooks(MethodHookStage.PostOperation);
    }

    [MethodHook(MethodHookStage.PostOperation, 10)]
    public void InitializeGameManager()
    {
        PRUnitySDK.InitializeType<GameManager>(() =>
        {
            GameManager = GameManager.Instance;
            GameManager.transform.SetParent(Container.transform);
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 20)]
    public void InitializeProjectPropertiesManager()
    {
        PRUnitySDK.InitializeType<ProjectPropertiesManager>(() => { ProjectPropertiesManager = ProjectPropertiesManager.Instance; });
    }
}
