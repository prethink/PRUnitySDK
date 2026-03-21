using System.Collections.Generic;

public abstract class EnumerationProviderBase : IEnumerationProvider
{
    public abstract bool IncludeInherited { get; }

    public virtual IEnumerable<Enumeration> GetOptions()
    {
        return GetType().GetEnumerations(this.IncludeInherited);
    }
}
