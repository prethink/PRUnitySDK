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
    private EnumerationReference<ResourceEnumerationProvider> resource;

    [SerializeField, Min(0)]
    [Tooltip("Сколько начислить.")]
    private long amount = 1;

    [SerializeField]
    [Tooltip("Сохранять сразу же. Для частых начислений выключите: запишется при автосохранении.")]
    private bool saveImmediately = true;

    /// <inheritdoc />
    public override bool CanExecute()
    {
        return base.CanExecute() && amount > 0 && resource.ToEnumeration() != null;
    }

    /// <inheritdoc />
    protected override void Action()
    {
        WalletService.Instance.Add(resource.ToEnumeration(), amount, saveImmediately);
    }
}
