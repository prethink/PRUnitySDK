/// <summary>
/// Базовое правило характеристики: ограничение, применяемое к итоговому значению
/// после всех персональных модификаторов.
/// </summary>
public abstract class StatRuleBase
{
    protected StatRuleBase(Enumeration stat, int priority = 100)
    {
        Stat = stat;
        Priority = priority;
    }

    /// <summary>
    /// Характеристика, к которой относится правило.
    /// </summary>
    public Enumeration Stat { get; protected set; }

    /// <summary>
    /// Порядок применения: меньшее значение применяется раньше,
    /// как у <c>MethodHookAttribute.Order</c>.
    /// </summary>
    /// <remarks>
    /// Правила с равным приоритетом сохраняют порядок объявления в провайдере.
    /// Для пары Min и Max порядок не важен, но становится значимым, как только
    /// появляется правило с некоммутативной операцией - умножение, округление, кривая.
    /// </remarks>
    public int Priority { get; protected set; }

    /// <summary>
    /// Применяет правило к значению.
    /// </summary>
    /// <param name="value">Текущее значение характеристики.</param>
    /// <returns>Значение после применения правила.</returns>
    public abstract float Apply(float value);
}
