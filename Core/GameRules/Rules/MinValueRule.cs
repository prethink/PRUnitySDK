using UnityEngine;

public class MinValueRule : StatRuleBase
{
    public MinValueRule(Enumeration stat, float minValue)
        : base(stat)
    {
        MinValue = minValue;
    }

    public MinValueRule(Enumeration stat, float minValue, int priority)
        : base(stat, priority)
    {
        MinValue = minValue;
    }

    public float MinValue { get; protected set; }

    public override float Apply(float value)
    {
        return Mathf.Max(value, MinValue);
    }
}
