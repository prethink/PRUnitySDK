using System;

public class StartGameSessionEventArgs : GameSessionEventArgsBase
{
    public override string EventId => "GameSession.StartSession";

    public DateTime StartSessionData { get; }

    public StartGameSessionEventArgs()
    {
        StartSessionData = PRUnitySDK.ServerTime.GetNow();
    }
}
