using System;
using System.Collections.Generic;

public class LocalizationConnector : ILocalizationProvider, ILocalizationPostfix
{
    public LocalizationConnector(string key) : this(key, string.Empty, Array.Empty<string>())
    {
       
    }

    public LocalizationConnector(string key, string prefixValue) : this(key, prefixValue, Array.Empty<string>())
    {

    }

    public LocalizationConnector(string key, string prefix, params string[] args)
    {
        LocalizationKey = key;
        Args = args;
        this.prefix = prefix;
    }

    public string prefix = string.Empty;

    public string[] Args;

    public string LocalizationKey { get; }

    public IReadOnlyDictionary<LangType, string> LocalizationValues => L.GetDictionary(LocalizationKey);

    public string LocalizationPostfix => prefix;
}
