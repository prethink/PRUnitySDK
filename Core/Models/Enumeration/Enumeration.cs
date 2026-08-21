using System;
using System.Collections.Generic;
using UnityEngine;

public class Enumeration : IEnumeration, IEquatable<IEnumeration>
{
    private static readonly Dictionary<string, Enumeration> cache =
        new(StringComparer.Ordinal);

    private readonly EnumerationBridge bridge;

    public string Value => bridge.Value;

    public Enumeration(string value)
    {
        bridge = new EnumerationBridge(value);
    }

    public override string ToString()
    {
        return bridge.ToString();
    }

    public bool Equals(IEnumeration other)
    {
        return bridge.Equals(other);
    }

    public override bool Equals(object obj)
    {
        return obj is IEnumeration other && Equals(other);
    }

    public override int GetHashCode()
    {
        return bridge.GetHashCode();
    }

    public static bool operator ==(Enumeration left, Enumeration right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(Enumeration left, Enumeration right)
    {
        return !(left == right);
    }

    public static Enumeration GetOrCreate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
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
