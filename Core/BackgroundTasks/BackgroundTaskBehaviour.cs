using UnityEngine;

/// <summary>
/// Фоновая задача, живущая на объекте сцены.
/// Нужна, когда задаче требуются ссылки на компоненты или настройка в инспекторе -
/// то, чего обычный <see cref="BackgroundTask"/> дать не может.
/// </summary>
/// <remarks>
/// Компонент регистрируется в реестре при включении и снимается при выключении,
/// поэтому выключенный объект задачу не выполняет. Вся механика расписания и
/// диагностики та же самая: она живёт в <see cref="BackgroundTaskRuntime"/>.
/// </remarks>
public abstract class BackgroundTaskBehaviour : PRMonoBehaviour, IBackgroundTask
{
    #region Расписание

    [Header("Расписание задачи")]
    [SerializeField, Min(0f)]
    [Tooltip("Интервал между запусками в секундах. 0 - каждый тик хоста.")]
    private float repeatSeconds = 60f;

    [SerializeField, Min(0f)]
    [Tooltip("Задержка перед первым запуском в секундах.")]
    private float initialDelaySeconds;

    [SerializeField]
    [Tooltip("Максимальное количество запусков. Меньше 1 - без ограничения.")]
    private int maxRepeatCount = -1;

    [SerializeField]
    [Tooltip("Считать по игровому времени: задача встанет вместе с логической паузой.")]
    private bool useGameTime;

    [SerializeField]
    [Tooltip("Зарегистрировать, но не запускать до вызова Resume().")]
    private bool startPaused;

    [SerializeField, Min(0)]
    [Tooltip("Сколько ошибок подряд допускается до отключения задачи. 0 - без защиты.")]
    private int maxConsecutiveErrors = 5;

    /// <inheritdoc />
    public abstract Enumeration Key { get; }

    /// <inheritdoc />
    public virtual string Name => Key?.Value ?? GetType().Name;

    /// <inheritdoc />
    public virtual float RepeatSeconds => repeatSeconds;

    /// <inheritdoc />
    public virtual float InitialDelaySeconds => initialDelaySeconds;

    /// <inheritdoc />
    public virtual int MaxRepeatCount => maxRepeatCount;

    /// <inheritdoc />
    public virtual bool UseGameTime => useGameTime;

    /// <inheritdoc />
    public virtual bool StartPaused => startPaused;

    /// <inheritdoc />
    public virtual int MaxConsecutiveErrors => maxConsecutiveErrors;

    #endregion

    #region Состояние

    private BackgroundTaskRuntime runtime;

    /// <summary>
    /// Состояние и выполнение задачи.
    /// </summary>
    /// <remarks>
    /// Создаётся лениво: обращение к задаче возможно раньше, чем Unity вызовет Awake.
    /// </remarks>
    public BackgroundTaskRuntime Runtime => runtime ??= new BackgroundTaskRuntime(this);

    /// <summary>
    /// Текущее состояние задачи.
    /// </summary>
    public BackgroundTaskStatus Status => Runtime.Status;

    /// <summary>
    /// Задача отключена и больше не выполняется.
    /// </summary>
    public bool IsStopped => Runtime.IsStopped;

    #endregion

    #region MonoBehaviour

    protected override void OnEnable()
    {
        base.OnEnable();
        PRUnitySDK.Trackers.BackgroundTasks.Register(this);
    }

    protected override void OnDisable()
    {
        PRUnitySDK.Trackers.BackgroundTasks.Unregister(this);
        base.OnDisable();
    }

    #endregion

    #region Методы

    /// <inheritdoc />
    public virtual bool CanExecute()
    {
        return true;
    }

    /// <summary>
    /// Выполняет задачу немедленно, вне расписания.
    /// </summary>
    public bool Execute()
    {
        return Runtime.Execute();
    }

    /// <summary>
    /// Приостанавливает задачу до вызова <see cref="Resume"/>.
    /// </summary>
    public void Pause()
    {
        Runtime.Pause();
    }

    /// <summary>
    /// Возобновляет приостановленную задачу.
    /// </summary>
    public void Resume()
    {
        Runtime.Resume();
    }

    /// <summary>
    /// Снимает признак отказа и возвращает задачу в работу.
    /// </summary>
    public void ResetFault()
    {
        Runtime.ResetFault();
    }

    /// <summary>
    /// Сбрасывает счётчик запусков, позволяя завершённой задаче работать снова.
    /// </summary>
    public void ResetRepeatCount()
    {
        Runtime.ResetRepeatCount();
    }

    /// <summary>
    /// Тело задачи. Вызывается трекером по расписанию.
    /// </summary>
    protected abstract void OnExecute();

    /// <inheritdoc />
    void IBackgroundTask.ExecuteTask()
    {
        OnExecute();
    }

    #endregion
}
