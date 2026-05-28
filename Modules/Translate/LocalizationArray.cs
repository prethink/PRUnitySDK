using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class LocalizationArray
{
    [SerializeField] private List<string> values;

    public IEnumerable<string> Values => values.ToList();
}