using System;

public class EndSessionEventArgs : GameSessionEventArgsBase
{
    public DateTime EndSessionData { get; }

    public EndSessionEventArgs()
    {
        EndSessionData = PRUnitySDK.ServerTime.GetNow();
    }

    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "EndSession");
    }
}
