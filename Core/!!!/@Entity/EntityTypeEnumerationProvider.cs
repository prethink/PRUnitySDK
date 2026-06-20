public partial class EntityTypeEnumerationProvider : EnumerationProviderBase
{
    public static Enumeration Player        = new Enumeration(nameof(Player));
    public static Enumeration Portal        = new Enumeration(nameof(Portal));
    public static Enumeration Pet           = new Enumeration(nameof(Pet));
    public static Enumeration Dashboard     = new Enumeration(nameof(Dashboard));
    public static Enumeration GameEvent     = new Enumeration(nameof(GameEvent));
    public override bool IncludeInherited => true;
}
