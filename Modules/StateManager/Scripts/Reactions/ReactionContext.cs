using System;

public class ReactionContext<TContext>
{
    public TContext Context { get; set; }

    public bool TryReact(TContext context, Func<bool> canReact, Action internalReact)
    {
        Context = context;

        if (!canReact())
            return false;

        internalReact();
        return true;
    }
}