public partial class PRManagerContainer 
{
    /// <summary>
    /// Игровой менеджер.
    /// </summary>
    public GameManager Game;

    /// <summary>
    /// Менеджер управления свойств.
    /// </summary>
    public ProjectPropertiesManager ProjectProperties;

    /// <summary>
    /// Менеджер управления ресурсами.
    /// </summary>
    public ResourceManager Resource;

    /// <summary>
    /// Менеджер звуков.
    /// </summary>
    public SoundManager Sound;

    /// <summary>
    /// Pool Manager.
    /// </summary>
    public ObjectPoolManager ObjectPool;

    /// <summary>
    /// Менеджер аудиомиксера.
    /// </summary>
    public AudioMixerManager AudioMixer;

    /// <summary>
    /// Менеджер открытых предметов.
    /// </summary>
    public OpenedItemsManager OpenedItems;

    /// <summary>
    /// Менеджер флагов в игре.
    /// </summary>
    public FlagsManager Flags;

    /// <summary>
    /// Контейнер для менеджеров.   
    /// </summary>
    public PRContainer ManagerContainer;

    public void Initialize()
    {
        this.RunMethodHooks(MethodHookStage.PreOperation);

        ManagerContainer = MonoBehaviourUtils.CreateContainer("Managers");

        this.RunMethodHooks(MethodHookStage.PostOperation);
    }

    [MethodHook(MethodHookStage.PostOperation, 10)]
    public void InitializeGameManager()
    {
        PRUnitySDK.InitializeType<GameManager>(() =>
        {
            Game = GameManager.Instance;
            Game.InitializeGameManager();
            Game.transform.SetParent(ManagerContainer.transform);
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 20)]
    public void InitializeProjectPropertiesManager()
    {
        PRUnitySDK.InitializeType<ProjectPropertiesManager>(() => { ProjectProperties = ProjectPropertiesManager.Instance; });
    }

    [MethodHook(MethodHookStage.PostOperation, 20)]
    public void InitializeResourceManager()
    {
        PRUnitySDK.InitializeType<ResourceManager>(() => { Resource = ResourceManager.Instance; });
    }

    [MethodHook(MethodHookStage.PostOperation, 20)]
    public void InitializeAudioMixerManager()
    {
        PRUnitySDK.InitializeType<AudioMixerManager>(() => 
        {
            AudioMixer = MonoBehaviourUtils.CreateMonoBehaviourDontDestroyOnLoad(AudioMixerManager.Factory);
            AudioMixer.transform.SetParent(ManagerContainer.transform);
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 30)]
    public void InitializeSoundManager()
    {
        PRUnitySDK.InitializeType<SoundManager>(() => 
        {
            Sound = MonoBehaviourUtils.CreateMonoBehaviourDontDestroyOnLoad(SoundManager.Factory);
            Sound.transform.SetParent(ManagerContainer.transform);
            AudioMixer.RegisterSoundManager(Sound);
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 35)]
    public void InitializeObjectPollManager()
    {
        PRUnitySDK.InitializeType<ObjectPoolManager>(() =>
        {
            ObjectPool = MonoBehaviourUtils.CreateMonoBehaviourDontDestroyOnLoad(ObjectPoolManager.Factory);
            ObjectPool.transform.SetParent(ManagerContainer.transform);
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 40)]
    public void InitializeOpenItemManager()
    {
        PRUnitySDK.InitializeType<OpenedItemsManager>(() =>
        {
            OpenedItems = OpenedItemsManager.Instance;
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 50)]
    public void InitializeFlagsManager()
    {
        PRUnitySDK.InitializeType<FlagsManager>(() =>
        {
            Flags = FlagsManager.Instance;
            Flags.transform.SetParent(ManagerContainer.transform);
        });
    }
}
