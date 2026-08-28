using System;
using System.Collections.Generic;

public static class LocalizationUtils 
{
    /// <summary>
    /// Код языка для платформы по значению перечисления.
    /// Неизвестный язык отдаёт язык проекта по умолчанию.
    /// </summary>
    /// <param name="language">Язык из перечисления.</param>
    /// <returns>Строковый код языка.</returns>
    public static string GetLanguageCode(LangType language)
    {
        switch (language)
        {
            case LangType.Russian:
                return LangCode.RU;
            case LangType.English:
                return LangCode.EN;
            case LangType.Turkey:
                return LangCode.TR;
            default:
                return PRUnitySDK.DefaultLanguage; // язык проекта по умолчанию
        }
    }

    /// <summary>
    /// Значение перечисления по коду языка.
    /// Неизвестный код трактуется как английский.
    /// </summary>
    /// <param name="languageCode">Строковый код языка.</param>
    /// <returns>Язык из перечисления.</returns>
    public static LangType GetLanguageEnum(string languageCode)
    {
        switch (languageCode)
        {
            case LangCode.RU:
                return LangType.Russian;
            case LangCode.EN:
                return LangType.English;
            case LangCode.TR:
                return LangType.Turkey;
            default:
                return LangType.English;
        }
    }

    public static ILocalizationProvider CreateLocalization(string key, Dictionary<LangType, string> dictionary)
    {
        return new LocalizationControl(key, dictionary);
    }

    public static ILocalizationProvider CreateLocalization(Dictionary<LangType, string> dictionary)
    {
        return new LocalizationControl(Guid.NewGuid().ToString(), dictionary);
    }

    public static int GetMaxSizeMessage(ILocalizationProvider localization)
    {
        int maxSize = 0;
        foreach (var item in localization.LocalizationValues)
        {
            if(item.Value.Length > maxSize)
                maxSize = item.Value.Length;
        }

        return maxSize;
    }

    //public static IEnumerable<ILocalizationProvider> SplitLocalization(IEnumerable<int> chunkSize, ILocalizationProvider)
    //{

    //}
}
