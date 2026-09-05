public abstract class FlagsProviderBase : EnumerationProviderBase
{
    /// <inheritdoc />
    public override Enumeration Default => FirstOption;

    public override bool IncludeInherited => true;
}
