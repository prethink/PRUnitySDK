using System;
using System.Collections.Generic;

public class LanguageManager : ILanguageManager
{
    #region ILanguageManager

    public event Action<string> OnChangeLangEvent;

    private string currentLang = "ru";

    // ключ: язык, значение: словарь ключ → перевод
    private readonly Dictionary<string, Dictionary<string, string>> translations = new();

    public void InitSystem()
    {

    }

    public void InitLang(string lang)
    {
        SwitchLang(lang);
    }

    public void SwitchLang(string lang)
    {
        if (!translations.ContainsKey(lang)) 
            return;

        currentLang = lang;
        OnChangeLangEvent?.Invoke(lang);
    }

    public string GetCurrentLang()
    {
        return currentLang;
    }

    public void InvokeUpdateTranslate()
    {
        SwitchLang(PRUnitySDK.CurrentLang);
    }

    #endregion
}