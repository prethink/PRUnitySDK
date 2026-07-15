using System.Collections.Generic;
using UnityEngine;

public class ColoredLocalizationDecorator : ILocalizationProvider
{
    public Color Color { get; private set; }
    public ILocalizationProvider localizationProvider { get; private set; }

    public string LocalizationKey => localizationProvider.LocalizationKey;

    public IReadOnlyDictionary<LangType, string> LocalizationValues { get; }

    public ColoredLocalizationDecorator(ILocalizationProvider provider, Color color)
    {
        this.Color = color;    
        this.localizationProvider = provider;

        var overrideDictionary = new Dictionary<LangType, string>();
        foreach (var providerDict in this.localizationProvider.LocalizationValues)
            overrideDictionary.Add(providerDict.Key, TextUtils.GetColoredText(color, providerDict.Value));

        LocalizationValues = overrideDictionary;
    }
}
