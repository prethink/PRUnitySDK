using System;

/// <summary>
/// Контракт, реализации которого SDK находит сам: интерфейс, базовый класс или атрибут-метка.
/// </summary>
/// <remarks>
/// По этой метке генератор списка типов узнаёт, чьи классы перечислять при сборке билда
/// (<see cref="ReflectionTypeRegistry"/>). Контракт без метки тоже работает, но его классы игра ищет
/// при запуске перебором всей сборки — это полторы секунды на слабом телефоне.
/// <para>
/// Метка, а не перечень в коде генератора: контракт может лежать в проекте, о котором ядро не знает.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public sealed class ReflectionContractAttribute : Attribute
{
}
