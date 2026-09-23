using System;
using UnityEngine;

/// <summary>
/// Встроенное условие, которое спрашивает условие-ассет.
/// </summary>
/// <remarks>
/// Мостик между двумя способами хранения. Слоты владельцев объявлены через
/// <c>SerializeReference</c>, а ассет так не положишь — он ссылка на объект Unity,
/// а не встроенный объект. Через этот переходник общее правило из ассета попадает
/// и в список <see cref="AllCondition"/>, и в любой другой слот.
/// </remarks>
[Serializable]
public class AssetCondition : ICondition
{
    [SerializeField]
    [Tooltip("Условие-ассет, у которого спрашивают ответ.")]
    private ConditionBase condition;

    /// <summary>
    /// Условие-ассет, у которого спрашивают ответ.
    /// </summary>
    /// <remarks>
    /// Отдаётся наружу ради подписи на экране: описание собирается по самому правилу,
    /// а переходник о нём ничего не знает.
    /// </remarks>
    public ConditionBase Condition => condition;

    /// <inheritdoc />
    /// <remarks>
    /// Пустая ссылка считается выполненной: недонастроенный слот не запирает.
    /// </remarks>
    public bool Evaluate(ConditionContextBase context)
    {
        return condition == null || condition.Evaluate(context);
    }
}
