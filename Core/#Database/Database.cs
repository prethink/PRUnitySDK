using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Database<T> 
{
    [SerializeField] private List<T> data = new();

    public IEnumerable<T> Data => data.ToList();
}
