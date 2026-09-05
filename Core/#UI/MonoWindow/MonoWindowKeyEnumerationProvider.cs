public partial class MonoWindowKeyEnumerations : EnumerationProviderBase
{

    /// <inheritdoc />
    /// <remarks>
    /// Первое значение, как и у остальных наборов. Пустая ссылка отдавала бы <c>null</c>,
    /// и объект, у которого поле не тронули, молча ничего не делал бы — при том что
    /// в инспекторе значение показано. Видимое и выполняемое должны совпадать.
    /// </remarks>
    public override Enumeration Default => FirstOption;

    public override bool IncludeInherited => true;
}
