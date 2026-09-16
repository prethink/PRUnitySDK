using System;
using System.Collections.Generic;
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

        // Проектная база важнее общей: игра переопределяет строку SDK своим ключом.
        var translate = FindTranslate(localizationDataBase.Project, key);

        if (string.IsNullOrEmpty(translate))
            translate = FindTranslate(localizationDataBase.Common, key);

        return string.IsNullOrEmpty(translate)
            ? $"NOT_FOUND_KEY_{key}"
            : GetTranslate(translate, args);
    }

    /// <summary>
    /// Ищет перевод ключа в указанном списке.
    /// </summary>
    private static string FindTranslate(List<LocalizationControl> localizations, string key)
    {
        var localization = localizations.FirstOrDefault(
            x => x.LocalizationKey.Trim().Equals(key.Trim(), StringComparison.OrdinalIgnoreCase));

        return localization?.GetTranslate(languageTranslator.GetCurrentLang());
    }

    public static IReadOnlyDictionary<LangType, string> GetDictionary(string key)
    {
        var localizationDataBase = PRUnitySDK.Database.LocalizationDatabase;

        var projectLocalization = localizationDataBase.Project.FirstOrDefault(x => x.LocalizationKey.Trim().Equals(key.Trim(), StringComparison.OrdinalIgnoreCase));
        if (projectLocalization != null)
            return projectLocalization.LocalizationValues;

        var commonLocalization = localizationDataBase.Common.FirstOrDefault(x => x.LocalizationKey.Trim().Equals(key.Trim(), StringComparison.OrdinalIgnoreCase));
        if (commonLocalization != null)
            return commonLocalization.LocalizationValues;

        return new Dictionary<LangType, string>();
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
