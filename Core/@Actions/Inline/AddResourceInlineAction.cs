using System;
using UnityEngine;

/// <summary>
/// Встроенное действие: начисляет ресурс игроку.
/// </summary>
/// <remarks>
/// Пример действия с параметрами, ради которых встроенный вариант и нужен: количество
/// задаётся у конкретного объекта, и заводить ассет под каждое значение не приходится.
/// </remarks>
[Serializable]
public class AddResourceInlineAction : InlineActionBase
{
    [SerializeField]
    [Tooltip("Тип начисляемого ресурса.")]
    private EnumerationReference<ResourceEnumerations> resource;

    [SerializeField, Min(0)]
    [Tooltip("Сколько начислить.")]
    private long amount = 1;

    [SerializeField]
    [Tooltip("Сохранять сразу же. Для частых начислений выключите: запишется при автосохранении.")]
    private bool saveImmediately = true;

    /// <inheritdoc />
    public override ActionResult CanExecute()
    {
        ActionResult availability = base.CanExecute();
        if (availability.IsFailed)
            return availability;
        if (amount <= 0)
            return ActionResult.Fail(ActionLabels.InvalidAmount);
        if (resource == null || resource.ToEnumeration() == null)
            return ActionResult.Fail(ActionLabels.MissingResource);
        return ActionResult.Success;
    }

    /// <inheritdoc />
    protected override ActionResult Action()
    {
        WalletService.Instance.Add(resource.ToEnumeration(), amount, saveImmediately);
        return ActionResult.Success;
    }
}
