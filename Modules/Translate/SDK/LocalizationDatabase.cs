using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocalizationDatabase 
{
    [field: SerializeField] public LangType DefaultLanguage { get; protected set; }
    [field: SerializeField, HideInInspector] public List<LocalizationControl> Common { get; protected set; } = new List<LocalizationControl>();
    [field: SerializeField, HideInInspector] public List<LocalizationControl> Project { get; protected set; } = new List<LocalizationControl>();
}