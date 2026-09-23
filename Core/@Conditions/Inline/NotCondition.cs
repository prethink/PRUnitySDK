using System;
using UnityEngine;

/// <summary>
/// Выполнено, когда вложенное условие не выполнено.
/// </summary>
/// <remarks>
/// Избавляет от парных условий вида «есть ресурс» и «нет ресурса»: правило пишется одно,
/// а обратное собирается здесь.
/// </remarks>
[Serializable]
public class NotCondition : ICondition
{
    [SerializeReference, ReferenceSelector]
    [Tooltip("Условие, которое переворачивается.")]
    private ICondition condition;

    /// <inheritdoc />
    /// <remarks>
    /// Пустой слот считается выполненным, а не перевёрнутым в запрет: недонастроенное
    /// условие нигде в системе не запирает.
    /// </remarks>
    public bool Evaluate(GameObject actor = null)
    {
        return condition == null || !condition.Evaluate(actor);
    }
}
