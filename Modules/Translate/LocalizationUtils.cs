using System;
using System.Collections.Generic;

public static class LocalizationUtils 
{
    /// <summary>
    /// �������� ���� ����� � ������� ������ ��������� enum.
    /// </summary>
    /// <param name="language">enum ���� �����.</param>
    /// <returns>��������� �������� ����� �����.</returns>
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
                return PRUnitySDK.DefaultLanguage; // �� ��������� ����������
        }
    }

    /// <summary>
    /// �������� enum �������� ����� ��������� ���� �����.
    /// </summary>
    /// <param name="languageCode">���� �����.</param>
    /// <returns>���� ����� � ������� enum.</returns>
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
