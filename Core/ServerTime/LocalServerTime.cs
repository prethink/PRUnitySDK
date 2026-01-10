using System;

public class LocalServerTime : ServerTimeBase
{
    public override DateTime GetNow()
    {
        return DateTime.Now;
    }
}
