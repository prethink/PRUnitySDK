using System;

public class SaveGameEventArgs : GameplayEventArgsBase
{
    public override DateTime EventTime { get; }

    public SaveGameEventArgs()
    {
        EventTime = PRUnitySDK.ServerTime.GetNow();
    }

    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "SaveGame");
    }
}
