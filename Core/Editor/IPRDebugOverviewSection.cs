/// <summary>
/// Раздел вкладки Overview окна отладки, объявленный модулем.
/// </summary>
/// <remarks>
/// Для управления, которое нужно под рукой, а не на отдельной вкладке: пройти обучение,
/// сбросить прогресс. Окно находит реализации через <c>TypeCache</c>, как и
/// <see cref="IPRDebugTab"/>, и создаёт их конструктором без параметров.
/// <para>
/// Заголовок рисует окно, раздел рисует только своё содержимое. Показывается в Play Mode,
/// вместе со всей вкладкой.
/// </para>
/// </remarks>
public interface IPRDebugOverviewSection
{
    /// <summary>
    /// Заголовок раздела.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Порядок среди разделов модулей. Встроенные разделы вкладки всегда идут первыми.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Рисует раздел.
    /// </summary>
    /// <param name="context">Состояние окна.</param>
    void Draw(PRDebugTabContext context);
}
