using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Строковый идентификатор, дополнительно указывающий тип связанного значения.
/// </summary>
public class Enumeration<T> : Enumeration
{
    /// <summary>
    /// Тип значения, связанного с идентификатором.
    /// </summary>
    public Type ValueType => typeof(T);

    public Enumeration(string value) : base(value)
    {

    }
}

public class Enumeration : IEquatable<Enumeration>
{
    private static readonly Dictionary<string, Enumeration> cache = new(StringComparer.Ordinal);

    /// <summary>
    /// Стабильное строковое значение идентификатора.
    /// </summary>
    public string Value { get; }

    public Enumeration(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Enumeration value cannot be null, empty, or whitespace.", nameof(value));

        Value = value;
    }

    public override string ToString() => Value;

    public bool Equals(Enumeration other) =>
        other != null && StringComparer.Ordinal.Equals(Value, other.Value);

    public override bool Equals(object obj) =>
        obj is Enumeration other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearCacheOnLoad()
    {
        cache.Clear();
    }
}
