using System;

/// <summary>
/// События произвольных свойств проекта (ProjectData.ProjectProperties).
/// Рассылку выполняет ProjectPropertiesManager при записи и удалении свойств.
/// </summary>
public static class ProjectPropertyEvents
{
    /// <summary>
    /// Рассылает уведомление об изменении свойства: сначала типизированным подписчикам
    /// (с предыдущим и новым значением), затем общим (только имя свойства). Порядок именно
    /// такой, чтобы обработчик, которому нужны значения, отработал до тех, кто просто
    /// реагирует на факт изменения.
    /// </summary>
    public static void RaiseChanged<T>(string propertyName, T previousValue, T currentValue)
    {
        RaiseTypedChanged(propertyName, previousValue, currentValue);

        EventBus.RaiseEvent<IProjectPropertyChangedEvent>(invoke => invoke.OnProjectPropertyChanged(propertyName));
    }

    /// <summary>
    /// Рассылает уведомление об удалении свойства.
    /// </summary>
    public static void RaiseRemoved(string propertyName, Type valueType)
    {
        EventBus.RaiseEvent<IProjectPropertyRemovedEvent>(invoke => invoke.OnProjectPropertyRemoved(propertyName, valueType));
    }

    /// <summary>
    /// Выбирает интерфейс подписчиков по типу значения. Ветвление по typeof(T) - тот же
    /// приём, что и в ProjectPropertiesManager.GetProperties&lt;T&gt;(): T известен статически
    /// в точке вызова, поэтому ветка всегда одна и та же. Неподдерживаемый тип не является
    /// ошибкой - типизированного события для него просто нет, общее событие всё равно уйдёт.
    /// </summary>
    private static void RaiseTypedChanged<T>(string propertyName, T previousValue, T currentValue)
    {
        if (typeof(T) == typeof(long))
        {
            var previous = (long)(object)previousValue;
            var current = (long)(object)currentValue;
            EventBus.RaiseEvent<ILongProjectPropertyChangedEvent>(invoke => invoke.OnLongProjectPropertyChanged(propertyName, previous, current));
            return;
        }

        if (typeof(T) == typeof(float))
        {
            var previous = (float)(object)previousValue;
            var current = (float)(object)currentValue;
            EventBus.RaiseEvent<IFloatProjectPropertyChangedEvent>(invoke => invoke.OnFloatProjectPropertyChanged(propertyName, previous, current));
            return;
        }

        if (typeof(T) == typeof(bool))
        {
            var previous = (bool)(object)previousValue;
            var current = (bool)(object)currentValue;
            EventBus.RaiseEvent<IBoolProjectPropertyChangedEvent>(invoke => invoke.OnBoolProjectPropertyChanged(propertyName, previous, current));
            return;
        }

        if (typeof(T) == typeof(string))
        {
            var previous = (string)(object)previousValue;
            var current = (string)(object)currentValue;
            EventBus.RaiseEvent<IStringProjectPropertyChangedEvent>(invoke => invoke.OnStringProjectPropertyChanged(propertyName, previous, current));
            return;
        }

        if (typeof(T) == typeof(DateTime))
        {
            var previous = (DateTime)(object)previousValue;
            var current = (DateTime)(object)currentValue;
            EventBus.RaiseEvent<IDateTimeProjectPropertyChangedEvent>(invoke => invoke.OnDateTimeProjectPropertyChanged(propertyName, previous, current));
        }
    }
}
