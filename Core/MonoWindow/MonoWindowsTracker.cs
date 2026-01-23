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
}
