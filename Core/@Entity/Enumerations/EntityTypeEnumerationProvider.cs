public partial class EntityTypeEnumerations : EnumerationProviderBase
{
    public static Enumeration Unknown       = new Enumeration(nameof(Unknown));
    public static Enumeration Box           = new Enumeration(nameof(Box));
    public static Enumeration Common        = new Enumeration(nameof(Common));
    public static Enumeration Player        = new Enumeration(nameof(Player));
    public static Enumeration GameEvent     = new Enumeration(nameof(GameEvent));

    /// <inheritdoc />
    public override Enumeration Default => FirstOption;

    public override bool IncludeInherited => true;
}
