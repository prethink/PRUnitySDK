#if !PRSDK_DISABLE_STEAM
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
using System;
using Steamworks;
using UnityEngine;

/// <summary>
/// Язык игры в Steam: выбор игрока, а без него — язык, выбранный для игры в клиенте Steam.
/// </summary>
/// <remarks>
/// Steam сообщает язык названием (<c>russian</c>, <c>english</c>, <c>turkish</c>…). В игре три языка: русский — для
/// русского, украинского и белорусского, турецкий — для турецкого, английский — для остальных. Выбор игрока хранится
/// на этом компьютере (<c>PlayerPrefs</c>, ключ <see cref="PrefsKey"/>).
/// </remarks>
public class SteamLanguageManager : ILanguageManager
{
    /// <summary>
    /// Ключ <c>PlayerPrefs</c> с выбранным языком.
    /// </summary>
    public const string PrefsKey = "prlang";

    private string currentLang = StartLanguage();

    /// <summary>
    /// Язык при запуске: выбранный игроком, иначе — по языку Steam.
    /// </summary>
    public static string StartLanguage()
    {
        string saved = PlayerPrefs.GetString(PrefsKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(saved))
            return saved;

        string steam = SteamPlatform.IsReady ? SteamApps.GetCurrentGameLanguage() : string.Empty;
        switch ((steam ?? string.Empty).ToLowerInvariant())
        {
            case "russian":
            case "ukrainian":
                return "ru";
            case "turkish":
                return "tr";
            case "":
                return Application.systemLanguage is SystemLanguage.Russian or SystemLanguage.Ukrainian or SystemLanguage.Belarusian
                    ? "ru"
                    : Application.systemLanguage == SystemLanguage.Turkish ? "tr" : "en";
            default:
                return "en";
        }
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
            PlayerPrefs.SetString(PrefsKey, lang);
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
#endif
