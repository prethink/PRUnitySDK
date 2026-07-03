using System;

public class Timer : IDisposable, IOnSecondEvent
{
    private int time;
    public int CurrentTime { get; private set; }

    private bool isStarted;
    private bool isDisposing;
    private Action endAction;

    public event Action<int> OnTick;

    public Timer(int time)
    {
        this.time = time;
        this.CurrentTime = time;
        EventBus.Subscribe(this);
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
        EventBus.Unsubscribe(this);
    }

    public void OnSecondTick(long currentSecond)
    {
        if (!isStarted)
            return;

        CurrentTime--;

        OnTick?.Invoke(CurrentTime);

        if (CurrentTime <= 0)
            End();
    }
}