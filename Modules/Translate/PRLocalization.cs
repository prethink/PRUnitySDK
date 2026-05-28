using System.Linq;

public static class PRLocalization
{
    public static string GetTranslate(ILocalization localization)
    {
        return GetTranslate(localization, PRUnitySDK.CurrentLang);
    }

    public static string GetTranslate(ILocalization localization, string langKey)
    {
        return GetTranslate(localization, LocalizationUtils.GetLanguageEnum(langKey));
    }

    public static string GetTranslate(ILocalization localization, LangType lang)
    {
        var index = LocalizationUtils.GetLanguageIndex(lang);
        return GetResultTranslate(localization, index);
    }

    public static string GetResultTranslate(ILocalization localization, int index)
    {
        if (localization == null)
            return "EMPTY_LOCALIZATION";

        if (index < 0 || index >= localization.LangData.Count())
            return localization.Key;

        var value = localization.LangData.ElementAt(index);

        if (string.IsNullOrEmpty(value))
            return localization.Key;

        return value;
    }
}
