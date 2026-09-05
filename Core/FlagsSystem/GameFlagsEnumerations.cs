public class GameFlagsEnumerations : EnumerationProviderBase
{
    public static Enumeration UseGravity = new Enumeration(nameof(UseGravity));

    /// <inheritdoc />
    public override Enumeration Default => FirstOption;

    public override bool IncludeInherited => true;
}
