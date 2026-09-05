using System;

/// <summary>
/// Помечает систему, которая сама сообщает реестру, какие предметы она выдаёт.
/// </summary>
/// <remarks>
/// Помеченный класс создаётся и регистрируется при инициализации SDK, вызывать
/// <c>Register</c> вручную не нужно. Он должен реализовывать
/// <see cref="IReservedItemsProvider"/>, не быть абстрактным и иметь публичный
/// конструктор без параметров. Провайдер с аргументами или ссылкой на объект сцены
/// регистрируется вручную.
/// <para>
/// Атрибут не наследуется: иначе базовый провайдер и его наследник дали бы два
/// одинаковых ответа. <c>[Preserve]</c> ставить не нужно, помеченные типы попадают
/// в <c>link.xml</c> сами.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AutoReservedItemsProviderAttribute : Attribute
{
    /// <summary>
    /// Порядок регистрации: меньшее значение регистрируется раньше.
    /// </summary>
    /// <remarks>
    /// Важен только при совпадении предметов у двух систем: источником покажется та,
    /// что записалась позже.
    /// </remarks>
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
    public AutoReservedItemsProviderAttribute(int order = 0)
    {
        Order = order;
    }
}
