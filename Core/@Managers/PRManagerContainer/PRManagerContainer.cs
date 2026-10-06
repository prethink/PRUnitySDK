using System;
using UnityEngine;

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
    /// Что выбрано каждым локальным игроком.
    /// </summary>
    public SelectedItemsManager SelectedItems;

    /// <summary>
    /// Менеджер флагов в игре.
    /// </summary>
    public FlagsManager Flags;

    /// <summary>
    /// Предметы, которые выдаются не покупкой.
    /// </summary>
    public ReservedItemsManager ReservedItems;

    /// <summary>
    /// Контейнер для менеджеров.   
    /// </summary>
    public PRContainer ManagerContainer;

    public void Initialize()
    {
        foreach (string step in InitializeSteps())
        {
        }
    }

    /// <summary>
    /// Сколько шагов у <see cref="InitializeSteps"/>.
    /// </summary>
    public int InitializationStepCount => 1 + this.CountMethodHooks(MethodHookStage.PostOperation);

    /// <summary>
    /// Создаёт менеджеры по одному и после каждого отдаёт имя выполненного шага.
    /// </summary>
    /// <remarks>
    /// Тот же порядок, что у <see cref="Initialize"/>; нужен загрузчику, который растягивает
    /// сборку SDK по кадрам.
    /// </remarks>
    public System.Collections.Generic.IEnumerable<string> InitializeSteps()
    {
        this.RunMethodHooks(MethodHookStage.PreOperation);

        ManagerContainer = MonoBehaviourUtils.CreateContainer("Managers");
        yield return "Managers";

        foreach (string step in this.RunMethodHooksStepwise(MethodHookStage.PostOperation))
            yield return step;
    }

    [MethodHook(MethodHookStage.PostOperation, 10)]
    public void InitializeGameManager()
    {
        InitializeMonoManager(() =>
        {
            Game = GameManager.Instance;
            Game.InitializeGameManager();
            return Game;
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 20)]
    public void InitializeProjectPropertiesManager()
    {
        PRUnitySDK.InitializeManager(() =>
        {
            ProjectProperties = ProjectPropertiesManager.Instance;
            return ProjectProperties;
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 20)]
    public void InitializeResourceManager()
    {
        PRUnitySDK.InitializeManager(() =>
        {
            Resource = ResourceManager.Instance;
            return Resource;
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 20)]
    public void InitializeAudioMixerManager()
    {
        InitializeMonoManager(() =>
        {
            AudioMixer = MonoBehaviourUtils.CreateMonoBehaviourDontDestroyOnLoad(new AudioMixerManagerFactory());
            return AudioMixer;
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 30)]
    public void InitializeSoundManager()
    {
        InitializeMonoManager(() =>
        {
            Sound = MonoBehaviourUtils.CreateMonoBehaviourDontDestroyOnLoad(new SoundManagerFactory());
            AudioMixer.RegisterSoundManager(Sound);
            return Sound;
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 35)]
    public void InitializeObjectPollManager()
    {
        InitializeMonoManager(() =>
        {
            ObjectPool = MonoBehaviourUtils.CreateMonoBehaviourDontDestroyOnLoad(new ObjectPoolManagerFactory());
            return ObjectPool;
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 40)]
    public void InitializeOpenItemManager()
    {
        PRUnitySDK.InitializeManager(() =>
        {
            OpenedItems = OpenedItemsManager.Instance;
            return OpenedItems;
        });
    }

    /// <summary>
    /// Создаётся после менеджера открытых предметов: выбор опирается на то, что уже есть.
    /// </summary>
    [MethodHook(MethodHookStage.PostOperation, 41)]
    public void InitializeSelectedItemsManager()
    {
        PRUnitySDK.InitializeManager(() =>
        {
            SelectedItems = SelectedItemsManager.Instance;
            return SelectedItems;
        });
    }

    /// <summary>
    /// Создаётся раньше систем, которые выдают предметы: они регистрируются в нём сами.
    /// </summary>
    [MethodHook(MethodHookStage.PostOperation, 42)]
    public void InitializeReservedItemsManager()
    {
        PRUnitySDK.InitializeManager(() =>
        {
            ReservedItems = ReservedItemsManager.Instance;
            ReservedItems.RegisterAutoProviders();
            PRUnitySDK.RegisterService(ReservedItems);
            return ReservedItems;
        });
    }

    [MethodHook(MethodHookStage.PostOperation, 50)]
    public void InitializeFlagsManager()
    {
        InitializeMonoManager(() =>
        {
            Flags = FlagsManager.Instance;
            return Flags;
        });
    }

    public void InitializeMonoManager<T>(Func<T> factory) where T : MonoBehaviour
    {
        PRUnitySDK.InitializeManager(() =>
        {
            var instance = factory();
            instance.transform.SetParent(ManagerContainer.transform);
            return instance;
        });

    }
}
