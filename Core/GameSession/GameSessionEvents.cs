public static class GameSessionEvents 
{
    public static void RaiseStartSession() => EventBus.RaiseEvent<IGameSessionEvent>(invoke => invoke.Track(new StartGameSessionEventArgs()));
    public static void RaiseEndSession() => EventBus.RaiseEvent<IGameSessionEvent>(invoke => invoke.Track(new EndSessionEventArgs()));

    public static void RaiseStartRound(StartRoundArgsBase args) => EventBus.RaiseEvent<IGameSessionEvent>(invoke => invoke.Track(args));
    public static void RaiseEndRound(EndRoundArgsBase args) => EventBus.RaiseEvent<IGameSessionEvent>(invoke => invoke.Track(args));
}
