using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

[Serializable]
public class LocalizationDatabase 
{
    [SerializeField] private List<KeyValueWrapper<LangType, TMP_FontAsset>> languageFonts = new();

    public IReadOnlyCollection<KeyValueWrapper<LangType, TMP_FontAsset>> LanguageFonts => languageFonts.ToList();

    [field: SerializeField] public LangType DefaultLanguage { get; protected set; }
}
