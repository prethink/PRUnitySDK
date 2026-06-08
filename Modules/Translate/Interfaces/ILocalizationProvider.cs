using System.Collections.Generic;

public interface ILocalizationProvider 
{
    string LocalizationKey { get; }
    IReadOnlyDictionary<LangType, string> LocalizationValues { get;}
}