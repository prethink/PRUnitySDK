/// <summary>
/// Уведомления об изменении свойства проекта с уже готовыми значениями нужного типа.
/// <para>
/// Интерфейс объявлен для каждого типа отдельно, а не одним generic: набор типов
/// у ProjectProperties закрыт (long, float, bool, string, DateTime), и подписаться
/// на посторонний тип нельзя. Один класс может реализовать сразу несколько интерфейсов.
/// </para>
/// <para>
/// Вместе с новым значением приходит предыдущее, чтобы подписчику не хранить копию для
/// подсчёта разницы. При первой записи свойства предыдущим будет default, и от
/// сохранённого ранее 0/false/null он неотличим: проверяйте
/// ProjectPropertiesManager.TryGet* до записи.
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
