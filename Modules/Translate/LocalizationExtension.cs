public static class LocalizationExtension 
{
    public static string GetTranslate(this ILocalizationProvider localization)
    {
        return PRLocalization.GetTranslate(localization);
    }

    public static string GetTranslate(this ILocalizationProvider localization, string langKey)
    {
        return PRLocalization.GetTranslate(localization, langKey);
    }

    public static string GetTranslate(this ILocalizationProvider localization, LangType lang)
    {
        return PRLocalization.GetTranslate(localization, lang);
    }
}
