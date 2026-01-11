using System;

public class SaveGameEventArgs : GameplayEventArgsBase
{
    public override string EventId => nameof(SaveGameEventArgs);

    public override DateTime EventTime { get; }

    public SaveGameEventArgs()
    {
        EventTime = PRUnitySDK.ServerTime.GetNow();
    }
}
