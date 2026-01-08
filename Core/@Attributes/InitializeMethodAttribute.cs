using System;

[AttributeUsage(AttributeTargets.Method)]
public class InitializeMethodAttribute : Attribute
{
    /// <summary>
    /// Приоритет.
    /// Чем значение ниже, тем выше приоритет.
    /// </summary>
    public int Order { get; }
    public InitializeType InitializeType { get; }

    public InitializeMethodAttribute(InitializeType type, int order = 0)
    {
        this.InitializeType = type;
        this.Order = order;
    }
}

/// <summary>
/// Тип обозначающий, когда должна быть вызвана инициализация.
/// </summary>
public enum InitializeType
{
    /// <summary>
    /// Вызов при создании объекта через конструктор (new).
    /// </summary>
    Construct,

    /// <summary>
    /// Вызов в методе Awake Unity (до Start, сразу после активации объекта).
    /// </summary>
    Awake,

    /// <summary>
    /// Вызов при старте Unity-объекта (Unity Start).
    /// </summary>
    Start,

    /// <summary>
    /// Вызов во время биндинга зависимостей в Zenject (метод InstallBindings).
    /// </summary>
    InstallBindings,

    /// <summary>
    /// Вызов после того, как Zenject завершил конструкторный Inject (метка [Inject]).
    /// </summary>
    ZInjectConstruct,

    /// <summary>
    /// Вызов, когда проект полностью готов к работе (после загрузки всех систем).
    /// </summary>
    ReadyProject,

    /// <summary>
    /// Инициализация для SDK.  
    /// </summary>
    SDK,

    /// <summary>
    /// Пользовательский вызов в произвольный момент (ручной триггер).
    /// </summary>
    Custom
}
