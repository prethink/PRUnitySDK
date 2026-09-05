using System;

/// <summary>
/// Помечает фоновую задачу для автоматической регистрации при инициализации SDK.
/// Помеченный класс создаётся и ставится в <see cref="BackgroundTaskTracker"/> сам,
/// вручную вызывать <c>Register</c> не нужно.
/// </summary>
/// <remarks>
/// Класс должен наследовать <see cref="BackgroundTask"/>, не быть абстрактным и иметь
/// публичный конструктор без параметров. Задачи с параметрами конструктора или ссылкой
/// на объект сцены регистрируйте вручную.
/// <para>
/// Атрибут не наследуется: иначе базовая задача и её наследник попали бы в реестр оба.
/// <c>[Preserve]</c> ставить не нужно, помеченные типы попадают в <c>link.xml</c> сами.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AutoBackgroundTaskAttribute : Attribute
{
    /// <summary>
    /// Порядок регистрации: меньшее значение регистрируется раньше.
    /// Влияет только на порядок обхода в трекере, но не на расписание.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Позволяет временно отключить автоматическую регистрацию, не убирая атрибут
    /// и не удаляя класс.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="order">Порядок регистрации.</param>
    public AutoBackgroundTaskAttribute(int order = 0)
    {
        Order = order;
    }
}
