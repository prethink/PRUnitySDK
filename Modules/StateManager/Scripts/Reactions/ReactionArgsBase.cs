public abstract class ReactionArgsBase : EventArgsBase
{

}

public class EnumerationReactionArgs : ReactionArgsBase
{
    public Enumeration Enumeration { get; protected set; }

    public EnumerationReactionArgs(Enumeration enumeration)
    {
        this.Enumeration = enumeration;
    }
}