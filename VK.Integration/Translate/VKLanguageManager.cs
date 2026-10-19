#if !PRSDK_DISABLE_VK
using System;

/// <summary>
/// Язык игры в VK: выбор игрока, а без него — язык интерфейса площадки.
/// </summary>
/// <remarks>
/// Площадка сообщает язык параметром запуска <c>vk_language</c>. В игре три языка: русский — для русского,
/// украинского, белорусского и казахского интерфейса, турецкий — для турецкого, английский — для остальных.
/// Сменённый игроком язык пишется в VK Storage ключом <see cref="StorageKey"/> и приходит при следующем запуске.
/// </remarks>
public class VKLanguageManager : ILanguageManager
{
    /// <summary>
    /// Ключ VK Storage с выбранным языком.
    /// </summary>
    public const string StorageKey = "prlang";

    private string currentLang = StartLanguage();

    /// <summary>
    /// Язык при запуске: выбранный игроком, иначе — по языку площадки.
    /// </summary>
    public static string StartLanguage()
    {
        VKInitResult launch = VKBridge.Launch;
        if (launch == null)
            return "ru";

        if (!string.IsNullOrWhiteSpace(launch.savedLanguage))
            return launch.savedLanguage.Trim();

        switch ((launch.language ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "":
            case "ru":
            case "uk":
            case "ua":
            case "be":
            case "kk":
            case "kz":
                return "ru";
            case "tr":
                return "tr";
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
            VKBridge.StorageSetValue(StorageKey, lang);
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
