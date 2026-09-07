using PRGameSessions;

public partial class PRUnitySDK
{
    /// <summary>
    /// Модуль выключен до явного SetEnabled(true) или подключения SessionSceneHost.
    /// </summary>
    public static IGameSessionsService GameSessions { get; private set; }

    [MethodHook(MethodHookStage.SDK, 80)]
    private static void InitializeGameSessions()
    {
        InitializeModuleSDK(nameof(IGameSessionsService), () =>
        {
            InitializeDefault(nameof(GameSessions), () => GameSessions,
                () => GameSessions = new GameSessionsController());
            return GameSessions;
        });
    }
}
