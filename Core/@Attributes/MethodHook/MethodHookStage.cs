/// <summary>
/// Тип, обозначающий момент/стадию, когда должна быть вызвана инициализация или хук.
/// </summary>
public enum MethodHookStage
{
    /// <summary>
    /// Вызов сразу после конструктора объекта (new).
    /// </summary>
    Construct,

    /// <summary>
    /// Перед методом Awake Unity (можно использовать для подготовки данных).
    /// </summary>
    PreAwake,

    /// <summary>
    /// После метода Awake Unity, до Start.
    /// </summary>
    PostAwake,

    /// <summary>
    /// Перед методом Start Unity.
    /// </summary>
    PreStart,

    /// <summary>
    /// После метода Start Unity.
    /// </summary>
    PostStart,

    /// <summary>
    /// Перед сохранением данных (например, сериализация или сохранение состояния).
    /// </summary>
    PreSave,

    /// <summary>
    /// Во время процесса сохранения данных.
    /// </summary>
    Saving,

    /// <summary>
    /// После завершения сохранения данных.
    /// </summary>
    PostSave,

    /// <summary>
    /// Перед методом OnEnable Unity.
    /// </summary>
    PreOnEnable,

    /// <summary>
    /// После метода OnEnable Unity.
    /// </summary>
    PostOnEnable,

    /// <summary>
    /// Перед методом OnDisable Unity.
    /// </summary>
    PreOnDisable,

    /// <summary>
    /// После метода OnDisable Unity.
    /// </summary>
    PostOnDisable,

    /// <summary>
    /// Перед клонированием объекта.
    /// </summary>
    PreClone,

    /// <summary>
    /// Во время процесса клонирования объекта.
    /// </summary>
    Cloning,

    /// <summary>
    /// После завершения клонирования объекта.
    /// </summary>
    PostClone,

    /// <summary>
    /// Перед основной инициализацией (SDK/системной).
    /// </summary>
    PreInitialize,

    /// <summary>
    /// Во время основной инициализации объекта.
    /// </summary>
    Initializing, // исправлено с "Initialing"

    /// <summary>
    /// После основной инициализации объекта.
    /// </summary>
    PostInitialize,

    /// <summary>
    /// Перед произвольной операцией или хук-логикой.
    /// </summary>
    PreOperation, // исправлено с "Pre"

    /// <summary>
    /// После произвольной операции или хук-логики.
    /// </summary>
    PostOperation, // исправлено с "Post"

    /// <summary>
    /// Пауза / временная остановка выполнения логики.
    /// </summary>
    Pause,

    /// <summary>
    /// Метод Awake Unity (вызывается при активации объекта).
    /// </summary>
    Awake,

    /// <summary>
    /// Метод Start Unity (вызывается после Awake на активном объекте).
    /// </summary>
    Start,

    /// <summary>
    /// В момент биндинга зависимостей Zenject (InstallBindings).
    /// </summary>
    InstallBindings,

    /// <summary>
    /// После того как Zenject завершил конструкторный Inject ([Inject]).
    /// </summary>
    ZenjectConstruct, // исправлено с "ZInjectConstruct"

    /// <summary>
    /// После полной загрузки всех систем проекта, когда проект готов к работе.
    /// </summary>
    ReadyProject,

    /// <summary>
    /// Инициализация SDK.
    /// </summary>
    SDK,

    /// <summary>
    /// Инициализация конвертеров.
    /// </summary>
    Converter,

    /// <summary>
    /// Установка настроек по умолчанию.
    /// </summary>
    DefaultSettings,

    /// <summary>
    /// Создание коллекций.
    /// </summary>
    CreateCollections,

    /// <summary>
    /// Пользовательский вызов в произвольный момент (ручной триггер).
    /// </summary>
    Custom
}
