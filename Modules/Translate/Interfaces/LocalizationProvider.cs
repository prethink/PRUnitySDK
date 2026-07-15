using System.Collections.Generic;

public class LocalizationProvider : ILocalizationProvider
{
    public string LocalizationKey { get; private set; }

    public IReadOnlyDictionary<LangType, string> LocalizationValues { get; private set; }

    public LocalizationProvider(IReadOnlyDictionary<LangType, string> localization)
    {
        this.LocalizationValues = localization;
    }

    public LocalizationProvider(string key, IReadOnlyDictionary<LangType, string> localization)
    {
        this.LocalizationKey = key;
        this.LocalizationValues = localization;
    }

    public LocalizationProvider(string text)
    {
        this.LocalizationValues = new Dictionary<LangType, string>() 
        { 
            { LangType.Russian, text }, 
            { LangType.English, text }, 
            { LangType.Turkey, text }, 
        };
    }
}
