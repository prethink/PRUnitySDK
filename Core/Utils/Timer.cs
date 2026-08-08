using System;

/// <summary>
/// Базовый таймер с шагом в одну секунду, управляемый событиями <see cref="PRTime"/>.
/// После использования таймер необходимо освободить через <see cref="Dispose"/>.
/// </summary>
public abstract class TimerBase : IDisposable
{
    /// <summary>
    /// Начальная длительность таймера в секундах.
    /// </summary>
    protected int time;

    /// <summary>
    /// Оставшееся количество секунд.
    /// </summary>
    public int CurrentTime { get; private set; }

    /// <summary>
    /// Признак того, что таймер запущен и должен обрабатывать секундные события.
    /// </summary>
    protected bool isStarted;

    /// <summary>
    /// Признак того, что ресурсы таймера освобождены и подписки удалены.
    /// </summary>
    protected bool isDisposing;

    /// <summary>
    /// Действие, вызываемое при завершении таймера.
    /// </summary>
    protected Action endAction;

    /// <summary>
    /// Вызывается после каждого секундного шага и передаёт оставшееся время.
    /// При достижении нуля событие вызывается перед действием завершения.
    /// </summary>
    public event Action<int> OnTick;

    /// <summary>
    /// Создаёт остановленный таймер и подписывает его на соответствующие события времени.
    /// </summary>
    /// <param name="time">Длительность таймера в секундах.</param>
    public TimerBase(int time)
    {
        this.time = time;
        this.CurrentTime = time;
        EventBusSubscribe();
    }

    /// <summary>
    /// Запускает или продолжает отсчёт.
    /// </summary>
    public void Start()
    {
        if (isStarted)
            return;

        if (PRTime.Instance == null)
            return;

        isStarted = true;
    }

    /// <summary>
    /// Приостанавливает отсчёт, сохраняя оставшееся время.
    /// </summary>
    public void Stop()
    {
        if (!isStarted)
            return;

        isStarted = false;
    }

    /// <summary>
    /// Останавливает таймер и восстанавливает первоначальную длительность.
    /// </summary>
    public void Reset()
    {
        Stop();
        CurrentTime = time;
    }

    /// <summary>
    /// Немедленно завершает таймер, устанавливает время в ноль
    /// и вызывает зарегистрированное действие завершения.
    /// </summary>
    public void End()
    {
        Stop();
        CurrentTime = 0;
        endAction?.Invoke();
    }

    /// <summary>
    /// Заменяет действие, вызываемое при завершении таймера.
    /// </summary>
    public void RegisterEndAction(Action action)
    {
        endAction = action;
    }

    /// <summary>
    /// Останавливает таймер, очищает callbacks и отписывает его от шины событий.
    /// После освобождения экземпляр не следует использовать повторно.
    /// </summary>
    public void Dispose()
    {
        if (isDisposing)
            return;

        Stop();
        OnTick = null;
        endAction = null;
        isDisposing = true;
        EventBusUnsubscribe();
    }

    /// <summary>
    /// Уменьшает оставшееся время на одну секунду.
    /// </summary>
    protected void Tick()
    {
        if (!isStarted)
            return;

        CurrentTime--;

        OnTick?.Invoke(CurrentTime);

        if (CurrentTime <= 0)
            End();
    }

    /// <summary>
    /// Подписывает таймер на источник секундных событий.
    /// </summary>
    protected abstract void EventBusSubscribe();

    /// <summary>
    /// Отписывает таймер от источника секундных событий.
    /// </summary>
    protected abstract void EventBusUnsubscribe();
}

/// <summary>
/// Таймер игрового времени, учитывающий логическую паузу и масштаб времени SDK.
/// </summary>
public class GameTimer : TimerBase, IOnGameSecondsEvent
{
    public GameTimer(int time) : base(time)
    {
    }

    /// <summary>
    /// Обрабатывает очередную игровую секунду.
    /// </summary>
    public void OnGameSecondTick(long currentSecond)
    {
        Tick();
    }

    protected override void EventBusSubscribe()
    {
        EventBus.Subscribe(this);
    }

    protected override void EventBusUnsubscribe()
    {
        EventBus.Unsubscribe(this);
    }
}

/// <summary>
/// Таймер реального времени SDK, не зависящий от логической паузы и игрового time scale.
/// </summary>
public class RealTimer : TimerBase, IOnRealSecondsEvent
{
    public RealTimer(int time) : base(time)
    {
    }

    /// <summary>
    /// Обрабатывает очередную реальную секунду.
    /// </summary>
    public void OnRealSecondTick(long currentSecond)
    {
        Tick();
    }

    protected override void EventBusSubscribe()
    {
        EventBus.Subscribe(this);
    }

    protected override void EventBusUnsubscribe()
    {
        EventBus.Unsubscribe(this);
    }
}
