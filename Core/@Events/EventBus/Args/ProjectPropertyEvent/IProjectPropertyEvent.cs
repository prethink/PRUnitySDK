using System;

/// <summary>
/// Уведомление об изменении любого свойства проекта - без типа и без значения.
/// <para>
/// Это самый простой уровень подписки: подписчик узнаёт только имя изменившегося свойства
/// и при необходимости сам читает значение через ProjectPropertiesManager нужным методом.
/// Такой подписчик не зависит от типа свойства и не выполняет приведение типов, поэтому
/// подходит, например, для автосохранения, аналитики или инвалидации кеша.
/// </para>
/// </summary>
public interface IProjectPropertyChangedEvent : IGlobalSubscriber
{
    /// <summary>
    /// Вызывается после того, как значение свойства propertyName записано (и сохранено,
    /// если запись шла с save = true).
    /// </summary>
    void OnProjectPropertyChanged(string propertyName);
}

/// <summary>
/// Уведомление об удалении свойства проекта. Тип передаётся отдельно, потому что одно
/// и то же имя может существовать в словарях разных типов - удаление затрагивает только один.
/// </summary>
public interface IProjectPropertyRemovedEvent : IGlobalSubscriber
{
    /// <summary>
    /// Вызывается после фактического удаления свойства. Если свойства с таким именем
    /// не было, событие не вызывается.
    /// </summary>
    void OnProjectPropertyRemoved(string propertyName, Type valueType);
}
