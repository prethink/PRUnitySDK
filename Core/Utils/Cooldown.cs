using System;

public abstract class CooldownBase
{
    private float lastTime;

    private object initiator;

    public bool TryExecute(float interval, Action action)
    {
        if (GetTime() >= lastTime + interval)
        {
            lastTime = GetTime();
            action?.Invoke();
            return true;
        }

        if(initiator != null)
            PRLog.WriteDebug(initiator, $"Cooldown for {initiator} is not ready yet. Time left: {lastTime + interval - GetTime()}", new PRLogSettings { LevelDebug = 10 });

        return false;
    }

    public T ExecuteWithResult<T>(float interval, Func<T> action, T fallback)
    {
        if (GetTime() >= lastTime + interval)
        {
            lastTime = GetTime();
            return action.Invoke();
        }

        if (initiator != null)
            PRLog.WriteDebug(initiator, $"Cooldown for {initiator} is not ready yet. Time left: {lastTime + interval - GetTime()}", new PRLogSettings { LevelDebug = 10});

        return fallback;
    }

    protected abstract float GetTime();

    public CooldownBase(float lastTime = 0)
    {
        this.lastTime = lastTime;
        initiator = null;
    }

    public CooldownBase(object initiator, float lastTime)
    {
        this.lastTime = lastTime;
        this.initiator = initiator;
    }
}

public class CooldownRealTime : CooldownBase
{
    protected override float GetTime()
    {
        return PRTime.Instance.RealTime;
    }
}

public class CooldownGameTime : CooldownBase
{
    protected override float GetTime()
    {
        return PRTime.Instance.GameTime;
    }
}