using SABI;
using System.Linq;

/// <summary>
/// Хранит UI-уведомители с уникальными ключами и предоставляет безопасный поиск.
/// </summary>
public class NotifierTracker : TrackerBase<NotifierBase>
{
    /// <summary>
    /// Регистрирует ненулевой notifier, если объект и его ключ ещё не заняты.
    /// </summary>
    public override bool Register(NotifierBase element)
    {
        if (element == null || elements.Contains(element) || elements.Any(x => x != null && x.Key == element.Key))
            return false;

        elements.Add(element);
        return true;
    }

    /// <summary>
    /// Удаляет ранее зарегистрированный notifier.
    /// </summary>
    public override bool Unregister(NotifierBase element)
    {
        if (element == null)
            return false;

        return elements.Remove(element);
    }

    public bool TryGetNotifier<T>(Enumeration key, out T notifier)
        where T : NotifierBase
    {
        notifier = null;

        var searchNotifier = elements.FirstOrDefault(x => x != null && x.Key == key);
        if (searchNotifier == null)
            return false;

        searchNotifier.TryGetComponent<T>(out notifier);
        return notifier != null;
    }

    public T GetNotifier<T>(Enumeration key)
        where T : NotifierBase
    {
        return TryGetNotifier(key, out T notifier) ? notifier : null;
    }

    public T GetNotifier<T>()
        where T : NotifierBase
    {
        foreach (var element in elements)
        {
            if (element != null && element.TryGetComponent<T>(out var component))
                return component;
        }

        return null;
    }
}

public class NotifierService : SingletonProviderBase<NotifierTracker>
{

}
