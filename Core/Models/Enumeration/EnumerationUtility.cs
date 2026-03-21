using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public static class EnumerationUtility
{
    public static IEnumerable<Enumeration> GetFromType(Type type, bool includeInherited = false)
    {
        var flags = BindingFlags.Public | BindingFlags.Static;

        if (!includeInherited)
            flags |= BindingFlags.DeclaredOnly;

        var fields = type.GetFields(flags);

        return fields
            .Where(f => f.FieldType == typeof(Enumeration))
            .Select(f => f.GetValue(null))
            .Cast<Enumeration>();
    }
}