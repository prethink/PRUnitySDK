using System.Linq;

public class MonoWindowsTracker : TrackerBase<MonoWindowBase>
{
    public override bool Register(MonoWindowBase element)
    {
        elements.Add(element);
        return true;
    }

    public override bool Unregister(MonoWindowBase element)
    {
        return elements.Remove(element);
    }

    public void HideAllWindows()
    {
        foreach (var window in elements)
            window.Hide();
    }

    public void TryShowWindow(Enumeration key, MonoWindowArgs args)
    {
        HideAllWindows();

        var requiredWindow = elements.FirstOrDefault(x => x.Key == key);
        if (requiredWindow != null)
            requiredWindow.Show(args);
    }

    public void TryShowWindow(Enumeration key)
    {
        TryShowWindow(key, new MonoWindowsArgsEmpty());
    }
}
