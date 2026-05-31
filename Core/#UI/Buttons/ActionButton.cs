using UnityEngine;

public class ActionButton : ButtonBase
{
    [SerializeField] protected ActionBase action;

    protected override void OnEnable()
    {
        base.OnEnable();
        button.onClick.AddListener(() => { ButtonAction(); });
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        button.onClick.RemoveAllListeners();
    }

    public override bool CanExecute()
    {
        return base.CanExecute() && action.CanExecute();
    }

    private void ButtonAction()
    {
        if (!CanExecute())
            return;

        action.Execute();
    }
}

