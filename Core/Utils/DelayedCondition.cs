public struct DelayedCondition
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
            triggerTime = PRTime.Instance.Time;
        }

        return PRTime.Instance.Time >= triggerTime + delay;
    }

    public void Reset()
    {
        isTriggered = false;
        triggerTime = 0f;
    }
}
