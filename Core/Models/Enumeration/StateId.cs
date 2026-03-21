using System.Collections.Generic;

public class StateId : IEnumerationProvider
{
    public static readonly Enumeration Idle = new("Idle");
    public static readonly Enumeration Move = new("Move");
    public static readonly Enumeration Attack = new("Attack");
    public static readonly Enumeration Death = new("Death");

    public IEnumerable<Enumeration> GetOptions()
    {
        yield return Idle;
        yield return Move;
        yield return Attack;
    }
}
