using System;

public class EndSessionEventArgs : GameSessionEventArgsBase
{
    public override string EventId => "GameSession.EndSession";

    public DateTime EndSessionData { get; }

    public EndSessionEventArgs()
    {
        EndSessionData = PRUnitySDK.ServerTime.GetNow();
    }
}
