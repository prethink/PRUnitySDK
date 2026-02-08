using System;
using UnityEngine;

[Serializable]
public class EnumerationReference
{
    [SerializeField]
    private string value;

    public string Value => value;

    public Enumeration ToEnumeration() => new Enumeration(value);
}
