public class EntityDisposeEnumerations : EnumerationProviderBase
{
    public static readonly Enumeration Destroy      = new Enumeration(nameof(Destroy));
    public static readonly Enumeration HideInPool   = new Enumeration(nameof(HideInPool));
    public static readonly Enumeration Hide         = new Enumeration(nameof(Hide));
    public static readonly Enumeration HideWire     = new Enumeration(nameof(HideWire));
    /// <inheritdoc />
    public override Enumeration Default => FirstOption;

    public override bool IncludeInherited => true;
}
