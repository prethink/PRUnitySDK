using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static UnityEngine.ProBuilder.AutoUnwrapSettings;

public static class EnumerationExtensions
{
    private static readonly Dictionary<(Type, bool), IEnumerable<Enumeration>> cache = new();

    /// <summary>
    /// Получить все Enumeration из типа.
    /// </summary>
    public static IEnumerable<Enumeration> GetEnumerations(this Type type, bool includeInherited = false)
    {
        var key = (type, includeInherited);

        if (cache.TryGetValue(key, out var cached))
            return cached;

        var flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

        var result = new List<List<Enumeration>>();

        var currentType = type;

        while (currentType != null)
        {
            var fields = currentType
                .GetFields(flags)
                .Where(f => f.FieldType == typeof(Enumeration))
                .Select(f => f.GetValue(null))
                .Cast<Enumeration>();

            fields.Reverse();
            result.Add(new List<Enumeration>(fields));

            if (!includeInherited)
                break;

            currentType = currentType.BaseType;
        }
        result.Reverse();
        var final = result.SelectMany(x => x);

        cache[key] = final;
        return final;
    }

    /// <summary>
    /// Получить все Enumeration с учётом IEnumerationProvider (если реализован).
    /// </summary>
    public static IEnumerable<Enumeration> GetEnumerationsSmart(this Type type, bool includeInherited = false)
    {
        if (typeof(IEnumerationProvider).IsAssignableFrom(type))
        {
            var provider = Activator.CreateInstance(type) as IEnumerationProvider;
            return provider.GetOptions();
        }

        return type.GetEnumerations(includeInherited);
    }

    /// <summary>
    /// Получить значения Enumeration как строки.
    /// </summary>
    public static IEnumerable<string> GetEnumerationValues(this Type type, bool includeInherited = false)
    {
        return type.GetEnumerations(includeInherited)
                   .Select(e => e.Value);
    }

    /// <summary>
    /// Проверка: содержит ли тип конкретное значение.
    /// </summary>
    public static bool ContainsEnumeration(this Type type, string value, bool includeInherited = false)
    {
        return type.GetEnumerations(includeInherited)
                   .Any(e => e.Value == value);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearCacheOnLoad()
    {
        cache.Clear();
    }
}