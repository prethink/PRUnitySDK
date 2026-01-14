using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class VirtualAttributeAttribute : Attribute
{
    public string PropertyName { get; }
    public Type AttributeType { get; }

    public object[] Parameters { get; }

    public VirtualAttributeAttribute(string propertyName, Type attributeType, params object[] parameters)
    {
        PropertyName = propertyName;
        AttributeType = attributeType;
        Parameters = parameters;
    }
}
