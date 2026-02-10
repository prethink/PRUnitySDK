using System;

public struct Cooldown
{
    private float lastTime;

    private object initiator;

    public bool TryExecute(float interval, Action action)
    {
        if (PRTime.Instance.Time >= lastTime + interval)
        {
            lastTime = PRTime.Instance.Time;
            action?.Invoke();
            return true;
        }

        if(initiator != null)
            PRLog.WriteDebug(initiator, $"Cooldown for {initiator} is not ready yet. Time left: {lastTime + interval - PRTime.Instance.Time}", new PRLogSettings { LevelDebug = 10 });

        return false;
    }

    public T ExecuteWithResult<T>(float interval, Func<T> action, T fallback)
    {
        if (PRTime.Instance.Time >= lastTime + interval)
        {
            lastTime = PRTime.Instance.Time;
            return action.Invoke();
        }

        if (initiator != null)
            PRLog.WriteDebug(initiator, $"Cooldown for {initiator} is not ready yet. Time left: {lastTime + interval - PRTime.Instance.Time}", new PRLogSettings { LevelDebug = 10});

        return fallback;
    }

    public Cooldown(float lastTime = 0)
    {
        this.lastTime = lastTime;
        initiator = null;
    }

    public Cooldown(object initiator, float lastTime)
    {
        this.lastTime = lastTime;
        this.initiator = initiator;
    }
}
