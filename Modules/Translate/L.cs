using System;
using System.Linq;

public static class L 
{
    private static ILanguageManager languageTranslator;

    /// <summary>
    /// Получить текущий перевод.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="args">Аргументы.</param>
    /// <returns></returns>
    public static string Tr(string key, params string[] args)
    {
        if (languageTranslator == null)
            return key;

        var localizationDataBase = PRUnitySDK.Database.LocalizationDatabase;
        var translate = string.Empty;
        var projectLocalization = localizationDataBase.Project.FirstOrDefault(x => x.Key.Trim().Equals(key.Trim(), StringComparison.OrdinalIgnoreCase));
        if (projectLocalization != null)
            translate = projectLocalization.GetTranslate(languageTranslator.GetCurrentLang());

        if (!string.IsNullOrEmpty(translate))
            return GetTranslate(translate, args);

        var commonLocalization = localizationDataBase.Common.FirstOrDefault(x => x.Key.Trim().Equals(key.Trim(), StringComparison.OrdinalIgnoreCase));
        if (commonLocalization != null)
            translate = commonLocalization.GetTranslate(languageTranslator.GetCurrentLang());

        if (!string.IsNullOrEmpty(translate))
            return GetTranslate(translate, args);

        return $"NOT_FOUND_KEY_{key}";
    }

    private static string GetTranslate(string translate, params string[] args)
    {
        if(args.Length == 0) 
            return translate;

        return string.Format(translate, args);
    }

    public static void InitTranslate(ILanguageManager translate)
    {
        languageTranslator = translate;
    }
}
