#if !PRSDK_DISABLE_GAMEPUSH
using System;
using GamePush;

/// <summary>
/// Язык игры по GamePush: площадка знает язык игрока и сама запоминает его смену.
/// </summary>
/// <remarks>
/// Коды языков у GamePush те же, что у Яндекса: «ru», «en», «tr».
/// </remarks>
public class GamePushLanguageManager : ILanguageManager
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
        GP_Language.Change(lang);
    }

    public string GetCurrentLang()
    {
        return GP_Language.CurrentISO();
    }

    public void InvokeUpdateTranslate()
    {
        OnChangeLangEvent?.Invoke(PRUnitySDK.CurrentLang);
    }

    #endregion

    /// <summary>
    /// Площадка сообщает язык значением своего перечисления, а SDK ждёт код.
    /// </summary>
    private void HandleLanguageChanged(GamePush.Language language)
    {
        OnChangeLangEvent?.Invoke(GP_Language.CurrentISO());
    }

    #region Конструкторы

    public GamePushLanguageManager()
    {
        GP_Language.OnChangeLanguage += HandleLanguageChanged;
    }

    #endregion

    #region Деструкторы

    ~GamePushLanguageManager()
    {
        GP_Language.OnChangeLanguage -= HandleLanguageChanged;
    }

    #endregion
}
#endif
