using System;

public class Timer : IDisposable
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
    }

    public void Start()
    {
        if (isStarted)
            return;

        if (PRTime.Instance == null)
            return;

        isStarted = true;
        PRTime.Instance.OnNextSecond += OnSecondTick;
    }

    public void Stop()
    {
        if (!isStarted)
            return;

        isStarted = false;
        PRTime.Instance.OnNextSecond -= OnSecondTick;
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

    private void OnSecondTick(int _)
    {
        if (!isStarted)
            return;

        CurrentTime--;

        OnTick?.Invoke(CurrentTime);

        if (CurrentTime <= 0)
            End();
    }

    public void Dispose()
    {
        if (isDisposing)
            return;

        Stop();
        OnTick = null;
        endAction = null;
        isDisposing = true;
    }
}