using System;

/// <summary>
/// Наблюдает таймер одной кнопки. Владелец регистрирует и снимает задачу вместе с UI.
/// </summary>
/// <remarks>
/// Значение - целые секунды до конца ожидания, ноль - ждать нечего. Секунды округляются
/// вверх: пока ожидание не кончилось, на кнопке не должно стоять «00:00». Подписчики
/// узнают об изменении раз в секунду, а не каждый кадр.
/// </remarks>
public sealed class UIButtonTimerWatcher : WatcherTask<long>
{
    private readonly IButtonTimer timer;
    private readonly Enumeration key;

    public UIButtonTimerWatcher(Enumeration key, IButtonTimer timer)
    {
        this.key = key;
        this.timer = timer;
    }

    /// <inheritdoc />
    public override Enumeration Key => key;
    /// <inheritdoc />
    public override float RepeatSeconds => 1f;

    /// <inheritdoc />
    public override long Read()
    {
        if (timer == null || !timer.TryGetRemaining(out TimeSpan remaining) || remaining <= TimeSpan.Zero)
            return 0;

        return (long)Math.Ceiling(remaining.TotalSeconds);
    }
}
