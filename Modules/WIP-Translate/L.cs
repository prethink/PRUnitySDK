public static class L 
{
    private static ILocalizationService localizationService;
    private static ILanguageManager languageTranslator;

    /// <summary>
    /// Получить текущий перевод.
    /// </summary>
    /// <param name="translateKey"></param>
    /// <returns></returns>
    public static string Tr(string translateKey)
    {
        if (localizationService == null || languageTranslator == null)
            return translateKey;

        return localizationService.GetTranslation(translateKey, languageTranslator.GetCurrentLang());
    }

    public static void InitLocalizationService(ILocalizationService service)
    {
        localizationService = service;
    }

    public static void InitTranslate(ILanguageManager translate)
    {
        languageTranslator = translate;
    }
}
