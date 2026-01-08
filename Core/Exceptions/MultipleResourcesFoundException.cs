using System;

public class MultipleResourcesFoundException : InvalidOperationException
{
    public Type Type { get; }

    public MultipleResourcesFoundException(Type type)
        : base($"Multiple {type.Name} found in Resources. Only one instance is allowed.") { this.Type = type; }
}
