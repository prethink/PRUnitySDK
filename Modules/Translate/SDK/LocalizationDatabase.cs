using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocalizationDatabase 
{
    [field: SerializeField] public LangType DefaultLanguage { get; protected set; }
    [field: SerializeField] public List<LocalizationControl> Common { get; protected set; } = new List<LocalizationControl>();
    [field: SerializeField] public List<LocalizationControl> Project { get; protected set; } = new List<LocalizationControl>();
}