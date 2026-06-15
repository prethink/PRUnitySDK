public class LocalizationAffixOptions
{
    public string Value { get; set; }
    public AffixType Type { get; set; }

    public static LocalizationAffixOptions Postfix(string value)
    {
        return new LocalizationAffixOptions
        {
            Value = value,
            Type = AffixType.Postfix
        };
    }

    public static LocalizationAffixOptions Prefix(string value)
    {
        return new LocalizationAffixOptions
        {
            Value = value,
            Type = AffixType.Prefix
        };
    }
}

public enum AffixType
{
    Prefix,
    Postfix
}