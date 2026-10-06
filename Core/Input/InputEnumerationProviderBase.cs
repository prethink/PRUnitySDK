/// <summary>
/// Набор ключей ввода: действия, оси и векторы, которые источники пишут в состояние игрока.
/// </summary>
/// <remarks>
/// От обычного набора отличается одним: его значения попадают в общий список
/// <see cref="InputActionEnumerations"/>, из которого окно управления предлагает действие
/// для клавиши и кнопки на экране. Модуль, у которого свои ключи (панель быстрого доступа),
/// объявляет свой наследник; проект, которому нужны новые действия персонажа, наследуется
/// от набора персонажа — базовые ключи придут вместе с новыми.
/// </remarks>
[ReflectionContract]
public abstract class InputEnumerationProviderBase : EnumerationProviderBase
{
    /// <inheritdoc />
    public override Enumeration Default => FirstOption;

    /// <inheritdoc />
    public override bool IncludeInherited => true;
}
