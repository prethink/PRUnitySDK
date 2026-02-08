using System.Collections.Generic;

public static class StateId
{
    public static readonly Enumeration Idle = new("Idle");
    public static readonly Enumeration Move = new("Move");
    public static readonly Enumeration Attack = new("Attack");
    public static readonly Enumeration Death = new("Death");

    public static IEnumerable<Enumeration> GetAllOptions()
    {
        yield return Idle;
        yield return Move;
        yield return Attack;
    }
}
