using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Выполнено, когда выполнены все вложенные условия.
/// </summary>
/// <remarks>
/// Без такого условия каждый, кто спрашивает, писал бы свой цикл по списку — и общий
/// механизм потерял бы смысл: слот у владельца один, а правил бывает несколько.
/// <para>
/// Пустой список считается выполненным: «ограничений нет» — это разрешение, а не запрет.
/// Иначе забытая настройка запирала бы объект наглухо.
/// </para>
/// </remarks>
[Serializable]
public class AllCondition : ICondition
{
    [SerializeReference, ReferenceSelector]
    [Tooltip("Условия, которые должны выполниться все сразу.")]
    private List<ICondition> conditions = new();

    /// <summary>
    /// Вложенные условия.
    /// </summary>
    public IReadOnlyList<ICondition> Conditions => conditions;

    /// <inheritdoc />
    public bool Evaluate(ConditionContextBase context)
    {
        if (conditions == null)
            return true;

        foreach (ICondition condition in conditions)
        {
            // Пустая строка списка - недонастроенный слот, а не запрет.
            if (condition != null && !condition.Evaluate(context))
                return false;
        }

        return true;
    }
}
