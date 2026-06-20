using System;
using System.Collections.Generic;

public class Enumeration<T> : Enumeration
{
    public Type ValueType => typeof(T);

    public Enumeration(string value) : base(value)
    {

    }
}

public class Enumeration : IEquatable<Enumeration>
{
    private static readonly Dictionary<string, Enumeration> cache = new();

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

    public static bool operator ==(Enumeration a, Enumeration b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a is null || b is null)
            return false;

        return a.Equals(b);
    }
    public static bool operator !=(Enumeration a, Enumeration b) => !(a == b);

    public static Enumeration GetOrCreate(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        if (cache.TryGetValue(value, out var existing))
            return existing;

        var created = new Enumeration(value);
        cache[value] = created;
        return created;
    }
}
