using System;

public class ResourceNotFoundException : InvalidOperationException
{
    public Type Type { get; }
    public ResourceNotFoundException(Type type)
        : base($"{type.Name} not found in Resources.") { this.Type = type; }
}
