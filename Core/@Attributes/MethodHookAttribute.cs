using System;

[AttributeUsage(AttributeTargets.Method)]
public class MethodHookAttribute : Attribute
{
    /// <summary>
    /// Приоритет.
    /// Чем значение ниже, тем выше приоритет.
    /// </summary>
    public int Order { get; }
    public string MethodHookStage { get; }

    public MethodHookAttribute(MethodHookStage hookStage, int order = 0) 
        : this(hookStage.ToString(), order) { }

    public MethodHookAttribute(string hookStage, int order = 0)
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
    /// 
    /// </summary>
    PreAwake,

    /// <summary>
    /// 
    /// </summary>
    PostAwake,

    /// <summary>
    /// 
    /// </summary>
    PreStart,

    /// <summary>
    /// 
    /// </summary>
    PostStart,

    /// <summary>
    /// 
    /// </summary>
    PreSave,

    /// <summary>
    /// 
    /// </summary>
    Saving,

    /// <summary>
    /// 
    /// </summary>
    PostSave,

    /// <summary>
    /// 
    /// </summary>
    PreOnEnable,

    /// <summary>
    /// 
    /// </summary>
    PostOnEnable,

    /// <summary>
    /// 
    /// </summary>
    PreOnDisable,


    /// <summary>
    /// 
    /// </summary>
    PostOnDisable,

    /// <summary>
    /// 
    /// </summary>
    Pre,

    /// <summary>
    /// 
    /// </summary>
    Post,

    /// <summary>
    /// 
    /// </summary>
    Pause,

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
    /// Пользовательский вызов в произвольный момент (ручной триггер).
    /// </summary>
    Custom
}
