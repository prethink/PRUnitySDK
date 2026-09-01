public partial class EntityTypeEnumerations : EnumerationProviderBase
{
    public static Enumeration Unknown       = new Enumeration(nameof(Unknown));
    public static Enumeration Common        = new Enumeration(nameof(Common));
    public static Enumeration Player        = new Enumeration(nameof(Player));
    public static Enumeration Portal        = new Enumeration(nameof(Portal));
    public static Enumeration Pet           = new Enumeration(nameof(Pet));
    public static Enumeration Hat           = new Enumeration(nameof(Hat));
    public static Enumeration Dashboard     = new Enumeration(nameof(Dashboard));
    public static Enumeration GameEvent     = new Enumeration(nameof(GameEvent));
    public static Enumeration Gift          = new Enumeration(nameof(Gift));
    public static Enumeration Reward        = new Enumeration(nameof(Reward));

    public override bool IncludeInherited => true;
}
