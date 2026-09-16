using System;
using UnityEngine;

/// <summary>
/// Действие, которому в инспекторе задают срок: дни, часы и минуты.
/// </summary>
public abstract class TimedActionBase : ActionBase
{
    [SerializeField, Min(0)] protected int days;
    [SerializeField, Range(0, 23)] protected int hours;
    [SerializeField, Range(0, 59)] protected int minutes;

    /// <summary>
    /// Суммарное заданное время.
    /// </summary>
    public TimeSpan AddTime => new(days, hours, minutes, 0);
}
