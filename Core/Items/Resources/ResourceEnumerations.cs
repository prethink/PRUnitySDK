public partial class ResourceEnumerations : EnumerationProviderBase
{
    public static Enumeration Coin          = new Enumeration(nameof(Coin));
    public static Enumeration Crystal       = new Enumeration(nameof(Crystal));
    /// <inheritdoc />
    /// <remarks>
    /// Первое значение, как и у остальных наборов. Пустая ссылка отдавала бы <c>null</c>,
    /// и объект с нетронутым полем ничего бы не делал, хотя в инспекторе значение
    /// показано.
    /// </remarks>
    public override Enumeration Default => FirstOption;

    public override bool IncludeInherited => true;
}
