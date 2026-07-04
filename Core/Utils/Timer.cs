using System;

public abstract class TimerBase : IDisposable
{
    protected int time;
    public int CurrentTime { get; private set; }

    protected bool isStarted;
    protected bool isDisposing;
    protected Action endAction;

    public event Action<int> OnTick;

    public TimerBase(int time)
    {
        this.time = time;
        this.CurrentTime = time;
        EventBusSubscribe();
    }

    public void Start()
    {
        if (isStarted)
            return;

        if (PRTime.Instance == null)
            return;

        isStarted = true;
    }

    public void Stop()
    {
        if (!isStarted)
            return;

        isStarted = false;
    }

    public void Reset()
    {
        Stop();
        CurrentTime = time;
    }

    public void End()
    {
        Stop();
        CurrentTime = 0;
        endAction?.Invoke();
    }

    public void RegisterEndAction(Action action)
    {
        endAction = action;
    }

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

    protected void Tick()
    {
        if (!isStarted)
            return;

        CurrentTime--;

        OnTick?.Invoke(CurrentTime);

        if (CurrentTime <= 0)
            End();
    }

    protected abstract void EventBusSubscribe();
    protected abstract void EventBusUnsubscribe();
}

public class GameTimer : TimerBase, IOnGameSecondsEvent
{
    public GameTimer(int time) : base(time)
    {
    }

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

public class RealTimer : TimerBase, IOnRealSecondsEvent
{
    public RealTimer(int time) : base(time)
    {
    }

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