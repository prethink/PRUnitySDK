using System.Collections.Generic;

public interface IEnumerationProvider
{
    IEnumerable<Enumeration> GetOptions();
}
