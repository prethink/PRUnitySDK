using SABI;
using System.Linq;

public class NotifierTracker : TrackerBase<NotifierBase>
{
    public override bool Register(NotifierBase element)
    {
        elements.Add(element);
        return true;
    }

    public override bool Unregister(NotifierBase element)
    {
        return elements.Remove(element);
    }

    public bool TryGetNotifier<T>(Enumeration key, out T notifier)
        where T : NotifierBase
    {
        notifier = null;

        var searchNotifier = elements.FirstOrDefault(x => x.Key == key);
        if (searchNotifier == null)
            return false;

        searchNotifier.TryGetComponent<T>(out notifier);
        return notifier != null;
    }

    public T GetNotifier<T>(Enumeration key)
        where T : NotifierBase
    {
        return elements.FirstOrDefault(x => x.Key == key).GetComponent<T>();
    }

    public T GetNotifier<T>()
        where T : NotifierBase
    {
        foreach (var element in elements)
        {
            if (element.TryGetComponent<T>(out var component))
                return component;
        }

        return null;
    }
}

public class NotifierService : SingletonProviderBase<NotifierTracker>
{

}
