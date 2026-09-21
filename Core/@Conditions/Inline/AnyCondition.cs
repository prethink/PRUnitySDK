using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Выполнено, когда выполнено хотя бы одно вложенное условие.
/// </summary>
/// <remarks>
/// Так описывают несколько путей к одному и тому же: цель открывается либо за кубки,
/// либо по уровню.
/// <para>
/// Пустой список считается выполненным — по той же причине, что и у
/// <see cref="AllCondition"/>: отсутствие правил не должно запирать объект.
/// </para>
/// </remarks>
[Serializable]
public class AnyCondition : ICondition
{
    [SerializeReference, ReferenceSelector]
    [Tooltip("Условия, из которых достаточно одного выполненного.")]
    private List<ICondition> conditions = new();

    /// <summary>
    /// Вложенные условия.
    /// </summary>
    public IReadOnlyList<ICondition> Conditions => conditions;

    /// <inheritdoc />
    public bool Evaluate()
    {
        if (conditions == null || conditions.Count == 0)
            return true;

        var hasAny = false;

        foreach (ICondition condition in conditions)
        {
            if (condition == null)
                continue;

            hasAny = true;

            if (condition.Evaluate())
                return true;
        }

        // Список из одних пустых строк - тот же недонастроенный слот, что и пустой список.
        return !hasAny;
    }
}
