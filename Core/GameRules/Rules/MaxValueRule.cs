using System;
using UnityEngine;

/// <summary>
/// Верхняя граница значения характеристики.
/// </summary>
[Serializable]
public class MaxValueRule : StatRuleBase
{
    [SerializeField]
    [Tooltip("Больше этого значение не поднимется.")]
    private float maxValue;

    public MaxValueRule()
    {
    }

    public MaxValueRule(Enumeration stat, float maxValue, int priority = DefaultPriority)
        : base(stat, priority)
    {
        this.maxValue = maxValue;
    }

    public float MaxValue => maxValue;

    public override float Apply(float value)
    {
        return Mathf.Min(value, maxValue);
    }
}
