/// <summary>
/// Уведомления об изменении свойства проекта с уже готовыми значениями нужного типа.
/// <para>
/// Интерфейсы намеренно объявлены отдельно для каждого поддерживаемого типа, а не одним
/// generic-интерфейсом: набор типов у ProjectProperties закрыт (long, float, bool, string,
/// DateTime), и отдельные интерфейсы не позволяют подписаться на тип, которого в хранилище
/// быть не может. Подписчик получает значения напрямую, без приведения типов и без боксинга
/// на своей стороне.
/// </para>
/// <para>
/// Вместе с новым значением передаётся предыдущее - оно всё равно читается менеджером перед
/// записью, поэтому подписчику не нужно хранить копию, чтобы посчитать разницу (насколько
/// выросли монеты, в какую сторону переключили флаг и т.п.). Если свойство сохраняется
/// впервые, предыдущим значением будет default: этот случай неотличим от сохранённого
/// ранее 0/false/null, и при необходимости его нужно проверять через
/// ProjectPropertiesManager.TryGet* до записи.
/// </para>
/// <para>
/// Один класс может реализовать сразу несколько таких интерфейсов - сигнатуры методов
/// различаются, конфликта реализаций не возникает.
/// </para>
/// </summary>
public interface ILongProjectPropertyChangedEvent : IGlobalSubscriber
{
    /// <summary>Вызывается при изменении long-свойства.</summary>
    void OnLongProjectPropertyChanged(string propertyName, long previousValue, long currentValue);
}

/// <inheritdoc cref="ILongProjectPropertyChangedEvent"/>
public interface IFloatProjectPropertyChangedEvent : IGlobalSubscriber
{
    /// <summary>Вызывается при изменении float-свойства.</summary>
    void OnFloatProjectPropertyChanged(string propertyName, float previousValue, float currentValue);
}

/// <inheritdoc cref="ILongProjectPropertyChangedEvent"/>
public interface IBoolProjectPropertyChangedEvent : IGlobalSubscriber
{
    /// <summary>Вызывается при изменении bool-свойства.</summary>
    void OnBoolProjectPropertyChanged(string propertyName, bool previousValue, bool currentValue);
}

/// <inheritdoc cref="ILongProjectPropertyChangedEvent"/>
public interface IStringProjectPropertyChangedEvent : IGlobalSubscriber
{
    /// <summary>Вызывается при изменении string-свойства.</summary>
    void OnStringProjectPropertyChanged(string propertyName, string previousValue, string currentValue);
}

/// <inheritdoc cref="ILongProjectPropertyChangedEvent"/>
public interface IDateTimeProjectPropertyChangedEvent : IGlobalSubscriber
{
    /// <summary>Вызывается при изменении DateTime-свойства.</summary>
    void OnDateTimeProjectPropertyChanged(string propertyName, System.DateTime previousValue, System.DateTime currentValue);
}
