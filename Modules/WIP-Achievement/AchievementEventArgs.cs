using System;

public class AchievementEventArgs : GameplayEventArgsBase
{
    public string Trigger { get; }

    public AchievementEventArgs(string triggerName)
    {
        EventTime = PRUnitySDK.ServerTime.GetNow();
        Trigger = triggerName;
    }

    public override CategoryPath GetEventId()
    {
        return new CategoryPath(base.GetEventId(), "Achievement.Trigger");
    }
}
