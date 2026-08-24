using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Набор сервисов PR SDK.
/// </summary>
public partial class PRUnitySDK
{
    private static readonly List<PRInitializationInfo> initializationHistory = new();
    private static readonly IReadOnlyList<PRInitializationInfo> initializationHistoryView = initializationHistory.AsReadOnly();

    /// <summary>
    /// Инициализированные типы.
    /// </summary>
    public readonly static HashSet<Type> InitializedTypes = new();

    /// <summary>
    /// Диагностические данные успешно завершённых элементов инициализации SDK в порядке их запуска.
    /// </summary>
    public static IReadOnlyList<PRInitializationInfo> InitializationHistory => initializationHistoryView;

    /// <summary>
    /// Признак, что SDK инициализирован.
    /// </summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>
    /// Признак, что SDK начал инициализацию. 
    /// Предотвращает повторный/одновременный запуск инициализации.
    /// </summary>
    public static bool IsStartInitialize { get; private set; }

    /// <summary>
    /// Сигнал готовности SDK.
    /// </summary>
    public static IReadySignal ReadySignal => readySignal;

    /// <summary>
    /// Сигнал готовности SDK.
    /// </summary>
    private static ReadySignal readySignal = new ReadySignal(typeof(PRUnitySDK));

    /// <summary>
    /// Инициализация SDK.
    /// </summary>
    public static void InitializeSDK()
    {
        if (IsStartInitialize)
        {
            PRLog.WriteWarning(typeof(PRUnitySDK), $"Initialization already started.");
            return;
        }

        IsStartInitialize = true;
        if (IsInitialized)
        {
            PRLog.WriteWarning(typeof(PRUnitySDK), $"Already is initialized.");
            return;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();


        GameRules.Initialize();
        InitializeConverters();
        InitializeSingletons();
        RegisterFactories();

        typeof(PRUnitySDK).RunStaticMethodHooks(MethodHookStage.SDK);

        Managers.Initialize();
        Windows.Initialize();
        IsInitialized = true;
        EventBus.RaiseEvent<ISDKEvents>(x => x.OnInitialized());
        readySignal.SetReady();
        PRLog.WriteDebug(typeof(PRUnitySDK), $"Initialize SDK complete. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
        stopwatch.Stop();
    }

    private static void InitializeSingletons()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        TrackInitialization<PRMonoBehaviourHost>(nameof(PRMonoBehaviourHost), PRInitializationCategory.Singleton,
            () =>
            {
                var instance = PRMonoBehaviourHost.Instance;
                instance.SingletonInitialize();
                return instance;
            });
        TrackInitialization<PRTimeScale>(nameof(PRTimeScale), PRInitializationCategory.Singleton,
            () =>
            {
                var instance = PRTimeScale.Instance;
                instance.SingletonInitialize();
                return instance;
            });
        PRLog.WriteDebug(typeof(PRUnitySDK), $"Initialize InitializeSingletons complete. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
        stopwatch.Stop();
    }

    /// <summary>
    /// Инициализация конвертеров.
    /// </summary>
    private static void InitializeConverters()
    {
        typeof(PRUnitySDK).RunStaticMethodHooks(MethodHookStage.Converter);
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            Converters =
            {
                new IIdentifiableItemConverter()
            }
        };
    }

    /// <summary>
    /// Признак, что сервис инициализирован.
    /// </summary>
    /// <param name="service">Тип сервиса.</param>
    /// <returns>True если проинициализирован, False - если нет.</returns>
    public static bool IsInitialize(Type service)
    {
        return InitializedTypes.Contains(service);
    }

    /// <summary>
    /// Признак, что сервис инициализирован.
    /// </summary>
    /// <typeparam name="T">Тип.</typeparam>
    /// <returns>True если проинициализирован, False - если нет.</returns>
    public static bool IsInitialize<T>()
    {
        return IsInitialize(typeof(T));
    }

    /// <summary>
    /// Установить признак, что тип инициализирован.
    /// </summary>
    /// <typeparam name="T">Тип.</typeparam>
    /// <param name="action">Кастомное действие.</param>
    /// <param name="name">Отображаемое имя.</param>
    public static void InitializeType<T>(Action action, string name = null)
    {
        InitializeTrackedType<T>(() =>
        {
            action?.Invoke();
            return default;
        }, name, PRInitializationCategory.Type);
    }

    /// <summary>
    /// Инициализирует manager и автоматически сохраняет его фактический тип.
    /// </summary>
    internal static void InitializeManager<T>(Func<T> initializeAction, string name = null)
    {
        InitializeTrackedType(initializeAction, name, PRInitializationCategory.Manager);
    }

    private static void InitializeTrackedType<T>(Func<T> initializeAction, string name,
        PRInitializationCategory category)
    {
        var result = InitializedTypes.Add(typeof(T));

        if (!result)
        {
            PRLog.WriteWarning(typeof(PRUnitySDK), $"Type {typeof(T)} already initialized.");
            initializeAction?.Invoke();
            return;
        }

        string displayName = string.IsNullOrEmpty(name) ? typeof(T).Name : name;
        double durationMilliseconds = TrackInitialization<T>(displayName, category,
            () => (object)(initializeAction == null ? default : initializeAction.Invoke()));
        PRLog.WriteDebug(typeof(PRUnitySDK), $"Initialize complete <color={Color.yellow}>{displayName}</color> in {durationMilliseconds:F2} ms.");
    }

    /// <summary>
    /// Инициализация модуля SDK.
    /// </summary>
    /// <param name="name">Название модуля.</param>
    /// <param name="initializeAction">Метод инициализации.</param>
    private static void InitializeModuleSDK<T>(string name, Func<T> initializeAction)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            InitializeTrackedType<T>(() =>
            {
                T implementation = initializeAction.Invoke();
                RegisterService(implementation);
                return implementation;
            }, name, PRInitializationCategory.Module);
        }
        catch (Exception exception)
        {
            PRLog.WriteError(typeof(PRUnitySDK), $"Cannot initialize module <color={Color.yellow}>{name}</color>. {exception}");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            PRLog.WriteDebug(typeof(PRUnitySDK), $"Module <color={Color.yellow}>{name}</color> initialized in {stopwatch.ElapsedMilliseconds} ms");
        }
    }

    private static void InitializeDefault<T>(string name, Func<T> getProperty, Func<T> setProperty)
    {
        if(getProperty() == null)
        {
            var result = setProperty();
            PRLog.WriteDebug(typeof(PRUnitySDK), $"Initialize <color={Color.yellow}>{name}</color> implement {result.GetType()}.", new PRLogSettings() { LevelDebug = 8 });
        }
    }
    
    private static void RegisterFactories()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        ScreenFade.RegisterFactory(new ScreenFadeFactory());
        typeof(PRUnitySDK).RunStaticMethodHooks(MethodHookStage.RegisterFactories);

        PRLog.WriteDebug(typeof(PRUnitySDK), $"Initialize RegisterFactories complete. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
        stopwatch.Stop();
    }

    /// <summary>
    /// Выполняет операцию и сохраняет её длительность в общей диагностике инициализации.
    /// </summary>
    internal static double TrackInitialization<TContract>(string name,
        PRInitializationCategory category, Func<object> initializeAction)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        object implementation = initializeAction?.Invoke();
        stopwatch.Stop();

        initializationHistory.Add(new PRInitializationInfo(category, name, typeof(TContract),
            implementation?.GetType() ?? typeof(TContract), stopwatch.Elapsed.TotalMilliseconds));
        return stopwatch.Elapsed.TotalMilliseconds;
    }
}
