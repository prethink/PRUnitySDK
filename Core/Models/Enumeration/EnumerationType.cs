using System;

public sealed class EnumerationType<T> : IEnumeration, IEquatable<IEnumeration>
{
    private readonly EnumerationBridge bridge;

    public string Value => bridge.Value;

    public Type ValueType => typeof(T);

    public EnumerationType(string value)
    {
        bridge = new EnumerationBridge(value);
    }

    public override string ToString() => bridge.ToString();

    public bool Equals(IEnumeration other) =>
        bridge.Equals(other);

    public override bool Equals(object obj) =>
        bridge.Equals(obj);

    public override int GetHashCode() =>
        bridge.GetHashCode();
}