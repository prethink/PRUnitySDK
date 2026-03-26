public class GameSettingsEnumerationProvider : EnumerationProviderBase
{
    public static readonly Enumeration<float> Sensitivity           = new Enumeration<float>(nameof(Sensitivity));
    public static readonly Enumeration<bool> InvertHorizontal       = new Enumeration<bool>(nameof(InvertHorizontal));
    public static readonly Enumeration<bool> InvertVertical         = new Enumeration<bool>(nameof(InvertVertical));
    public override bool IncludeInherited => true;
}
