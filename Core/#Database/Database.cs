using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public abstract class Database<T> 
{
    [SerializeField] private List<T> data = new();

    public IReadOnlyList<T> Data => data.ToList();

    public abstract string DataBaseKey { get; }
}
