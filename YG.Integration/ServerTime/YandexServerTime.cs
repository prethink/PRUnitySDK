using System;
using YG;

public class YandexServerTime : IServerTime
{
    private DateTime serverStartTime;
    private DateTime localStartTime;
    private bool initialized;

    public DateTime GetNow()
    {
        Initialize();

        TimeSpan elapsed = DateTime.Now - localStartTime;
        return serverStartTime + elapsed;
    }

    public void Initialize()
    {
        if (initialized)
            return;

        try
        {
            serverStartTime = PRUnitySDK.Settings.ProjectSettings.ReleaseType == ReleaseType.Release
                ? DateTimeOffset.FromUnixTimeMilliseconds(YG2.ServerTime()).DateTime
                : DateTime.Now;
        }
        catch(Exception ex)
        {
            serverStartTime = DateTime.Now;
            PRLog.WriteWarning(this, ex);
        }

        localStartTime = DateTime.Now;
        initialized = true;
    }
}
