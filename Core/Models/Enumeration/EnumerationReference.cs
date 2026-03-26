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
        {
            var available = GetOptions();
            if (!available.Any(e => e == enumeration))
                throw new Exception($"Enumeration '{enumeration}' не существует в {typeof(T).Name}");

            value = enumeration.Value;
        }
    }
}

[Serializable]
public class EnumerationReference
{
    [SerializeField]
    protected string value;

    public string Value => value;

    public Enumeration ToEnumeration() => new Enumeration(value);
}
