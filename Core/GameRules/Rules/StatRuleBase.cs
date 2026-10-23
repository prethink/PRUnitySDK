using System;
using UnityEngine;

/// <summary>
/// Базовое правило характеристики: ограничение, применяемое к итоговому значению
/// после всех персональных модификаторов.
/// </summary>
/// <remarks>
/// Правила хранятся в настройках проекта (<see cref="GameRulesSettings"/>) списком
/// <c>[SerializeReference]</c>, поэтому у наследника должны быть <see cref="SerializableAttribute"/>,
/// конструктор без параметров и поля с <see cref="SerializeField"/>, а не свойства.
/// </remarks>
[Serializable]
public abstract class StatRuleBase
{
    /// <summary>
    /// Приоритет правила, если он не задан.
    /// </summary>
    public const int DefaultPriority = 100;

    [SerializeField, StatKey]
    [Tooltip("Характеристика, к которой относится правило.")]
    private string stat;

    [SerializeField]
    [Tooltip("Порядок применения: меньшее значение раньше. Важен только для правил, где порядок меняет результат.")]
    private int priority = DefaultPriority;

    protected StatRuleBase()
    {
    }

    protected StatRuleBase(Enumeration stat, int priority = DefaultPriority)
    {
        this.stat = stat?.Value;
        this.priority = priority;
    }

    /// <summary>
    /// Характеристика, к которой относится правило; <see langword="null"/>, если не выбрана.
    /// </summary>
    public Enumeration Stat => Enumeration.GetOrCreate(stat);

    /// <summary>
    /// Порядок применения: меньшее значение применяется раньше,
    /// как у <c>MethodHookAttribute.Order</c>.
    /// </summary>
    /// <remarks>
    /// Правила с равным приоритетом сохраняют порядок в списке настроек.
    /// Для пары Min и Max порядок не важен, но становится значимым, как только
    /// появляется правило с некоммутативной операцией - умножение, округление, кривая.
    /// </remarks>
    public int Priority => priority;

    /// <summary>
    /// Применяет правило к значению.
    /// </summary>
    /// <param name="value">Текущее значение характеристики.</param>
    /// <returns>Значение после применения правила.</returns>
    public abstract float Apply(float value);

#if UNITY_EDITOR
    /// <summary>
    /// Имена общих полей — окно правил рисует их отдельно от полей наследника.
    /// </summary>
    public const string StatFieldName = nameof(stat);

    public const string PriorityFieldName = nameof(priority);
#endif
}
