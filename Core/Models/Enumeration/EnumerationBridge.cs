using System;

internal sealed class EnumerationBridge
{
    public string Value { get; }

    public EnumerationBridge(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(
                "Enumeration value cannot be null, empty, or whitespace.",
                nameof(value));

        Value = value;
    }

    public override string ToString() => Value;

    public bool Equals(IEnumeration other)
    {
        return other != null &&
               StringComparer.Ordinal.Equals(Value, other.Value);
    }

    public override bool Equals(object obj)
    {
        return obj is IEnumeration other && Equals(other);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Value);
    }
}