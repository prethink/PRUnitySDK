using TMPro;

public class LocalizationService : ILocalizationService
{
    public TMP_FontAsset GetFontOrNull(string langKey)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// Получить перевод по ключу перевода и ключу языка.
    /// </summary>
    /// <param name="key">Ключ перевода.</param>
    /// <param name="lang">Ключ языка.</param>
    /// <returns>Перевод или ключ, если перевод не найден.</returns>
    public string GetTranslation(string key, string lang)
    {
        //if (translationDictionary == null)
        //    Initialize();

        //if (translationDictionary.TryGetValue(key, out LocalizedText entry))
        //    return entry.GetTranslation(IsLanguageEnabled(GetLanguageEnum(lang)) ? lang : DefaultLanguage);

        //return key;
        return "";
    }
}
