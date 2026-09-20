/// <summary>
/// Имена правил, которые игра объявляет кодом.
/// </summary>
/// <remarks>
/// Пустой набор: своих правил у SDK нет, их заводит игра partial-частью этого класса —
/// как и остальные наборы <c>Enumeration</c>. Сюда попадает то, у чего нет настроек:
/// «туториал пройден», «идёт бой с боссом», «первый запуск». Заводить под такое класс
/// с нулём полей незачем.
/// <para>
/// Правило с числом — «сто кубков» — сюда не относится: у него есть, что настраивать,
/// и оно должно остаться в инспекторе (<see cref="ResourceCondition"/>), а не уехать
/// в код.
/// </para>
/// <para>
/// Набор, а не голая строка, ради выпадашки в инспекторе: строку опечатают, и ассет
/// молча перестанет находить правило.
/// </para>
/// </remarks>
public partial class ConditionEnumerations : EnumerationProviderBase
{
    /// <inheritdoc />
    public override Enumeration Default => null;

    /// <inheritdoc />
    public override bool IncludeInherited => true;
}
