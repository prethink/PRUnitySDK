using System;

public class BotEventArgs : EventArgsBase
{

}

public class BotAddEventArgs : BotEventArgs 
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

public class BotKillEventArgs : BotEventArgs { }

public class BotStopEventArgs : BotEventArgs { }

public class BotStartEventArgs : BotEventArgs { }
