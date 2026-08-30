/// <summary>
/// Фоновая задача, не привязанная к объекту сцены.
/// Выполняется по расписанию и живёт всё время работы приложения.
/// </summary>
/// <remarks>
/// Если задаче нужны ссылки на объекты сцены или настройка в инспекторе,
/// используйте <see cref="BackgroundTaskBehaviour"/>.
/// </remarks>
public abstract class BackgroundTask : IBackgroundTask
{
    #region Расписание

    /// <summary>
    /// Уникальный ключ задачи. Используется для защиты от повторной регистрации
    /// и для поиска в реестре.
    /// </summary>
    /// <remarks>
    /// Ключи объявляются в <see cref="BackgroundTaskKeyEnumerations"/> - своей
    /// `partial`-частью рядом с модулем задачи.
    /// </remarks>
    public abstract Enumeration Key { get; }

    /// <summary>
    /// Человекочитаемое имя для логов. По умолчанию совпадает со значением ключа.
    /// </summary>
    public virtual string Name => Key?.Value ?? GetType().Name;

    /// <summary>
    /// Интервал между запусками в секундах.
    /// Значение меньше или равное нулю означает «каждый тик хоста».
    /// </summary>
    public abstract float RepeatSeconds { get; }

    /// <summary>
    /// Задержка перед первым запуском в секундах.
    /// Ноль или отрицательное значение - запуск при ближайшей возможности.
    /// </summary>
    public virtual float InitialDelaySeconds => 0f;

    /// <summary>
    /// Максимальное количество запусков, после которого задача переходит в
    /// <see cref="BackgroundTaskStatus.Completed"/> и больше не выполняется.
    /// Значение меньше единицы означает «без ограничения».
    /// </summary>
    public virtual int MaxRepeatCount => -1;

    /// <summary>
    /// Использовать игровое время вместо реального.
    /// При <see langword="true"/> задача останавливается вместе с логической паузой
    /// и замедляется вместе с игрой; при <see langword="false"/> выполняется всегда.
    /// </summary>
    public virtual bool UseGameTime => false;

    /// <summary>
    /// Зарегистрировать задачу, но не запускать её: сразу после регистрации она
    /// попадает в <see cref="BackgroundTaskStatus.Paused"/> и ждёт <see cref="Resume"/>.
    /// </summary>
    public virtual bool StartPaused => false;

    /// <summary>
    /// Сколько ошибок подряд допускается до отключения задачи.
    /// Значение меньше единицы отключает защиту.
    /// </summary>
    public virtual int MaxConsecutiveErrors => 5;

    #endregion

    #region Состояние

    /// <summary>
    /// Состояние и выполнение задачи.
    /// </summary>
    public BackgroundTaskRuntime Runtime { get; }

    /// <summary>
    /// Текущее состояние задачи.
    /// </summary>
    public BackgroundTaskStatus Status => Runtime.Status;

    /// <summary>
    /// Задача отключена и больше не выполняется.
    /// </summary>
    public bool IsStopped => Runtime.IsStopped;

    protected BackgroundTask()
    {
        Runtime = new BackgroundTaskRuntime(this);
    }

    #endregion

    #region Методы

    /// <summary>
    /// Проверяет, можно ли выполнить задачу прямо сейчас.
    /// Возврат <see langword="false"/> не является ошибкой: запуск пропускается,
    /// а проверка повторяется в следующий запланированный момент.
    /// </summary>
    public virtual bool CanExecute()
    {
        return true;
    }

    /// <summary>
    /// Выполняет задачу немедленно, вне расписания.
    /// </summary>
    /// <returns><see langword="true"/>, если запуск прошёл без ошибок.</returns>
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
