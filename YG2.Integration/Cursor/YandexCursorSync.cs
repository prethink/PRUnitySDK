using System.Threading;
using UnityEngine;
using YG;

/// <summary>
/// Возвращает курсор менеджеру после паузы площадки.
/// </summary>
/// <remarks>
/// Плагин YG2 на время рекламы, оплаты и потери фокуса ставит игру на паузу и сам пишет
/// <c>Cursor.visible</c> / <c>Cursor.lockState</c>: показывает курсор, а выходя из паузы,
/// возвращает его таким, каким запомнил в её начале. <see cref="CursorManager"/> об этом
/// не знает. Отсюда поломка: игрок платит с площадки на сцене (курсор спрятан), оплата проходит,
/// окно награды просит курсор — и следом плагин прячет его обратно, потому что до паузы он был
/// спрятан. Окно на экране, а нажать «Получить» нечем.
/// <para>
/// Плагин восстанавливает курсор сразу после события выхода из паузы, тем же вызовом, поэтому
/// ответ на событие он бы затёр. Состояние применяется заново позже — следующим шагом главного
/// потока, когда плагин уже закончил.
/// </para>
/// <para>
/// Код плагина не правится: его обновление стёрло бы правку.
/// </para>
/// </remarks>
public static class YandexCursorSync
{
    private static SynchronizationContext mainThread;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Subscribe()
    {
        mainThread = SynchronizationContext.Current;

        // Сначала отписка: в редакторе без перезагрузки домена статика плагина переживает запуск,
        // и вторая игра подписалась бы дважды.
        YG2.onPauseGame -= OnPauseGame;
        YG2.onPauseGame += OnPauseGame;
    }

    private static void OnPauseGame(bool pause)
    {
        if (pause || mainThread == null)
            return;

        mainThread.Post(_ => Reapply(), null);
    }

    private static void Reapply()
    {
        // Пауза могла начаться снова, пока шаг ждал очереди: тогда курсором опять владеет плагин.
        if (!Application.isPlaying || YG2.isPauseGame)
            return;

        CursorManager.Instance.Reapply();
    }
}
