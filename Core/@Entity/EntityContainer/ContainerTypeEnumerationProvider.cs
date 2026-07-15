public partial class ContainerTypeEnumerationProvider : EnumerationProviderBase
{
    public static readonly Enumeration ResourceContainer        = new Enumeration(nameof(ResourceContainer));
    public static readonly Enumeration ActionContainer          = new Enumeration(nameof(ActionContainer));
    public override bool IncludeInherited => true;
}
