using UnityEngine;

/// <summary>
/// Контекст с участником: условие спрашивают для конкретного объекта.
/// </summary>
/// <remarks>
/// Этим передают инициатора срабатывания или атакующего. Кому нужны ещё и детали
/// события — оружие, сила удара, — заводит своего наследника
/// <see cref="ConditionContextBase"/>.
/// </remarks>
public class ConditionActorContext : ConditionContextBase
{
    private readonly GameObject actor;

    /// <summary>
    /// Собирает контекст для участника.
    /// </summary>
    /// <param name="actor">Участник; <c>null</c> допустим и значит «участника нет».</param>
    public ConditionActorContext(GameObject actor)
    {
        this.actor = actor;
    }

    /// <inheritdoc />
    public override GameObject Actor => actor;
}
