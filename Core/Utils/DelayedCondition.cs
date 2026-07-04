public abstract class DelayedConditionBase
{
    private float triggerTime;
    private bool isTriggered;

    public bool Evaluate(bool condition, float delay)
    {
        if (!condition)
        {
            Reset();
            return false;
        }

        if (!isTriggered)
        {
            isTriggered = true;
            triggerTime = GetTime();
        }

        return GetTime() >= triggerTime + delay;
    }

    public abstract float GetTime();

    public void Reset()
    {
        isTriggered = false;
        triggerTime = 0f;
    }
}

public class DelayedConditionRealTime : DelayedConditionBase
{
    public override float GetTime()
    {
        return PRTime.Instance.RealTime;
    }
}

public class DelayedConditionGameTime : DelayedConditionBase
{
    public override float GetTime()
    {
        return PRTime.Instance.GameTime;
    }
}
