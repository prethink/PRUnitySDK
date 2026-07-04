public partial class PRTimeScaleEnumerationProvider : EnumerationProviderBase
{
    public override bool IncludeInherited => true;

    public static readonly Enumeration Global   = new Enumeration(nameof(Global));
    public static readonly Enumeration Player   = new Enumeration(nameof(Player));
    public static readonly Enumeration NPC      = new Enumeration(nameof(NPC));
    public static readonly Enumeration UI       = new Enumeration(nameof(UI));
}
