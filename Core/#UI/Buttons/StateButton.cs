using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StateButton : ButtonBase
{
    [SerializeField] protected List<StateIcon> stateIcon = new();
    public ValueRequestEvent<ValueStringContainer> OnRequestValue;

    protected override void Awake()
    {
        if (stateIcon.Count > 0)
            buttonIcon.sprite = stateIcon[0].Icon;

        OnPauseStateChanged(new PauseStateEventArgs());
        base.Awake();
    }


    protected override void InternalExecute()
    {
        var container = new ValueStringContainer();
        OnRequestValue?.Invoke(container);
        if (!string.IsNullOrEmpty(container.Value))
            buttonIcon.sprite = stateIcon.FirstOrDefault(x => x.Name.Equals(container.Value)).Icon;
    }

    public void ChangeState(ValueStringContainer container)
    {
        if (!string.IsNullOrEmpty(container.Value))
            buttonIcon.sprite = stateIcon.FirstOrDefault(x => x.Name.Equals(container.Value, StringComparison.OrdinalIgnoreCase)).Icon;
    }

    [Serializable]
    protected class StateIcon
    {
        public string Name;
        public Sprite Icon;
    }
}