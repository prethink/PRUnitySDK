using System;
using UnityEngine;

/// <summary>
/// Набор сервисов PR SDK.
/// </summary>
public partial class PRUnitySDK
{
    /// <summary>
    /// Признак, что SDK инициализирован.
    /// </summary>
    public static bool IsInitialized { get; private set; }

    /// <summary>
    /// Признак, что SDK начал иницилаизаци.
    /// </summary>
    public static bool IsStartInitialize { get; private set; }

    /// <summary>
    /// Инициализация SDK.
    /// </summary>
    //[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
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
        LoadSettings();
        typeof(PRUnitySDK).InvokePartialStaticMethods(InitializeType.SDK);

        IsInitialized = true;
        EventBus.RaiseEvent<ISDKEvents>(x => x.OnInitialized());
        PRLog.WriteDebug(typeof(PRUnitySDK), $"Initialize SDK complete.");
    }

    private static void LoadSettings()
    {
        try
        {
            GameSettings = ResourcesUtils.LoadSingleFromResources<PRGameSettings>();
            Database = ResourcesUtils.LoadSingleFromResources<PRDatabase>();

            RegisterService(GameSettings);
            RegisterService(Database);
        }
        catch(ResourceNotFoundException resourceNotFoundException)
        {
            PRLog.WriteError(typeof(PRUnitySDK), $"Required create {resourceNotFoundException.Type} in 'Resources' folder. Use Assets/Create/PRUnitySDK/...");
            throw;
        }
        catch(MultipleResourcesFoundException multipleResourcesFoundException)
        {
            PRLog.WriteError(typeof(PRUnitySDK), $"More than one {multipleResourcesFoundException.Type} found in 'Resources' folder.");
            throw;
        }
        catch (Exception exception)
        {
            PRLog.WriteError(typeof(PRUnitySDK), $"{exception}");
            throw;
        }
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
            PRLog.WriteDebug(typeof(PRUnitySDK), $"Try initialize module <color={Color.yellow}>{name}</color>.", new PRLogSettings() { LevelDebug = 8 });
            RegisterService(initializeAction.Invoke());
            PRLog.WriteDebug(typeof(PRUnitySDK), $"Initialize complete <color={Color.yellow}>{name}</color>.");
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
