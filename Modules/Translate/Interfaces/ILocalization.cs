using System.Collections.Generic;

public interface ILocalization 
{
    string Key { get; }
    IEnumerable<string> LangData { get;}
}
