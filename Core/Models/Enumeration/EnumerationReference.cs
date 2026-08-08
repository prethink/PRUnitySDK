using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class EnumerationReference<T> : EnumerationReference
    where T : IEnumerationProvider, new()
{
    public static IEnumerable<Enumeration> GetOptions()
    {
        return typeof(T).GetEnumerationsSmart(true);
    }

    public void SetDefaultIfNull(Enumeration enumeration)
    {
        if(string.IsNullOrEmpty(value))
            Set(enumeration);
    }

    public void Set(Enumeration enumeration)
    {
        if (enumeration == null)
            throw new ArgumentNullException(nameof(enumeration));

        var available = GetOptions();
        if (!available.Any(e => e == enumeration))
            throw new ArgumentException($"Enumeration '{enumeration}' не существует в {typeof(T).Name}.", nameof(enumeration));

        value = enumeration.Value;
    }
}

[Serializable]
public class EnumerationReference
{
    [SerializeField]
    protected string value;

    public string Value => value;

    public Enumeration ToEnumeration() => Enumeration.GetOrCreate(value);

    public const string ProtectedStringValueName = nameof(value);
}
