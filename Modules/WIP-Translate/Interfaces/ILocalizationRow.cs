using System.Collections.Generic;

public interface ILocalizationRow 
{
    string Key { get; }
    List<string> LangData { get;}
}
