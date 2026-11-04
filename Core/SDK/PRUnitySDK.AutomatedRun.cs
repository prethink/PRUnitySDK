using UnityEngine;

public partial class PRUnitySDK
{
    private static bool isAutomatedRun;

    /// <summary>
    /// Игрой управляет автоматическая проверка, а не игрок.
    /// </summary>
    /// <remarks>
    /// На этот признак смотрят те, кто останавливает игру снаружи: потеря фокуса, автоматическая реклама. Проверке
    /// они мешают, а игроку нет: заставка выключила бы кнопки, которые она нажимает. Паузу самих окон признак не
    /// отменяет. Новая помеха просто спрашивает его: ничего регистрировать не нужно.
    /// <para>
    /// Пока признак стоит, пауза по фокусу снята; при снятии признака она возвращается по настоящему фокусу.
    /// </para>
    /// </remarks>
    public static bool IsAutomatedRun
    {
        get => isAutomatedRun;
        set
        {
            if (isAutomatedRun == value)
                return;

            isAutomatedRun = value;
            PauseManager.SetFocusPaused(!value && !Application.isFocused, typeof(PRUnitySDK));
        }
    }
}
