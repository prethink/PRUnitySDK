#if !PRSDK_DISABLE_PIKABU
using System;

/// <summary>
/// Язык игры на Пикабу: русский по умолчанию, выбор игрока хранится в облаке площадки.
/// </summary>
/// <remarks>
/// Площадка язык игрока не сообщает и требует, чтобы игра запускалась на русском. Сменённый игроком язык
/// пишется в облачное хранилище отдельным ключом <see cref="StorageKey"/>, рядом с сохранением игры, и при
/// следующем запуске экран загрузки и игра открываются уже на нём.
/// </remarks>
public class PikabuLanguageManager : ILanguageManager
{
    /// <summary>
    /// Ключ облачного хранилища с выбранным языком.
    /// </summary>
    public const string StorageKey = "prlang";

    /// <summary>
    /// Язык, на котором игра запускается без выбора игрока.
    /// </summary>
    public const string DefaultLanguage = "ru";

    private string currentLang = LoadSavedOrDefault();

    /// <summary>
    /// Язык, выбранный игроком, а без выбора — <see cref="DefaultLanguage"/>.
    /// </summary>
    public static string LoadSavedOrDefault()
    {
        string saved = PikabuSDK.StorageGet(StorageKey);
        return string.IsNullOrWhiteSpace(saved) ? DefaultLanguage : saved.Trim();
    }

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
        if (string.IsNullOrWhiteSpace(lang))
            return;

        if (lang != currentLang)
        {
            currentLang = lang;
            PikabuSDK.StorageSet(StorageKey, lang);
        }

        OnChangeLangEvent?.Invoke(lang);
    }

    public string GetCurrentLang()
    {
        return currentLang;
    }

    public void InvokeUpdateTranslate()
    {
        OnChangeLangEvent?.Invoke(currentLang);
    }

    #endregion
}
#endif
