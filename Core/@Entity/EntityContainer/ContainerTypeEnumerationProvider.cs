public partial class ContainerTypeEnumerations : EnumerationProviderBase
{
    public static readonly Enumeration ResourceContainer        = new Enumeration(nameof(ResourceContainer));
    public static readonly Enumeration ActionContainer          = new Enumeration(nameof(ActionContainer));
    /// <inheritdoc />
    public override Enumeration Default => FirstOption;

    public override bool IncludeInherited => true;
}
