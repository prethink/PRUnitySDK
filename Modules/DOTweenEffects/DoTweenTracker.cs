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
public class DoTweenTracker : SingletonProviderBase<DoTweenTracker>, IPauseStateListener, IOnPRTimeScaleChange
{
    /// <summary>
    /// Все зарегистрированные tween'ы по уникальному идентификатору.
    /// </summary>
    private readonly Dictionary<Guid, TweenTimeScaleDTO> tweens = new();

    /// <summary>
    /// Флаги реакции tween'а на паузу:
    /// true  — tween будет ставиться на паузу / возобновляться
    /// false — tween игнорирует паузу
    /// </summary>
    private readonly Dictionary<Guid, bool> pauseData = new();

    /// <summary>
    /// Регистрирует tween в трекере.
    /// </summary>
    /// <param name="tween">DOTween-анимация</param>
    /// <param name="reactionOnPause">
    /// Должен ли tween реагировать на паузу игры
    /// </param>
    /// <returns>Guid — идентификатор tween'а</returns>
    public Guid Register(Tween tween, Enumeration layer = null, bool reactionOnPause = true)
    {
        if (tween == null)
            throw new ArgumentNullException(nameof(tween));

        if (layer == null)
            layer = PRTimeScaleEnumerations.Global;

        Guid guid = Guid.NewGuid();

        tween.SetId(guid);
        tween.timeScale = PRTimeScale.Instance.Resolve(layer);

        // Снимаем запись, только если она всё ещё указывает на ЭТОТ твин - защищает
        // от случая, когда для этого же guid успели зарегистрировать другой твин
        // (например, через RegisterOrReplace) раньше, чем сработал OnKill старого.
        tween.OnKill(() =>
        {
            if (tweens.TryGetValue(guid, out var current) && current.Tween == tween)
            {
                tweens.Remove(guid);
                pauseData.Remove(guid);
            }
        });

        var dto = new TweenTimeScaleDTO(tween, layer);
        if (reactionOnPause && PRUnitySDK.PauseManager.IsLogicPaused)
        {
            // Твин зарегистрирован во время паузы: он должен пойти, когда её снимут.
            dto.WasPlayingBeforePause = true;
            tween.Pause();
        }

        tweens[guid] = dto;
        pauseData[guid] = reactionOnPause;

        return guid;
    }

    /// <summary>
    /// Регистрирует tween под конкретным guid, убивая предыдущий tween с тем же id,
    /// если он был. Старый tween убивается ДО подписки колбэков нового - иначе
    /// асинхронный OnKill старого tween'а мог сработать уже после того, как
    /// новый tween был записан в tweens, и случайно удалить актуальную запись
    /// (гонка между Kill старого и записью нового под одним и тем же guid).
    /// </summary>
    public Guid RegisterOrReplace(Guid guid, Tween tween, Enumeration layer = null, bool reactionOnPause = true)
    {
        if (tween == null)
            throw new ArgumentNullException(nameof(tween));

        if (layer == null)
            layer = PRTimeScaleEnumerations.Global;

        if (tweens.TryGetValue(guid, out var existing))
        {
            tweens.Remove(guid);
            pauseData.Remove(guid);
            existing.Tween?.Kill();
        }

        tween.SetId(guid);
        tween.timeScale = PRTimeScale.Instance.Resolve(layer);

        tween.OnKill(() =>
        {
            if (tweens.TryGetValue(guid, out var current) && current.Tween == tween)
            {
                tweens.Remove(guid);
                pauseData.Remove(guid);
            }
        });

        var dto = new TweenTimeScaleDTO(tween, layer);
        if (reactionOnPause && PRUnitySDK.PauseManager.IsLogicPaused)
        {
            // Твин зарегистрирован во время паузы: он должен пойти, когда её снимут.
            dto.WasPlayingBeforePause = true;
            tween.Pause();
        }

        tweens[guid] = dto;
        pauseData[guid] = reactionOnPause;

        return guid;
    }

    /// <summary>
    /// Принудительно убивает tween и удаляет его из трекера.
    /// </summary>
    /// <param name="guid">Идентификатор tween'а</param>
    public void Kill(Guid guid)
    {
        if (tweens.TryGetValue(guid, out TweenTimeScaleDTO tweenDTO))
        {
            // Убираем запись ДО Kill() - OnKill-колбэк того же твина затем увидит,
            // что в tweens либо ничего нет под этим guid, либо там уже другой твин,
            // и корректно не тронет посторонние данные.
            tweens.Remove(guid);
            pauseData.Remove(guid);

            tweenDTO?.Tween?.Kill();
        }
    }

    /// <summary>
    /// Колбэк от системы паузы.
    /// Вызывается при изменении состояния паузы игры.
    /// </summary>
    public void OnPauseStateChanged(PauseStateEventArgs args)
    {
        List<Guid> toRemove = new List<Guid>();

        // Итерируем по снимку ключей, а не по самому словарю - если Pause()/Play()
        // синхронно вызовет колбэк, который трогает tweens (например, OnComplete
        // мгновенно завершённого твина дёргает Kill() трекера), прямая итерация
        // по tweens упала бы с InvalidOperationException.
        foreach (var guid in new List<Guid>(tweens.Keys))
        {
            if (!tweens.TryGetValue(guid, out var dto))
                continue; // запись уже удалена в течение этой же итерации

            if (!pauseData.TryGetValue(guid, out var pauseRequired) || !pauseRequired)
                continue;

            // dto.Tween.active - твин действительно ещё жив и не был убит/переиспользован
            // пулом DOTween в обход трекера (например, через DOTween.Kill(guid) напрямую).
            if (dto?.Tween == null || !dto.Tween.active)
            {
                toRemove.Add(guid);
                continue;
            }

            if (PRUnitySDK.PauseManager.IsLogicPaused)
            {
                // Запоминаем состояние только на переходе в паузу. Событие паузы может
                // прийти повторно — например, когда поверх одного окна открылось второе.
                // Раньше второй вызов перезаписывал флаг значением уже приостановленного
                // твина, то есть false, и после снятия паузы такой твин не запускался
                // никогда: анимация замирала навсегда вместе со всем, что её ждало.
                if (!dto.Tween.IsPlaying())
                    continue;

                dto.WasPlayingBeforePause = true;
                dto.Tween.Pause();
            }
            else if (dto.WasPlayingBeforePause)
            {
                dto.Tween.Play();
                dto.WasPlayingBeforePause = false;
            }
        }

        foreach (var guid in toRemove)
        {
            tweens.Remove(guid);
            pauseData.Remove(guid);
        }
    }

    /// <summary>
    /// Пересчитывает resolved time scale затронутых tween.
    /// Изменение глобального слоя обновляет все зарегистрированные tween.
    /// </summary>
    public void OnPRTimeScaleChange(Enumeration layer, float value)
    {
        foreach (var guid in new List<Guid>(tweens.Keys))
        {
            if (!tweens.TryGetValue(guid, out var dto))
                continue;

            if (layer != PRTimeScaleEnumerations.Global && dto.Layer != layer)
                continue;

            if (dto.Tween == null || !dto.Tween.active)
                continue;

            dto.Tween.timeScale = PRTimeScale.Instance.Resolve(dto.Layer);
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


/// <summary>
/// Runtime-данные зарегистрированного tween и его слоя времени.
/// </summary>
public class TweenTimeScaleDTO
{
    /// <summary>
    /// Зарегистрированная DOTween-анимация.
    /// </summary>
    public Tween Tween { get; }

    /// <summary>
    /// Слой PRTimeScale анимации.
    /// </summary>
    public Enumeration Layer { get; }

    /// <summary>
    /// Была ли анимация запущена перед логической паузой.
    /// </summary>
    public bool WasPlayingBeforePause { get; set; }

    /// <summary>
    /// Создаёт runtime-описание tween.
    /// </summary>
    public TweenTimeScaleDTO(Tween tween, Enumeration layer)
    {
        Tween = tween;
        Layer = layer;
    }
}
