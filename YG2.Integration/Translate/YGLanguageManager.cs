using System;
using YG;

public class YGLanguageManager : ILanguageManager
{
    #region ILanguageManager

    public event Action<string> OnChangeLangEvent;

    public void InitSystem()
    {
    }

    public void InitLang(string lang)
    {
        SwitchLang(lang);
    }

    public void SwitchLang(string lang)
    {
        YG2.SwitchLanguage(lang);
    }

    private void OnChangeLangEventInvoker(string lang)
    {
        OnChangeLangEvent?.Invoke(lang);
    }

    public string GetCurrentLang()
    {
        return YG2.lang;
    }

    public void InvokeUpdateTranslate()
    {
        OnChangeLangEventInvoker(PRUnitySDK.CurrentLang);
    }

    #endregion

    #region Конструкторы

    public YGLanguageManager()
    {
        YG2.onSwitchLang += OnChangeLangEventInvoker;
    }

    #endregion

    #region Деструкторы

    ~YGLanguageManager()
    {
        YG2.onSwitchLang -= OnChangeLangEventInvoker;
    }

    #endregion
}
