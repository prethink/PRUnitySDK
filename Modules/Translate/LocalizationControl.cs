using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocalizationControl : ILocalizationProvider
{
    [field: SerializeField] public string LocalizationKey { get; private set; }
    [field: SerializeField, SerializedDictionary("Lang", "Value")] public SerializedDictionary<LangType, string> localizationValues { get; private set; } = new();

    public IReadOnlyDictionary<LangType, string> LocalizationValues => localizationValues;

    public const string InternalLocalizationValuesPropertyName = nameof(localizationValues);

    public LocalizationControl()
    {
        
    }

    public LocalizationControl(string key, Dictionary<LangType, string> localization)
    {
        LocalizationKey = key;
        localizationValues = new SerializedDictionary<LangType, string>(localization);
    }
}
