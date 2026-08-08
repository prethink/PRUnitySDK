using System;
using System.Collections.Generic;

/// <summary>
/// Устаревшая точка доступа к значениям Enumeration.
/// </summary>
[Obsolete("Use type.GetEnumerations(includeInherited) instead.")]
public static class EnumerationUtility
{
    public static IEnumerable<Enumeration> GetFromType(Type type, bool includeInherited = false)
    {
        return type.GetEnumerations(includeInherited);
    }
}
