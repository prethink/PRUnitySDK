using System;

public abstract class BotEventArgsBase : EventArgsBase
{
    public long? BotId { get; protected set; }
    public bool All { get; protected set; }
}

public class BotAddEventArgs : BotEventArgsBase 
{
    public int Count { get; private set; }

    public BotAddEventArgs()
    {
        Count = 1;
    }

    public BotAddEventArgs(int count)
    {
        if(count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Count должен быть >= 1");
        Count = count;
    }
}

public class BotKillEventArgs : BotEventArgsBase { }

public class BotStopEventArgs : BotEventArgsBase 
{ 
    public BotStopEventArgs()
    {
        All = true;
    }

    public BotStopEventArgs(long botId)
    {
        BotId = botId;
    }
}

public class BotStartEventArgs : BotEventArgsBase
{
    public BotStartEventArgs()
    {
        All = true;
    }

    public BotStartEventArgs(long botId)
    {
        BotId = botId;
    }
}

public class BotSetPathEventArgs : BotEventArgsBase
{
    public WaypointController Route { get; private set; }

    public BotSetPathEventArgs(WaypointController route)
    {
        All = true;
        this.Route = route;
    }

    public BotSetPathEventArgs(WaypointController route, long botId)
    {
        BotId = botId;
        this.Route = route;
    }
}
