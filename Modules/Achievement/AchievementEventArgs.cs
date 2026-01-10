using System;

public class AchievementEventArgs : GameplayEventArgsBase
{
    public override string EventId => "TriggerAchievement";
    public override DateTime EventTime { get; }

    public string Trigger { get; }

    public AchievementEventArgs(string triggerName)
    {
        EventTime = PRUnitySDK.ServerTime.GetNow();
        Trigger = triggerName;
    }
}
