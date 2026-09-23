using System;
using UnityEngine;

/// <summary>
/// Выбирает правило, объявленное кодом, по имени.
/// </summary>
/// <remarks>
/// Для правил без настроек: «туториал пройден», «идёт бой с боссом». Сам ответ живёт
/// в <see cref="ConditionRegistry"/>, здесь только выбор имени.
/// <para>
/// Имя выбирается из набора <see cref="ConditionEnumerations"/>, а не пишется строкой:
/// строку опечатают, и ассет молча перестанет находить правило.
/// </para>
/// </remarks>
[Serializable]
public class RegisteredCondition : ICondition
{
    [SerializeField]
    [Tooltip("Имя правила, объявленного кодом игры.")]
    private EnumerationReference<ConditionEnumerations> rule = new();

    /// <summary>
    /// Имя выбранного правила.
    /// </summary>
    public Enumeration Rule => rule?.ToEnumeration();

    /// <inheritdoc />
    public bool Evaluate(GameObject actor = null)
    {
        return ConditionRegistry.Instance.Evaluate(Rule, actor);
    }
}
