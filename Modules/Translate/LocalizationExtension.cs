using SABI;
using TMPro;

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

    public static void SetLocalization(this TextMeshProUGUI textMesh, ILocalizationProvider localization, string[] args)
    {
        var languageComponent = textMesh.GetLanguageComponent();
        languageComponent.SetLocalization(localization, args);
    }

    public static void SetLocalization(this TextMeshProUGUI textMesh, ILocalizationProvider localization)
    {
        var languageComponent = textMesh.GetLanguageComponent();
        languageComponent.SetLocalization(localization);
    }

    public static LocalizationObserver GetLanguageComponent(this TextMeshProUGUI textMesh)
    {
        var languageComponent = textMesh.GetComponent<LocalizationObserver>();
        if(languageComponent == null)
            languageComponent = textMesh.AddComponent<LocalizationObserver>();

        return languageComponent;
    }
}
