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
    /// Менеджер управления ресурсами.
    /// </summary>
    public ResourceManager ResourceManager;

    /// <summary>
    /// Менеджер звуков.
    /// </summary>
    public SoundManager SoundManager;

    /// <summary>
    /// Pool Manager.
    /// </summary>
    public ObjectPoolManager ObjectPoolManager;

    /// <summary>
    /// Менеджер аудиомиксера.
    /// </summary>
    public AudioMixerManager AudioMixerManager;

    /// <summary>
    /// Менеджер открытых предметов.
    /// </summary>
    public OpenedItemsManager OpenedItemsManager;

    /// <summary>
    /// Менеджер флагов в игре.
    /// </summary>
    public FlagsManager FlagsManager;

    /// <summary>
    /// Контейнер для менеджеров.   
    /// </summary>
    public PRContainer ManagerContainer;

    /// <summary>
    /// Контейнер для окон.   
    /// </summary>
    public PRContainer WindowsContainer;

    public void Initialize()
    {
        this.RunMethodHooks(MethodHookStage.PreOperation);

        ManagerContainer = MonoBehaviourUtils.CreateContainer("Managers");
        WindowsContainer = MonoBehaviourUtils.CreateContainer("Windows");

        this.RunMethodHooks(MethodHookStage.PostOperation);
    }

    [MethodHook(MethodHookStage.PostOperation, 10)]
    public void InitializeGameManager()
    {
        PRUnitySDK.InitializeType<GameManager>(() =>
        {
            GameManager = GameManager.Instance;
            GameManager.InitializeGameManager();
            GameManager.transform.SetParent(ManagerContainer.transform);
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 20)]
    public void InitializeProjectPropertiesManager()
    {
        PRUnitySDK.InitializeType<ProjectPropertiesManager>(() => { ProjectPropertiesManager = ProjectPropertiesManager.Instance; });
    }

    [MethodHook(MethodHookStage.PostOperation, 20)]
    public void InitializeResourceManager()
    {
        PRUnitySDK.InitializeType<ResourceManager>(() => { ResourceManager = ResourceManager.Instance; });
    }

    [MethodHook(MethodHookStage.PostOperation, 20)]
    public void InitializeAudioMixerManager()
    {
        PRUnitySDK.InitializeType<AudioMixerManager>(() => 
        {
            AudioMixerManager = MonoBehaviourUtils.CreateMonoBehaviourDontDestroyOnLoad(AudioMixerManager.Factory);
            AudioMixerManager.transform.SetParent(ManagerContainer.transform);
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 30)]
    public void InitializeSoundManager()
    {
        PRUnitySDK.InitializeType<SoundManager>(() => 
        {
            SoundManager = MonoBehaviourUtils.CreateMonoBehaviourDontDestroyOnLoad(SoundManager.Factory);
            SoundManager.transform.SetParent(ManagerContainer.transform);
            AudioMixerManager.RegisterSoundManager(SoundManager);
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 35)]
    public void InitializeObjectPollManager()
    {
        PRUnitySDK.InitializeType<ObjectPoolManager>(() =>
        {
            ObjectPoolManager = MonoBehaviourUtils.CreateMonoBehaviourDontDestroyOnLoad(ObjectPoolManager.Factory);
            ObjectPoolManager.transform.SetParent(ManagerContainer.transform);
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 40)]
    public void InitializeOpenItemManager()
    {
        PRUnitySDK.InitializeType<OpenedItemsManager>(() =>
        {
            OpenedItemsManager = OpenedItemsManager.Instance;
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 50)]
    public void InitializeFlagsManager()
    {
        PRUnitySDK.InitializeType<FlagsManager>(() =>
        {
            FlagsManager = FlagsManager.Instance;
            FlagsManager.transform.SetParent(ManagerContainer.transform);
        });
    }
}
