using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Набор сервисов PR SDK.
/// </summary>
public partial class PRUnitySDK
{
    /// <summary>
    /// Инициализированные типы.
    /// </summary>
    public readonly static HashSet<Type> InitializedTypes = new();

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

        InitializeConverters();

        // На всякий случай инициализируем настройки и базу данных заранее.
        var initSettings = Settings;
        var initDatabase = Database;

        typeof(PRUnitySDK).RunStaticMethodHooks(MethodHookStage.SDK);

        Managers.Initialize();

        IsInitialized = true;
        EventBus.RaiseEvent<ISDKEvents>(x => x.OnInitialized());
        PRLog.WriteDebug(typeof(PRUnitySDK), $"Initialize SDK complete.");
    }

    /// <summary>
    /// Инициализация конвертеров.
    /// </summary>
    private static void InitializeConverters()
    {
        typeof(PRUnitySDK).RunStaticMethodHooks(MethodHookStage.Converter);
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
    public static void InitializeType<T>(Action action, string name = null)
    {
        var result = InitializedTypes.Add((typeof(T)));
        if(!result)
            PRLog.WriteWarning(typeof(PRUnitySDK), $"Type {typeof(T)} already initialized.");
        else
            PRLog.WriteDebug(typeof(PRUnitySDK), $"Initialize complete <color={Color.yellow}>{(string.IsNullOrEmpty(name) ? typeof(T).Name : name)}</color>.");
        
        action?.Invoke();
    }

    /// <summary>
    /// Инициализация модуля SDK.
    /// </summary>
    /// <param name="name">Название модуля.</param>
    /// <param name="initializeAction">Метод инициализации.</param>
    private static void InitializeModuleSDK<T>(string name, Func<T> initializeAction)
    {
        try
        {
            InitializeType<T>(() => { RegisterService(initializeAction.Invoke()); }, name);
        }
        catch(Exception exception)
        {
            PRLog.WriteError(typeof(PRUnitySDK), $"Cannot initialize module <color={Color.yellow}>{name}</color>. {exception}");
            throw;
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
}
