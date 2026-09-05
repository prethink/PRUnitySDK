public partial class MonoWindowKeyEnumerations : EnumerationProviderBase
{

    /// <inheritdoc />
    /// <remarks>
    /// Первое значение, как и у остальных наборов. Пустая ссылка отдавала бы <c>null</c>,
    /// и объект с нетронутым полем ничего бы не делал, хотя в инспекторе значение
    /// показано.
    /// </remarks>
    public override Enumeration Default => FirstOption;

    public override bool IncludeInherited => true;
}
