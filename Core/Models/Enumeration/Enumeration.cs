using System;

public readonly struct Enumeration : IEquatable<Enumeration>
{
    public string Value { get; }

    public Enumeration(string value)
    {
        Value = value;
    }

    public override string ToString() => Value;

    public bool Equals(Enumeration other) => Value == other.Value;

    public override bool Equals(object obj) =>
        obj is Enumeration other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(Enumeration a, Enumeration b) => a.Equals(b);
    public static bool operator !=(Enumeration a, Enumeration b) => !a.Equals(b);
}
