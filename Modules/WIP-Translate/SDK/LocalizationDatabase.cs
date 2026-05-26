using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocalizationDatabase 
{
    [field: SerializeField] public LangType DefaultLanguage { get; protected set; }
    [field: SerializeField] public List<LocalizationRow> Common { get; protected set; } = new List<LocalizationRow>();
    [field: SerializeField] public List<LocalizationRow> Project { get; protected set; } = new List<LocalizationRow>();
}

[Serializable]
public class LocalizationRow : ILocalizationRow
{
    [field: SerializeField] public string Key { get; protected set; }
    [field: SerializeField] public List<string> LangData { get; protected set; } = new();

    public string GetTranslate()
    {
        return GetTranslate(this);
    }

    public static string GetTranslate(ILocalizationRow localization)
    {
        return GetTranslate(localization, PRUnitySDK.CurrentLang);
    }

    public string GetTranslate(string langKey)
    {
        return GetTranslate(this, langKey);
    }

    public static string GetTranslate(ILocalizationRow localization, string langKey)
    {
        return GetTranslate(localization, LocalizationUtils.GetLanguageEnum(langKey));
    }

    public string GetTranslate(LangType lang)
    {
        return GetTranslate(this, lang);
    }

    public static string GetTranslate(ILocalizationRow localization, LangType lang)
    {
        var index = LocalizationUtils.GetLanguageIndex(lang);
        return GetResultTranslate(localization, index);
    }

    public static string GetResultTranslate(ILocalizationRow localization, int index)
    {
        if (localization == null)
            return "EMPTY_LOCALIZATION";

        if (index < 0 || index >= localization.LangData.Count)
            return localization.Key;

        var value = localization.LangData[index];

        if (string.IsNullOrEmpty(value))
            return localization.Key;

        return value;
    }
}