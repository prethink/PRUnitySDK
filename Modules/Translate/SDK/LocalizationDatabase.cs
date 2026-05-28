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
public class LocalizationRow : ILocalization
{
    [field: SerializeField] public string Key { get; protected set; }
    [SerializeField] protected List<string> langData = new();

    public IEnumerable<string> LangData => langData.ToList();

    public const string LangPropertyName = nameof(LocalizationRow.langData);

    public LocalizationRow()
    {
        
    }

    public LocalizationRow(string key)
    {
        this.Key = key;        
    }

    public LocalizationRow(Dictionary<LangType, string> dict, string key = "")
    {
        Key = key;

        var languages = (LangType[])Enum.GetValues(typeof(LangType));

        langData = new List<string>(new string[languages.Length]);

        foreach (var pair in dict)
        {
            int index = LocalizationUtils.GetLanguageIndex(pair.Key);

            if (index >= 0 && index < langData.Count)
                langData[index] = pair.Value;
        }
    }
}