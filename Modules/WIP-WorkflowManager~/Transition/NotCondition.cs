using System.Linq;

public class NotCondition<T> : ConditionBase<T>
    where T : WorkflowBase<T>
{
    public NotCondition(T manager, int priority, ICondition<T> condition) : base(manager, priority)
    {
        this.Conditions = new[] { condition };
    }

    public override bool Evaluate()
    {
        return !Conditions.Single().Evaluate();
    }
}
