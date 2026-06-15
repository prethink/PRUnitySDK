using System;
using System.Collections.Generic;

public class LocalizationConnector : ILocalizationProvider, ILocalizationAffix
{
    public LocalizationConnector(string key) : this(key, null, Array.Empty<string>())
    {
       
    }

    public LocalizationConnector(string key, LocalizationAffixOptions options) : this(key, options, Array.Empty<string>())
    {

    }

    public LocalizationConnector(string key, LocalizationAffixOptions options, params string[] args)
    {
        LocalizationKey = key;
        Args = args;
        Options = options;
    }

    public string prefix = string.Empty;

    public string[] Args;

    public string LocalizationKey { get; }

    public IReadOnlyDictionary<LangType, string> LocalizationValues => L.GetDictionary(LocalizationKey);

    public LocalizationAffixOptions Options { get; }
}


