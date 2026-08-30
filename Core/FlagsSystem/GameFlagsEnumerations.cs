public class GameFlagsEnumerations : EnumerationProviderBase
{
    public static Enumeration UseGravity = new Enumeration(nameof(UseGravity));

    public override bool IncludeInherited => true;
}
