using System;
using UnityEngine;

/// <summary>
/// Нижняя граница значения характеристики.
/// </summary>
[Serializable]
public class MinValueRule : StatRuleBase
{
    [SerializeField]
    [Tooltip("Меньше этого значение не опустится.")]
    private float minValue;

    public MinValueRule()
    {
    }

    public MinValueRule(Enumeration stat, float minValue, int priority = DefaultPriority)
        : base(stat, priority)
    {
        this.minValue = minValue;
    }

    public float MinValue => minValue;

    public override float Apply(float value)
    {
        return Mathf.Max(value, minValue);
    }
}
