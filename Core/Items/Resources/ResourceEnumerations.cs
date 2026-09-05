public partial class ResourceEnumerations : EnumerationProviderBase
{
    public static Enumeration Coin          = new Enumeration(nameof(Coin));
    public static Enumeration Crystal       = new Enumeration(nameof(Crystal));
    /// <inheritdoc />
    public override Enumeration Default => FirstOption;

    public override bool IncludeInherited => true;
}
