using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnumerationReference<T> : EnumerationReference
    where T : IEnumerationProvider, new()
{
    public static IEnumerable<Enumeration> GetOptions()
    {
        return typeof(T).GetEnumerationsSmart(true);
    }
}

[Serializable]
public class EnumerationReference
{
    [SerializeField]
    private string value;

    public string Value => value;

    public Enumeration ToEnumeration() => new Enumeration(value);
}
