using System.Collections.Generic;

public interface ILocalizationProvider 
{
    string LocalizationKey { get; }
    IReadOnlyList<string> LocalizationValues { get;}
}