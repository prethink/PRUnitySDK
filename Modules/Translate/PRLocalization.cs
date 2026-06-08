using System.Linq;

public static class PRLocalization
{
    public static string GetTranslate(ILocalizationProvider localization)
    {
        return GetTranslate(localization, PRUnitySDK.CurrentLang);
    }

    public static string GetTranslate(ILocalizationProvider localization, string langKey)
    {
        return GetTranslate(localization, LocalizationUtils.GetLanguageEnum(langKey));
    }

    public static string GetTranslate(ILocalizationProvider localization, LangType lang)
    {
        if (localization == null)
            return "EMPTY_LOCALIZATION";

        if(localization.LocalizationValues.TryGetValue(lang, out var value))
        {
            return value;
        }
        else
        {
            return $"{localization.LocalizationKey}, NotFoundTranslate";
        }
    }
}
