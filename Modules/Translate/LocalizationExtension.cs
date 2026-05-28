public static class LocalizationExtension 
{
    public static string GetTranslate(this ILocalization localization)
    {
        return PRLocalization.GetTranslate(localization);
    }

    public static string GetTranslate(this ILocalization localization, string langKey)
    {
        return PRLocalization.GetTranslate(localization, langKey);
    }

    public static string GetTranslate(this ILocalization localization, LangType lang)
    {
        return PRLocalization.GetTranslate(localization, lang);
    }
}
