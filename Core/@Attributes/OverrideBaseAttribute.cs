using System;

public abstract class OverrideBaseAttribute : Attribute
{
    public Type OverrideType { get; protected set; }

    public int Order { get; protected set; }

    public OverrideBaseAttribute(Type type, int order = 0)
    {
        OverrideType = type;
        Order = order;
    }
}
