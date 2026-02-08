using System.Collections.Generic;

public interface IEnumerationOptionsProvider
{
    IEnumerable<Enumeration> GetOptions();
}