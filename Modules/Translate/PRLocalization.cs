using System;

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
            TryApplyAffix(localization, ref value);
            return value;
        }
        else
        {
            return $"{localization.LocalizationKey}, NotFoundTranslate";
        }
    }

    private static void TryApplyAffix(ILocalizationProvider localization, ref string value)
    {
        if (localization is ILocalizationAffix affix && affix.Options != null)
        {
            if (affix.Options.Type == AffixType.Prefix)
                value = affix.Options.Value + value;
            else if (affix.Options.Type == AffixType.Postfix)
                value += affix.Options.Value;
            else
                throw new NotImplementedException();
        }
    }
}
