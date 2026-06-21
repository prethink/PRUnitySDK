public abstract class StatRuleBase 
{
    protected StatRuleBase(Enumeration stat, int priority = 100)
    {
        Stat = stat;
        Priority = priority;
    }

    public Enumeration Stat { get; protected set; }
    public int Priority { get; protected set; }
    public abstract float Apply(float value);
}
