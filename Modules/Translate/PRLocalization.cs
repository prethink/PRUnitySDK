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
        var index = LocalizationUtils.GetLanguageIndex(lang);
        return GetResultTranslate(localization, index);
    }

    public static string GetResultTranslate(ILocalizationProvider localization, int index)
    {
        if (localization == null)
            return "EMPTY_LOCALIZATION";

        if (index < 0 || index >= localization.LocalizationValues.Count())
            return localization.LocalizationKey;

        var value = localization.LocalizationValues .ElementAt(index);

        if (string.IsNullOrEmpty(value))
            return localization.LocalizationKey;

        return value;
    }
}
