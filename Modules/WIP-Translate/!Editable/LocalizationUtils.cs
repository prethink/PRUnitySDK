public static class LocalizationUtils 
{
    /// <summary>
    /// Получить ключ языка в формате строки используя enum.
    /// </summary>
    /// <param name="language">enum ключ языка.</param>
    /// <returns>Строковое значение ключа языка.</returns>
    public static string GetLanguageCode(LangType language)
    {
        switch (language)
        {
            case LangType.Russian:
                return LangDropDown.RU;
            case LangType.English:
                return LangDropDown.EN;
            case LangType.Turkey:
                return LangDropDown.TR;
            default:
                return PRUnitySDK.DefaultLanguage; // По умолчанию английский
        }
    }

    /// <summary>
    /// Получить enum значения языка используя ключ языка.
    /// </summary>
    /// <param name="languageCode">Ключ языка.</param>
    /// <returns>Ключ языка в формате enum.</returns>
    public static LangType GetLanguageEnum(string languageCode)
    {
        switch (languageCode)
        {
            case LangDropDown.RU:
                return LangType.Russian;
            case LangDropDown.EN:
                return LangType.English;
            case LangDropDown.TR:
                return LangType.Turkey;
            default:
                return LangType.English;
        }
    }
}
