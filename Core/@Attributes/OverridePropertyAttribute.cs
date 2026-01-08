using System;

[AttributeUsage(AttributeTargets.Method)]
public class OverridePropertyAttribute : OverrideBaseAttribute
{
    public OverridePropertyAttribute(Type type, int order = 0) : base(type, order) { }
}
