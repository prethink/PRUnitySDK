#if !PRSDK_DISABLE_YG2
using UnityEngine;
using YG;

/// <summary>
/// Снимает паузу по фокусу, когда площадка возвращает игру из своей паузы.
/// </summary>
/// <remarks>
/// Окно оплаты и реклама открываются поверх игры на странице площадки и забирают фокус
/// браузера. <see cref="GameManager"/> видит это как потерю фокуса и ставит свою паузу.
/// Закрыв окно, площадка сообщает, что игра снова идёт, но фокус браузер сам во фрейм игры
/// не возвращает — только с первым нажатием игрока. До тех пор пауза по фокусу держалась:
/// покупка оплачена, а окно награды не появляется, пока игрок не кликнет по игре.
/// <para>
/// Здесь слову площадки верят больше, чем событию браузера: раз она сняла паузу, игра видна
/// и должна идти. Уход на другую вкладку это не ломает — о нём площадка сообщает своей паузой,
/// а скрытую страницу ловит <c>OnApplicationPause</c>.
/// </para>
/// <para>
/// Клавиатура при этом всё равно заработает только после нажатия по игре: это правило
/// браузера, отсюда его не обойти.
/// </para>
/// </remarks>
public static class YandexFocusSync
{
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
        if (pause || !Application.isPlaying)
            return;

        IPauseManager manager = PRUnitySDK.PauseManager;

        if (manager == null || !manager.IsFocusPaused || !GameManager.HasInstance)
            return;

        // От имени GameManager, как и настоящая смена фокуса: звук возвращает только то,
        // что пришло от него (AudioMixerManager). С другим именем игра пошла бы без звука.
        manager.SetFocusPaused(false, GameManager.Instance);
    }
}
#endif
