using System;
using System.Linq;

public static class L 
{
    private static ILanguageManager languageTranslator;

    /// <summary>
    /// Получить текущий перевод.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string Tr(string key)
    {
        if (languageTranslator == null)
            return key;

        var localizationDataBase = PRUnitySDK.Database.LocalizationDatabase;
        var translate = string.Empty;
        var projectLocalization = localizationDataBase.Project.FirstOrDefault(x => x.Key.Trim().Equals(key.Trim(), StringComparison.OrdinalIgnoreCase));
        if (projectLocalization != null)
            translate = projectLocalization.GetTranslate(languageTranslator.GetCurrentLang());

        if (!string.IsNullOrEmpty(translate))
            return translate;

        var commonLocalization = localizationDataBase.Common.FirstOrDefault(x => x.Key.Trim().Equals(key.Trim(), StringComparison.OrdinalIgnoreCase));
        if (commonLocalization != null)
            translate = commonLocalization.GetTranslate(languageTranslator.GetCurrentLang());

        if (!string.IsNullOrEmpty(translate))
            return translate;

        return $"NOT_FOUND_KEY_{key}";
    }

    public static void InitTranslate(ILanguageManager translate)
    {
        languageTranslator = translate;
    }
}
