using System.Collections.Generic;

public class AndCondition<T> : ConditionBase<T>
    where T : WorkflowBase<T>
{
    public AndCondition(T manager, int priority, IEnumerable<ICondition<T>> conditions)
        : base(manager, priority)
    {
        this.Conditions = conditions;
    }

    public override bool Evaluate()
    {
        foreach (var condition in this.Conditions)
        {
            if (!condition.Evaluate())
                return false;
        }

        return true;
    }
}
