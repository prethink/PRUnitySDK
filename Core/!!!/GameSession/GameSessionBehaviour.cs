using System;

public class GameSessionBehaviour : IDisposable
{
    public GameSessionManager Manager { get; set; }
    public GameSessionSettings Settings { get; set; }

    private IGameSessionListener listener;

    public bool IsActiveSession => Manager.IsActiveSession;

    protected void OnPreparingData() 
    {
        listener?.OnPreparingData();
    }

    protected void OnSessionStart() 
    {
        listener?.OnSessionStart();
    }

    protected void OnSessionEnd() 
    {
        listener?.OnSessionEnd();
    }

    public void Register(IGameSessionListener listener) 
        => this.listener = listener;

    public void Unregister()
        => this.listener = null;

    public void Dispose()
    {
        Manager.OnSessionStart -= OnSessionStart;
        Manager.OnSessionEnd -= OnSessionEnd;
        Manager.OnPreparingData -= OnPreparingData;
    }

    public GameSessionBehaviour(GameSessionManager gameSessionManager, GameSessionSettings gameSessionSettings)
    {
        Manager = gameSessionManager;
        Settings = gameSessionSettings;

        Manager.OnSessionStart += OnSessionStart;
        Manager.OnSessionEnd += OnSessionEnd;
        Manager.OnPreparingData += OnPreparingData;
    }
}
