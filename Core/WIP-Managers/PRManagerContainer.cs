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
    /// Менеджер звуков.
    /// </summary>
    public SoundManager SoundManager;

    /// <summary>
    /// Менеджер аудиомиксера.
    /// </summary>
    public AudioMixerManager AudioMixerManager;

    /// <summary>
    /// Менеджер открытых предметов.
    /// </summary>
    public OpenedItemsManager OpenedItemsManager;

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

    [MethodHook(MethodHookStage.PostOperation, 20)]
    public void InitializeAudioMixerManager()
    {
        PRUnitySDK.InitializeType<AudioMixerManager>(() => 
        {
            AudioMixerManager = MonoBehaviourUtils.CreateMonoBehaviourDontDestroyOnLoad(AudioMixerManager.Factory);
            AudioMixerManager.transform.SetParent(Container.transform);
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 30)]
    public void InitializeSoundManager()
    {
        PRUnitySDK.InitializeType<SoundManager>(() => 
        {
            SoundManager = MonoBehaviourUtils.CreateMonoBehaviourDontDestroyOnLoad(SoundManager.Factory);
            SoundManager.transform.SetParent(Container.transform);
            AudioMixerManager.RegisterSoundManager(SoundManager);
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
}
