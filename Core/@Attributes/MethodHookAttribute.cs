using System;

[AttributeUsage(AttributeTargets.Method)]
public class MethodHookAttribute : Attribute
{
    /// <summary>
    /// Приоритет.
    /// Чем значение ниже, тем выше приоритет.
    /// </summary>
    public int Order { get; }
    public MethodHookStage MethodHookStage { get; }

    public MethodHookAttribute(MethodHookStage hookStage, int order = 0)
    {
        this.MethodHookStage = hookStage;
        this.Order = order;
    }
}

/// <summary>
/// Тип обозначающий, когда должна быть вызвана инициализация.
/// </summary>
public enum MethodHookStage
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
    /// Установки настроек по умолчанию.
    /// </summary>
    DefaultSettings,

    /// <summary>
    /// Вызов при активации Unity-объекта (Unity OnEnable).
    /// </summary>
    OnEnable,

    /// <summary>
    /// Вызов при деактивации Unity-объекта (Unity OnDisable).
    /// </summary>
    OnDisable,

    /// <summary>
    /// Пользовательский вызов в произвольный момент (ручной триггер).
    /// </summary>
    Custom
}
