using UnityEngine;

public class MaxValueRule : StatRuleBase
{
    public float MaxValue { get; protected set; }

    public MaxValueRule(Enumeration stat, float maxValue)
        : base(stat)
    {
        MaxValue = maxValue;
    }

    public MaxValueRule(Enumeration stat, float maxValue, int priority)
        : base(stat, priority)
    {
        MaxValue = maxValue;
    }

    public override float Apply(float value)
    {
        return Mathf.Min(value, MaxValue);
    }
}