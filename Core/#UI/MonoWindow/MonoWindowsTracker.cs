using System.Linq;

/// <summary>
/// Хранит UI-окна с уникальными ключами и управляет их отображением.
/// </summary>
public class MonoWindowsTracker : TrackerBase<MonoWindowBase>
{
    /// <summary>
    /// Регистрирует ненулевое окно, если объект и его ключ ещё не заняты.
    /// </summary>
    public override bool Register(MonoWindowBase element)
    {
        if (element == null || elements.Contains(element) || elements.Any(x => x != null && x.Key == element.Key))
            return false;

        elements.Add(element);
        return true;
    }

    /// <summary>
    /// Удаляет ранее зарегистрированное окно.
    /// </summary>
    public override bool Unregister(MonoWindowBase element)
    {
        if (element == null)
            return false;

        return elements.Remove(element);
    }

    public void HideAllWindows()
    {
        foreach (var window in elements.ToList())
        {
            if (window != null)
                window.Hide();
        }
    }

    public void TryShowWindow(Enumeration key, MonoWindowArgs args)
    {
        HideAllWindows();

        var requiredWindow = elements.FirstOrDefault(x => x != null && x.Key == key);
        if (requiredWindow != null)
            requiredWindow.Show(args);
    }

    public void TryShowWindow(Enumeration key)
    {
        TryShowWindow(key, new MonoWindowsArgsEmpty());
    }

    public bool TryGetWindow<T>(Enumeration key, out T window) 
        where T : MonoWindowBase
    {
        window = null;

        var searchWindow = elements.FirstOrDefault(x => x != null && x.Key == key);
        if(searchWindow == null)
            return false;
        searchWindow.TryGetComponent<T>(out window);
        return window != null;
    }
}
