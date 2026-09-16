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
        ApplyState(container);
    }

    public void ChangeState(ValueStringContainer container)
    {
        ApplyState(container);
    }

    /// <summary>
    /// Ставит кнопке иконку запрошенного состояния.
    /// </summary>
    /// <remarks>
    /// Состояние без своей иконки оставляет кнопку как есть: пустой список или опечатка
    /// в имени не должны стирать картинку.
    /// </remarks>
    private void ApplyState(ValueStringContainer container)
    {
        if (container == null || string.IsNullOrEmpty(container.Value))
            return;

        var state = stateIcon.FirstOrDefault(
            x => x.Name.Equals(container.Value, StringComparison.OrdinalIgnoreCase));

        if (state != null)
            buttonIcon.sprite = state.Icon;
    }

    [Serializable]
    protected class StateIcon
    {
        public string Name;
        public Sprite Icon;
    }
}