public class GameSettingsEnumerations : EnumerationProviderBase
{
    public static readonly EnumerationType<float> Sensitivity           = new EnumerationType<float>(nameof(Sensitivity));
    public static readonly EnumerationType<bool> InvertHorizontal       = new EnumerationType<bool>(nameof(InvertHorizontal));
    public static readonly EnumerationType<bool> InvertVertical         = new EnumerationType<bool>(nameof(InvertVertical));
    /// <inheritdoc />
    public override Enumeration Default => FirstOption;

    public override bool IncludeInherited => true;
}
