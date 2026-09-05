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
    private static readonly Dictionary<Type, Enumeration> defaultCache = new();

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
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

        // Собираем всё вместе и сортируем один раз: EnumerationOrder должен работать
        // и между уровнями иерархии, иначе наследник не смог бы поставить своё значение
        // впереди базовых.
        var entries = new List<(Enumeration Value, int Order, int Depth, int Token)>();

        for (int depth = 0; depth < typeHierarchy.Count; depth++)
        {
            foreach (var field in typeHierarchy[depth].GetFields(flags))
            {
                if (!typeof(Enumeration).IsAssignableFrom(field.FieldType))
                    continue;

                if (field.GetValue(null) is not Enumeration enumeration)
                    continue;

                var order = field.GetCustomAttribute<EnumerationOrderAttribute>()?.Order ?? 0;
                entries.Add((enumeration, order, depth, field.MetadataToken));
            }
        }

        // Без атрибута порядок прежний: сначала базовый набор, внутри типа — порядок
        // объявления. MetadataToken растёт вместе с ним, а GetFields порядка не обещает.
        entries.Sort((left, right) =>
        {
            int order = left.Order.CompareTo(right.Order);
            if (order != 0)
                return order;

            int depth = left.Depth.CompareTo(right.Depth);
            return depth != 0 ? depth : left.Token.CompareTo(right.Token);
        });

        var result = new List<Enumeration>(entries.Count);
        foreach (var entry in entries)
            result.Add(entry.Value);

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

        return GetEnumerationProvider(type).GetOptions();
    }

    /// <summary>
    /// Экземпляр провайдера набора; создаётся один раз на тип.
    /// </summary>
    /// <remarks>
    /// Общий вход для всех, кому нужен провайдер: и списку значений, и значению
    /// по умолчанию, и дроверу в инспекторе. Без него каждый заводил бы свой кеш
    /// и свой <c>Activator.CreateInstance</c>.
    /// </remarks>
    /// <summary>
    /// Значение по умолчанию набора; считается один раз на тип.
    /// </summary>
    /// <remarks>
    /// Кеш живёт здесь, рядом с остальными: он сбрасывается вместе с ними при запуске,
    /// поэтому вход в Play Mode без перезагрузки домена не оставляет значение
    /// от прошлой сессии.
    /// </remarks>
    public static Enumeration GetEnumerationDefault(this Type type)
    {
        if (defaultCache.TryGetValue(type, out Enumeration value))
            return value;

        value = type.GetEnumerationProvider() is EnumerationProviderBase provider ? provider.Default : null;
        defaultCache.Add(type, value);

        return value;
    }

    public static IEnumerationProvider GetEnumerationProvider(this Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (providerCache.TryGetValue(type, out var provider))
            return provider;

        provider = Activator.CreateInstance(type) as IEnumerationProvider
            ?? throw new InvalidOperationException($"Cannot create enumeration provider '{type.FullName}'.");

        providerCache.Add(type, provider);
        return provider;
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
        defaultCache.Clear();
    }
}
