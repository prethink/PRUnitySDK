#if !PRSDK_DISABLE_YG2
using UnityEngine;
using YG;

/// <summary>
/// Возвращает фокус браузера в игру, когда площадка сняла свою паузу.
/// </summary>
/// <remarks>
/// Окно оплаты и реклама открываются на странице площадки и забирают фокус у фрейма игры.
/// Без фокуса игра стоит: Unity не рисует кадры (в сборке выключен <c>Run In Background</c>),
/// а <see cref="GameManager"/> держит свою паузу по фокусу. Покупка к этому моменту уже
/// выдана, но игрок видит замерший экран, пока не кликнет по игре.
/// <para>
/// Шаблон страницы плагина при снятии паузы зовёт только <c>canvas.focus()</c>. Это выбирает
/// холст внутри фрейма, но фокус окна браузера фрейму не отдаёт: на площадке в момент успеха
/// покупки <c>Application.isFocused</c> оставался <c>false</c>. Здесь к этому добавлен
/// <c>window.focus()</c> (<c>Plugins/YandexFocus.jslib</c>), и просьба повторяется несколько
/// раз с растущей задержкой: страница площадки в это время ещё закрывает своё окно и может
/// вернуть фокус себе уже после первой попытки.
/// </para>
/// <para>
/// Паузу по фокусу код сам не снимает: вернулся фокус — её снимет <see cref="GameManager"/>,
/// не вернулся — игра без фокуса и должна молчать (требование площадки 1.3). Браузер вправе
/// отказать в фокусе без действия игрока; тогда всё остаётся как было, до первого нажатия.
/// </para>
/// <para>
/// Код плагина и его шаблон страницы не правятся: обновление плагина стёрло бы правку.
/// </para>
/// </remarks>
public static class YandexFocusSync
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void PRFocusGame_Request();

    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void PRFocusGame_Cancel();
#else
    // В редакторе и вне браузера фокус окна ведёт сама Unity.
    private static void PRFocusGame_Request()
    {
    }

    private static void PRFocusGame_Cancel()
    {
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Subscribe()
    {
        // Сначала отписка: в редакторе без перезагрузки домена статика плагина переживает запуск,
        // и вторая игра подписалась бы дважды.
        YG2.onPauseGame -= OnPauseGame;
        YG2.onPauseGame += OnPauseGame;
    }

    private static void OnPauseGame(bool pause)
    {
        if (!Application.isPlaying)
            return;

        // Новая пауза — площадка снова что-то показывает: отнимать у неё фокус нельзя.
        if (pause)
            PRFocusGame_Cancel();
        else
            PRFocusGame_Request();
    }
}
#endif
