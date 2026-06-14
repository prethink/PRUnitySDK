public class BeforeSaveGameEventArgs : GameplayEventArgsBase
{
    public BeforeSaveGameEventArgs()
    {
        EventTime = PRUnitySDK.ServerTime.GetNow();
    }

    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "BeforeSaveGame");
    }
}