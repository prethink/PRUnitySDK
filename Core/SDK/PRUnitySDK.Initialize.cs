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

    private static readonly HashSet<Type> initializingTypes = new();

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

    private static readonly List<KeyValuePair<string, double>> initializationSteps = new();

    /// <summary>
    /// Шаги сборки SDK в порядке выполнения и длительность каждого, миллисекунды.
    /// </summary>
    /// <remarks>
    /// Покрывает всю сборку без пропусков, в отличие от <see cref="InitializationHistory"/>: туда
    /// попадают только менеджеры и модули, а работа между ними — нет.
    /// </remarks>
    public static IReadOnlyList<KeyValuePair<string, double>> InitializationSteps => initializationSteps;

    /// <summary>
    /// Шаги сборки, которые есть всегда: правила, конвертеры, синглтоны, фабрики, фоновые задачи, готовность.
    /// </summary>
    private const int FixedStepCount = 6;

    /// <summary>
    /// Сколько шагов в сборке SDK: по нему считают долю пройденного.
    /// </summary>
    public static int InitializationStepCount =>
        FixedStepCount
        + typeof(PRUnitySDK).CountStaticMethodHooks(MethodHookStage.SDK)
        + Managers.InitializationStepCount
        + Windows.InitializationStepCount;

    /// <summary>
    /// Инициализация SDK одним вызовом.
    /// </summary>
    public static void InitializeSDK()
    {
        IEnumerator<string> steps = InitializeSDKSteps();

        while (steps.MoveNext())
        {
        }
    }

    /// <summary>
    /// Инициализация SDK по шагам: каждый <c>MoveNext</c> выполняет один шаг и отдаёт его имя.
    /// </summary>
    /// <remarks>
    /// Для загрузчика, который растягивает сборку по кадрам, чтобы экран загрузки не замирал на всё
    /// её время. Перечень и порядок шагов те же, что у <see cref="InitializeSDK"/>, — тот просто
    /// проходит их подряд.
    /// <para>
    /// Готовым SDK становится только на последнем шаге. До него <see cref="IsInitialized"/> ложно,
    /// общий цикл обновления SDK не идёт, а подписчики <see cref="ReadySignal"/> ничего не получают —
    /// сколько бы кадров ни прошло между шагами.
    /// </para>
    /// </remarks>
    public static IEnumerator<string> InitializeSDKSteps()
    {
        if (!IsStartInitialize)
            initializationSteps.Clear();

        IEnumerator<string> steps = RunInitialization();
        var stopwatch = new System.Diagnostics.Stopwatch();

        while (true)
        {
            stopwatch.Restart();

            if (!steps.MoveNext())
                yield break;

            initializationSteps.Add(new KeyValuePair<string, double>(steps.Current, stopwatch.Elapsed.TotalMilliseconds));
            yield return steps.Current;
        }
    }

    private static IEnumerator<string> RunInitialization()
    {
        if (IsStartInitialize)
        {
            PRLog.WriteWarning(typeof(PRUnitySDK), $"Initialization already started.");
            yield break;
        }

        IsStartInitialize = true;
        if (IsInitialized)
        {
            PRLog.WriteWarning(typeof(PRUnitySDK), $"Already is initialized.");
            yield break;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        GameRules.Initialize();
        yield return nameof(GameRules);

        InitializeConverters();
        yield return "Converters";

        InitializeSingletons();
        yield return "Singletons";

        RegisterFactories();
        yield return "Factories";

        foreach (string step in typeof(PRUnitySDK).RunStaticMethodHooksStepwise(MethodHookStage.SDK))
            yield return step;

        foreach (string step in Managers.InitializeSteps())
            yield return step;

        foreach (string step in Windows.InitializeSteps())
            yield return step;

        // Задачи регистрируются после менеджеров, но до IsInitialized: трекер не выполняет
        // их, пока SDK не готов, поэтому первый запуск гарантированно придётся на
        // полностью инициализированный проект.
        Trackers.BackgroundTasks.RegisterAutoTasks();
        yield return "BackgroundTasks";

        IsInitialized = true;
        EventBus.RaiseEvent<ISDKEvents>(x => x.OnInitialized());
        readySignal.SetReady();
        PRLog.WriteDebug(typeof(PRUnitySDK), $"Initialize SDK complete. in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");
        stopwatch.Stop();

        // Отдельным шагом, чтобы в замер попали и подписчики готовности.
        yield return "Ready";
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
        if (InitializedTypes.Contains(typeof(T)) || !initializingTypes.Add(typeof(T)))
        {
            PRLog.WriteWarning(typeof(PRUnitySDK), $"Type {typeof(T)} is already initialized or initializing.");
            return;
        }

        try
        {
            string displayName = string.IsNullOrEmpty(name) ? typeof(T).Name : name;
            double durationMilliseconds = TrackInitialization<T>(displayName, category,
                () => (object)(initializeAction == null ? default : initializeAction.Invoke()));
            InitializedTypes.Add(typeof(T));
            PRLog.WriteDebug(typeof(PRUnitySDK), $"Initialize complete <color={Color.yellow}>{displayName}</color> in {durationMilliseconds:F2} ms.");
        }
        finally
        {
            initializingTypes.Remove(typeof(T));
        }
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
