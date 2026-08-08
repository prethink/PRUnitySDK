using System;

public abstract class CooldownBase
{
    /// <summary>
    /// Время последнего успешного выполнения. Отрицательная бесконечность означает,
    /// что новый cooldown готов к использованию сразу после создания.
    /// </summary>
    private float lastTime;

    /// <summary>
    /// Включает диагностическое сообщение при попытке использовать неготовый cooldown.
    /// По умолчанию логирование выключено.
    /// </summary>
    public bool RequiredLogging { get; set; }

    /// <summary>
    /// Источник диагностического сообщения. Если не задан, используется экземпляр cooldown.
    /// </summary>
    public object LogInitiator { get; set; }

    protected abstract PRTimeType timeType { get; }

    /// <summary>
    /// Выполняет действие, если с последнего успешного выполнения прошёл указанный интервал.
    /// Новый экземпляр готов к первому выполнению сразу.
    /// </summary>
    /// <param name="interval">Интервал между успешными выполнениями в секундах.</param>
    /// <param name="action">Действие. Может быть null; cooldown всё равно будет запущен.</param>
    /// <returns>True, если cooldown был готов и действие было обработано.</returns>
    public bool TryExecute(float interval, Action action)
    {
        var now = GetTime();
        if (IsReady(now, interval))
        {
            lastTime = now;
            action?.Invoke();
            return true;
        }

        LogNotReady(now, interval);

        return false;
    }

    /// <summary>
    /// Запускает действие с задержкой. Метод не изменяет состояние cooldown.
    /// </summary>
    public void ExecuteAfter(float timeout, Action action)
    {
        this.DelayAction(timeout, (t) => action?.Invoke(), timeType);
    }

    /// <summary>
    /// Выполняет функцию, если cooldown готов; иначе возвращает fallback.
    /// </summary>
    public T ExecuteWithResult<T>(float interval, Func<T> action, T fallback)
    {
        var now = GetTime();
        if (IsReady(now, interval))
        {
            lastTime = now;
            return action.Invoke();
        }

        LogNotReady(now, interval);

        return fallback;
    }

    protected abstract float GetTime();

    protected CooldownBase(float lastTime = float.NegativeInfinity)
    {
        this.lastTime = lastTime;
    }

    private bool IsReady(float now, float interval)
    {
        // Вычитание устойчивее сложения абсолютного времени: lastTime + interval
        // со временем теряет точность и потенциально может переполниться.
        return now - lastTime >= Math.Max(0f, interval);
    }

    private void LogNotReady(float now, float interval)
    {
        if (!RequiredLogging)
            return;

        var timeLeft = Math.Max(0f, interval - (now - lastTime));
        var initiator = LogInitiator ?? this;
        PRLog.WriteDebug(
            initiator,
            $"Cooldown for {initiator} is not ready yet. Time left: {timeLeft:F2}",
            new PRLogSettings { LevelDebug = 10 });
    }
}

public class CooldownRealTime : CooldownBase
{
    protected override PRTimeType timeType => PRTimeType.RealTime;

    protected override float GetTime()
    {
        return PRTime.Instance.RealTime;
    }
}

public class CooldownGameTime : CooldownBase
{
    protected override PRTimeType timeType => PRTimeType.GameTime;

    protected override float GetTime()
    {
        return PRTime.Instance.GameTime;
    }
}
