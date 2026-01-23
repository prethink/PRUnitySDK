using System;

public class GameSession : ISession
{
    public RoundSession RoundSession { get; private set; }
    public bool IsActiveSession { get; private set; }
    public Guid SessionId => throw new NotImplementedException();

    public DateTime StartTime => throw new NotImplementedException();

    public DateTime? EndTime => throw new NotImplementedException();

    public void End()
    {
        throw new NotImplementedException();
    }

    public void Start()
    {
        throw new NotImplementedException();
    }

    public GameSession()
    {
        RoundSession = new RoundSession(this);
    }
}
