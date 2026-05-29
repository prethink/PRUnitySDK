using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class LocalizationDatabase 
{
    [field: SerializeField] public LangType DefaultLanguage { get; protected set; }
    [field: SerializeField] public List<LocalizationRow> Common { get; protected set; } = new List<LocalizationRow>();
    [field: SerializeField] public List<LocalizationRow> Project { get; protected set; } = new List<LocalizationRow>();
}

[Serializable]
public class LocalizationRow : ILocalizationProvider
{
    [field: SerializeField] public string LocalizationKey { get; protected set; }
    [SerializeField] protected List<string> localizations = new();

    public IReadOnlyList<string> LocalizationValues => localizations.ToList();

    public const string LangPropertyName = nameof(LocalizationRow.localizations);

    public LocalizationRow()
    {
        
    }

    public LocalizationRow(string key)
    {
        this.LocalizationKey = key;        
    }

    public LocalizationRow(string key, IEnumerable<string> localization)
    {
        this.LocalizationKey = key;
        localizations = localization.ToList();
    }
}