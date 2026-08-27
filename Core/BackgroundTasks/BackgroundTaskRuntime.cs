using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Выполнение фоновой задачи: расписание, состояние, счётчики и обработка ошибок.
/// </summary>
/// <remarks>
/// Реализующая сторона моста. Владелец (<see cref="IBackgroundTask"/>) описывает,
/// что за задача и что она делает; этот класс отвечает за то, как она выполняется.
/// Благодаря такому разделению обычный класс и компонент сцены используют одну и ту же
/// механику, ничего не дублируя.
/// </remarks>
public sealed class BackgroundTaskRuntime
{
    #region Константы

    /// <summary>
    /// Сколько последних ошибок сохраняется для диагностики.
    /// </summary>
    private const int MaxStoredErrors = 10;

    #endregion

    #region Поля и свойства

    private readonly IBackgroundTask owner;
    private readonly List<Exception> errors = new();

    /// <summary>
    /// Задача, которой принадлежит это состояние.
    /// </summary>
    public IBackgroundTask Owner => owner;

    /// <summary>
    /// Время следующего запланированного запуска в выбранной шкале времени.
    /// Планируется трекером; наружу открыто только для чтения и диагностики.
    /// </summary>
    public float NextRunTime { get; internal set; }

    /// <summary>
    /// Текущее время в той шкале, по которой живёт задача.
    /// </summary>
    public float CurrentTime => owner.UseGameTime
        ? PRTime.Instance.GameTime
        : PRTime.Instance.RealTime;

    /// <summary>
    /// Текущее состояние задачи.
    /// </summary>
    public BackgroundTaskStatus Status { get; private set; } = BackgroundTaskStatus.Pending;

    /// <summary>
    /// Количество выполненных запусков, включая завершившиеся ошибкой.
    /// Пропущенные по <c>CanExecute()</c> не считаются.
    /// </summary>
    public int ExecutedCount { get; private set; }

    /// <summary>
    /// Количество пропущенных запусков.
    /// </summary>
    public int SkippedCount { get; internal set; }

    /// <summary>
    /// Момент последнего запуска по реальному времени. Значение -1 означает «запусков не было».
    /// </summary>
    public float LastRunRealTime { get; private set; } = -1f;

    /// <summary>
    /// Длительность последнего запуска в миллисекундах.
    /// </summary>
    public double LastRunDurationMs { get; private set; }

    /// <summary>
    /// Последняя возникшая ошибка либо <see langword="null"/>.
    /// </summary>
    public Exception LastError { get; private set; }

    /// <summary>
    /// Последние сохранённые ошибки, не более десяти.
    /// </summary>
    public IReadOnlyList<Exception> Errors => errors;

    /// <summary>
    /// Общее количество ошибок за всё время.
    /// </summary>
    public int ErrorCount { get; private set; }

    /// <summary>
    /// Количество ошибок подряд без единого успешного запуска.
    /// </summary>
    public int ConsecutiveErrors { get; private set; }

    /// <summary>
    /// Задача отключена и больше не выполняется.
    /// </summary>
    public bool IsStopped => Status == BackgroundTaskStatus.Faulted ||
                             Status == BackgroundTaskStatus.Completed;

    #endregion

    #region События

    /// <summary>
    /// Вызывается после каждого успешного запуска.
    /// </summary>
    public event Action Executed;

    /// <summary>
    /// Вызывается, когда запуск завершился исключением.
    /// </summary>
    public event Action<Exception> Failed;

    /// <summary>
    /// Вызывается при смене состояния: передаются предыдущее и новое.
    /// </summary>
    public event Action<BackgroundTaskStatus, BackgroundTaskStatus> StatusChanged;

    #endregion

    #region Конструктор

    /// <summary>
    /// Создаёт состояние для указанной задачи.
    /// </summary>
    /// <param name="owner">Задача-владелец.</param>
    public BackgroundTaskRuntime(IBackgroundTask owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    #endregion

    #region Методы

    /// <summary>
    /// Выполняет задачу немедленно, вне расписания.
    /// Ошибка внутри задачи не выбрасывается наружу, а попадает в <see cref="LastError"/>.
    /// </summary>
    /// <returns><see langword="true"/>, если запуск прошёл без ошибок.</returns>
    public bool Execute()
    {
        SetStatus(BackgroundTaskStatus.Executing);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            owner.ExecuteTask();

            stopwatch.Stop();
            RegisterExecution(stopwatch.Elapsed.TotalMilliseconds);
            ConsecutiveErrors = 0;
            LastError = null;

            Executed?.Invoke();
            ApplyRepeatLimit();
            return true;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            RegisterExecution(stopwatch.Elapsed.TotalMilliseconds);
            RegisterError(exception);

            PRLog.WriteError(this, $"Background task '{owner.Name}' failed. {exception}");
            Failed?.Invoke(exception);

            if (owner.MaxConsecutiveErrors > 0 && ConsecutiveErrors >= owner.MaxConsecutiveErrors)
            {
                SetStatus(BackgroundTaskStatus.Faulted);
                PRLog.WriteError(this,
                    $"Background task '{owner.Name}' disabled after {ConsecutiveErrors} consecutive errors.");
                return false;
            }

            ApplyRepeatLimit();
            return false;
        }
    }

    /// <summary>
    /// Приостанавливает задачу до вызова <see cref="Resume"/>.
    /// </summary>
    public void Pause()
    {
        if (IsStopped || Status == BackgroundTaskStatus.Paused)
            return;

        SetStatus(BackgroundTaskStatus.Paused);
    }

    /// <summary>
    /// Возобновляет приостановленную задачу и планирует следующий запуск
    /// через обычный интервал.
    /// </summary>
    /// <remarks>
    /// Расписание пересчитывается от момента возобновления, иначе задача, простоявшая
    /// на паузе дольше своего интервала, сработала бы сразу же.
    /// </remarks>
    public void Resume()
    {
        if (Status != BackgroundTaskStatus.Paused)
            return;

        NextRunTime = CurrentTime + Mathf.Max(0f, owner.RepeatSeconds);
        SetStatus(BackgroundTaskStatus.WaitingNextRun);
    }

    /// <summary>
    /// Снимает признак отказа и возвращает задачу в работу.
    /// </summary>
    public void ResetFault()
    {
        if (Status != BackgroundTaskStatus.Faulted)
            return;

        ConsecutiveErrors = 0;
        LastError = null;
        SetStatus(BackgroundTaskStatus.WaitingNextRun);
    }

    /// <summary>
    /// Сбрасывает счётчик запусков, позволяя завершённой задаче работать снова.
    /// </summary>
    public void ResetRepeatCount()
    {
        ExecutedCount = 0;

        if (Status == BackgroundTaskStatus.Completed)
            SetStatus(BackgroundTaskStatus.WaitingNextRun);
    }

    /// <summary>
    /// Меняет состояние и уведомляет подписчиков.
    /// </summary>
    internal void SetStatus(BackgroundTaskStatus status)
    {
        if (Status == status)
            return;

        BackgroundTaskStatus previous = Status;
        Status = status;
        StatusChanged?.Invoke(previous, status);
    }

    private void RegisterExecution(double durationMs)
    {
        LastRunDurationMs = durationMs;
        LastRunRealTime = PRTime.Instance.RealTime;
        ExecutedCount++;
    }

    private void RegisterError(Exception exception)
    {
        LastError = exception;
        ErrorCount++;
        ConsecutiveErrors++;

        errors.Add(exception);
        if (errors.Count > MaxStoredErrors)
            errors.RemoveAt(0);
    }

    /// <summary>
    /// Переводит задачу в завершённое состояние, если исчерпан лимит запусков.
    /// </summary>
    private void ApplyRepeatLimit()
    {
        if (owner.MaxRepeatCount > 0 && ExecutedCount >= owner.MaxRepeatCount)
            SetStatus(BackgroundTaskStatus.Completed);
    }

    #endregion
}
