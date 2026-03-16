using System.Collections.Generic;

public class OrCondition<T> : ConditionBase<T>
    where T : WorkflowBase<T>
{
    public OrCondition(T manager, int priority, IEnumerable<ICondition<T>> conditions)
        : base(manager, priority)
    {
        this.Conditions = conditions;
    }

    public override bool Evaluate()
    {
        foreach (var condition in this.Conditions)
        {
            if (condition.Evaluate())
                return true;
        }

        return false;
    }
}
