using System;

public class StartGameSessionEventArgs : GameSessionEventArgsBase
{
    public DateTime StartSessionData { get; }

    public StartGameSessionEventArgs()
    {
        StartSessionData = PRUnitySDK.ServerTime.GetNow();
    }

    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "StartSession");
    }
}
