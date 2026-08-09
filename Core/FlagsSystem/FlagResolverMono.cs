/// <summary>
/// Component owner and convenience wrapper for a local <see cref="FlagResolver"/>.
/// </summary>
public class FlagResolverMono : MonoBehaviourLinkBase<FlagResolver>
{
    public bool Get(Enumeration key, bool defaultValue = true) => Link.Get(key, defaultValue);

    public FlagDecision Resolve(Enumeration key) => Link.Resolve(key);

    public void Allow(Enumeration key, object source) => Link.Allow(key, source);

    public void Deny(Enumeration key, object source) => Link.Deny(key, source);

    public void AllowFrame(Enumeration key, object source) => Link.AllowFrame(key, source);

    public void DenyFrame(Enumeration key, object source) => Link.DenyFrame(key, source);

    public void Remove(Enumeration key, object source) => Link.Remove(key, source);

    public void ClearSource(object source) => Link.ClearSource(source);

    private void OnDestroy()
    {
        Link.Clear();
    }
}
