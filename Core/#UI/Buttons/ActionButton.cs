using UnityEngine;

public class ActionButton : ButtonBase
{
    [SerializeField] protected ActionBase action;

    public override bool CanExecute()
    {
        return base.CanExecute() && action != null && action.CanExecute();
    }

    protected override void InternalExecute()
    {
        action?.Execute();
    }
}

