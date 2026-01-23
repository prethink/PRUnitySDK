using System.Collections.Generic;
using System.Linq;

public abstract class TrackerBase<T>
{
    protected List<T> elements = new List<T>();

    public IReadOnlyList<T> Elements => elements.ToList();

    public abstract bool Register(T element);

    public abstract bool Unregister(T element);

    public virtual bool Contains(T element)
    {
        return elements.Contains(element);
    }
}
