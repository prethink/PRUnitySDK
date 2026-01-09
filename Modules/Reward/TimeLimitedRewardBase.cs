using System;

public abstract class TimeLimitedRewardBase 
{
    protected virtual bool IsActive(string name, out DateTime endTime)
    {
        endTime = DateTime.MinValue;
        if (PRUnitySDK.Managers.ProjectPropertiesManager.TryGetDateTime(GetName(name), out var data))
        {
            if (PRUnitySDK.ServerTime.GetNow() < data)
            {
                endTime = data;
                return true;
            }

            return false;
        }

        return false;
    }

    protected virtual void AddTime(string name, TimeSpan addTime)
    {
        if (IsActive(name, out var endTime))
            PRUnitySDK.Managers.ProjectPropertiesManager.SetDateTime(GetName(name), endTime.Add(addTime));
        else
            PRUnitySDK.Managers.ProjectPropertiesManager.SetDateTime(GetName(name), PRUnitySDK.ServerTime.GetNow().Add(addTime));
    }

    public virtual string GetName(string name)
    {
        return name;
    }
}
