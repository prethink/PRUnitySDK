using System;

/// <summary>
/// Выполнено всегда.
/// </summary>
/// <remarks>
/// Для слота, где условие обязано быть, а спрашивать нечего: метка внимания на кнопке, которая
/// должна гореть постоянно. Пустой слот для этого не годится — там он значит «условия нет», и
/// владелец просто ничего не делает.
/// <para>
/// Обратное, «никогда», собирается из него же: <see cref="NotCondition"/> с этим условием внутри.
/// </para>
/// </remarks>
[Serializable]
public class AlwaysCondition : ICondition
{
    /// <inheritdoc />
    public ConditionResult Evaluate(ConditionContextBase context)
    {
        return ConditionResult.Success;
    }
}
