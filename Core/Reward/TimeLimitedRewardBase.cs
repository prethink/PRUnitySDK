using System;

public abstract class TimeLimitedRewardBase 
{
    public abstract string Name { get; }

    protected virtual bool IsActive(out DateTime endTime)
    {
        endTime = DateTime.MinValue;
        if (!PRUnitySDK.Managers.ProjectPropertiesManager.TryGetDateTime(Name, out var data))
            return false;

        if (PRUnitySDK.ServerTime.GetNow() > data)
            return false;

        endTime = data;
        return true;
    }

    protected virtual void AddTimeInternal(TimeSpan addTime)
    {
        if (IsActive(out var endTime))
            PRUnitySDK.Managers.ProjectPropertiesManager.SetDateTime(Name, endTime.Add(addTime));
        else
            PRUnitySDK.Managers.ProjectPropertiesManager.SetDateTime(Name, PRUnitySDK.ServerTime.GetNow().Add(addTime));
    }
}
