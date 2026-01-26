using DG.Tweening;
using System;
using System.Collections.Generic;

/// <summary>
/// Глобальный трекер DOTween-анимаций.
/// Позволяет централизованно:
/// - регистрировать Tween'ы
/// - управлять их жизненным циклом
/// - автоматически реагировать на паузу игры
/// </summary>
public class DoTweenTracker : SingletonProviderBase<DoTweenTracker>, IPauseStateListener
{
    /// <summary>
    /// Все зарегистрированные tween'ы по уникальному идентификатору.
    /// </summary>
    private Dictionary<Guid, Tween> tweens = new Dictionary<Guid, Tween>();

    /// <summary>
    /// Флаги реакции tween'а на паузу:
    /// true  — tween будет ставиться на паузу / возобновляться
    /// false — tween игнорирует паузу
    /// </summary>
    private Dictionary<Guid, bool> pauseData = new Dictionary<Guid, bool>();

    /// <summary>
    /// Регистрирует tween в трекере.
    /// </summary>
    /// <param name="tween">DOTween-анимация</param>
    /// <param name="reactionOnPause">
    /// Должен ли tween реагировать на паузу игры
    /// </param>
    /// <returns>Guid — идентификатор tween'а</returns>
    public Guid Register(Tween tween, bool reactionOnPause = true)
    {
        if (tween == null)
            throw new ArgumentNullException(nameof(tween));

        // Генерируем уникальный идентификатор
        Guid guid = Guid.NewGuid();

        // Сохраняем tween и его поведение при паузе
        tweens[guid] = tween;
        pauseData[guid] = reactionOnPause;

        return guid;
    }

    /// <summary>
    /// Принудительно убивает tween и удаляет его из трекера.
    /// </summary>
    /// <param name="guid">Идентификатор tween'а</param>
    public void Kill(Guid guid)
    {
        if (tweens.TryGetValue(guid, out Tween tween))
        {
            // Безопасно убиваем tween
            tween?.Kill();

            // Удаляем все связанные данные
            tweens.Remove(guid);
            pauseData.Remove(guid);
        }
    }

    /// <summary>
    /// Колбэк от системы паузы.
    /// Вызывается при изменении состояния паузы игры.
    /// </summary>
    public void OnPauseStateChanged(PauseEventArgs args)
    {
        // Список tween'ов, которые нужно удалить (если они стали null)
        List<Guid> toRemove = new List<Guid>();

        foreach (var kvp in tweens)
        {
            // Проверяем, должен ли tween реагировать на паузу
            if (pauseData.TryGetValue(kvp.Key, out var pauseRequired) && pauseRequired)
            {
                // Если tween был уничтожен извне — помечаем на удаление
                if (kvp.Value == null)
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }

                // Управляем tween'ом в зависимости от состояния паузы
                if (PRUnitySDK.PauseManager.IsLogicPaused)
                    kvp.Value.Pause();
                else
                    kvp.Value.Play();
            }
        }

        // Очищаем "битые" tween'ы
        foreach (var guid in toRemove)
        {
            tweens.Remove(guid);
        }
    }

    /// <summary>
    /// Конструктор.
    /// Подписывается на события системы паузы.
    /// </summary>
    public DoTweenTracker()
    {
        EventBus.Subscribe(this);
    }
}
