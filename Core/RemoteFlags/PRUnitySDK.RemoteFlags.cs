public partial class PRUnitySDK
{
    #region Поля и свойства

    /// <summary>
    /// Приоритет.
    /// </summary>
    private const int PRIORITY_REMOTE_FLAGS = 11;

    /// <summary>
    /// Флаги проекта: значения по имени, которые площадка может поменять без пересборки.
    /// </summary>
    /// <remarks>
    /// <code>
    /// if (PRUnitySDK.RemoteFlags.TryGetInt("intType", out int intType)) { ... }
    /// float speed = PRUnitySDK.RemoteFlags.GetFloat("speed", 1f);
    /// </code>
    /// </remarks>
    public static IRemoteFlags RemoteFlags;

    #endregion

    #region Методы

    /// <summary>
    /// Инициализация модуля: реализация площадки, если она подключена, иначе флаги из настроек.
    /// </summary>
    [MethodHook(MethodHookStage.SDK, PRIORITY_REMOTE_FLAGS)]
    private static void InitializeRemoteFlags()
    {
        InitializeModuleSDK(nameof(IRemoteFlags), () =>
        {
            typeof(PRUnitySDK).TryOverrideStaticProperty(typeof(IRemoteFlags));

            InitializeDefault(nameof(IRemoteFlags), () => RemoteFlags, () => { RemoteFlags = new LocalRemoteFlags(); return RemoteFlags; });

            return RemoteFlags;
        });
    }

    #endregion
}
