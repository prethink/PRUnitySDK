using System;

public class LocalServerTime : IServerTime
{
    public DateTime GetNow()
    {
        return DateTime.Now;
    }
}
