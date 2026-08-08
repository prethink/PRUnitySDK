using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Методы получения значений из классов-провайдеров Enumeration.
/// </summary>
public static class EnumerationExtensions
{
    private static readonly Dictionary<(Type, bool), Enumeration[]> enumerationCache = new();
    private static readonly Dictionary<Type, IEnumerationProvider> providerCache = new();

    /// <summary>
    /// Возвращает объявленные в типе публичные статические поля Enumeration.
    /// </summary>
    public static IReadOnlyList<Enumeration> GetEnumerations(this Type type, bool includeInherited = false)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        var key = (type, includeInherited);
        if (enumerationCache.TryGetValue(key, out var cached))
            return cached;

        var typeHierarchy = new List<Type>();
        for (var currentType = type; currentType != null; currentType = currentType.BaseType)
        {
            typeHierarchy.Add(currentType);
            if (!includeInherited)
                break;
        }

        typeHierarchy.Reverse();
        var result = new List<Enumeration>();
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (var currentType in typeHierarchy)
        {
            var fields = currentType.GetFields(flags);
            Array.Sort(fields, (left, right) => left.MetadataToken.CompareTo(right.MetadataToken));

            foreach (var field in fields)
            {
                if (!typeof(Enumeration).IsAssignableFrom(field.FieldType))
                    continue;

                if (field.GetValue(null) is Enumeration enumeration)
                    result.Add(enumeration);
            }
        }

        var values = result.ToArray();
        enumerationCache.Add(key, values);
        return values;
    }

    /// <summary>
    /// Получает Enumeration через IEnumerationProvider или напрямую из статических полей типа.
    /// </summary>
    public static IEnumerable<Enumeration> GetEnumerationsSmart(this Type type, bool includeInherited = false)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (!typeof(IEnumerationProvider).IsAssignableFrom(type))
            return type.GetEnumerations(includeInherited);

        if (!providerCache.TryGetValue(type, out var provider))
        {
            provider = Activator.CreateInstance(type) as IEnumerationProvider
                ?? throw new InvalidOperationException($"Cannot create enumeration provider '{type.FullName}'.");
            providerCache.Add(type, provider);
        }

        return provider.GetOptions();
    }

    /// <summary>
    /// Возвращает строковые значения всех найденных Enumeration.
    /// </summary>
    public static IEnumerable<string> GetEnumerationValues(this Type type, bool includeInherited = false)
    {
        foreach (var enumeration in type.GetEnumerations(includeInherited))
            yield return enumeration.Value;
    }

    /// <summary>
    /// Проверяет наличие указанного строкового значения.
    /// </summary>
    public static bool ContainsEnumeration(this Type type, string value, bool includeInherited = false)
    {
        foreach (var enumeration in type.GetEnumerations(includeInherited))
        {
            if (StringComparer.Ordinal.Equals(enumeration.Value, value))
                return true;
        }

        return false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearCacheOnLoad()
    {
        enumerationCache.Clear();
        providerCache.Clear();
    }
}
