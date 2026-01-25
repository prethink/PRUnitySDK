using System;

public class EndSessionEventArgs : GameSessionEventArgsBase
{
    public override string EventId => path.Value;

    private readonly CategoryPath path = new CategoryPath("GameSession", "EndSession");

    public DateTime EndSessionData { get; }

    public EndSessionEventArgs()
    {
        EndSessionData = PRUnitySDK.ServerTime.GetNow();
    }
}
