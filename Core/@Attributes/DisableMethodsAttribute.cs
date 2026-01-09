using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class DisableMethodsAttribute : Attribute
{
    public List<string> MethodsToDisable { get; } = new();

    public DisableMethodsAttribute(params string[] methods)
    {
        MethodsToDisable.AddRange(methods);
    }
}