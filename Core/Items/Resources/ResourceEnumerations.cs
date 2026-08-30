public partial class ResourceEnumerations : EnumerationProviderBase
{
    public static Enumeration Coin          = new Enumeration(nameof(Coin));
    public static Enumeration Crystal       = new Enumeration(nameof(Crystal));
    public override bool IncludeInherited => true;
}
