using System.Collections.Generic;

/// <summary>
/// Базовый класс работы с метриками.
/// </summary>
public abstract class MetricBase 
{
    /// <summary>
    /// Отправить метрику.
    /// </summary>
    /// <param name="eventName">Название события.</param>
    public abstract void Send(string eventName);

    /// <summary>
    /// Отправить метрику.
    /// </summary>
    /// <param name="rootKeyEvent">Корневое название события.</param>
    /// <param name="subKeyEvent">Дочерний ключ события.</param>
    /// <param name="subValueEvent">Дочернее значение события.</param>
    public abstract void Send(string rootKeyEvent, string subKeyEvent, string subValueEvent);

    /// <summary>
    /// Отправить метрику.
    /// </summary>
    /// <param name="eventName">Название события.</param>
    /// <param name="eventParams">Параметры события.</param>
    public abstract void Send(string eventName, Dictionary<string, string> eventParams);
}
