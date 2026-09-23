/// <summary>
/// Контекст без обстоятельств: условие спрашивают о состоянии мира.
/// </summary>
/// <remarks>
/// Один общий экземпляр: данных в нём нет, а <c>Evaluate</c> зовут на каждый кадр —
/// заводить объект на каждый вызов незачем.
/// </remarks>
public sealed class ConditionContextEmpty : ConditionContextBase
{
    /// <summary>
    /// Общий экземпляр.
    /// </summary>
    public static readonly ConditionContextEmpty Instance = new();

    private ConditionContextEmpty()
    {
    }
}
