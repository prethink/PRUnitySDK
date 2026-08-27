using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Реестр фоновых задач и единый цикл их выполнения.
/// Все задачи обслуживаются одним тиком <see cref="PRMonoBehaviourHost"/>,
/// поэтому их количество не влияет на число корутин.
/// </summary>
/// <remarks>
/// Трекер работает с <see cref="IBackgroundTask"/>, поэтому в одном реестре живут
/// и обычные задачи, и компоненты сцены.
/// </remarks>
public class BackgroundTaskTracker : TrackerBase<IBackgroundTask>, IPRTickable
{
    #region Поля и свойства

    /// <summary>
    /// Буфер обхода: позволяет регистрировать и снимать задачи прямо во время прохода.
    /// </summary>
    private readonly List<IBackgroundTask> runBuffer = new();

    /// <summary>
    /// Признак того, что трекер уже встал в тиковый цикл хоста.
    /// </summary>
    private bool registeredInHost;

    /// <summary>
    /// Признак того, что автоматические задачи уже были зарегистрированы.
    /// </summary>
    private bool autoTasksRegistered;

    /// <summary>
    /// Типы автоматических задач. Набор неизменен в рамках домена, поэтому
    /// сканирование сборки выполняется один раз.
    /// </summary>
    private static List<Type> autoTaskTypes;

    /// <summary>
    /// Количество выполненных проходов по списку задач.
    /// </summary>
    public long PassCount { get; private set; }

    #endregion

    #region TrackerBase

    /// <summary>
    /// Регистрирует задачу с уникальным ключом и планирует её первый запуск.
    /// </summary>
    public override bool Register(IBackgroundTask element)
    {
        if (element == null || element.Key == null)
        {
            PRLog.WriteWarning(this, "Cannot register background task without key.");
            return false;
        }

        if (elements.Contains(element) || TryGet(element.Key, out _))
        {
            PRLog.WriteWarning(this, $"Background task with key '{element.Key}' already registered.");
            return false;
        }

        elements.Add(element);
        Schedule(element, element.InitialDelaySeconds);
        element.Runtime.SetStatus(element.StartPaused
            ? BackgroundTaskStatus.Paused
            : BackgroundTaskStatus.Scheduled);
        EnsureHostRegistration();

        return true;
    }

    /// <summary>
    /// Снимает задачу с обслуживания.
    /// </summary>
    public override bool Unregister(IBackgroundTask element)
    {
        if (element == null || !elements.Remove(element))
            return false;

        element.Runtime.SetStatus(BackgroundTaskStatus.Pending);
        return true;
    }

    #endregion

    #region IPRTickable

    /// <summary>
    /// Выполняет задачи, у которых истёк интервал.
    /// </summary>
    /// <remarks>
    /// До завершения инициализации SDK запуски не выполняются: задачи обычно обращаются
    /// к менеджерам и сервисам, которых ещё нет. Проверка идёт по состоянию SDK,
    /// а не по разовому событию, поэтому порядок создания трекера значения не имеет.
    /// </remarks>
    public void PRTick()
    {
        if (!PRUnitySDK.IsInitialized || elements.Count == 0)
            return;

        PassCount++;

        runBuffer.Clear();
        runBuffer.AddRange(elements);

        foreach (IBackgroundTask task in runBuffer)
            TryExecute(task);
    }

    /// <summary>
    /// Выполняет одну задачу, если наступило её время и она к этому готова.
    /// </summary>
    private void TryExecute(IBackgroundTask task)
    {
        // Компонент мог быть уничтожен между проходами, поэтому проверка идёт
        // через IsNull(): для UnityEngine.Object обычное сравнение с null не годится.
        if (task == null || task.IsNull())
            return;

        BackgroundTaskRuntime runtime = task.Runtime;
        if (runtime.IsStopped || runtime.Status == BackgroundTaskStatus.Paused)
            return;

        if (runtime.CurrentTime < runtime.NextRunTime)
            return;

        // Пропуск по CanExecute не считается ошибкой и не тратит лимит запусков:
        // задача просто ждёт следующего окна.
        if (!SafeCanExecute(task))
        {
            runtime.SkippedCount++;
            runtime.SetStatus(BackgroundTaskStatus.Skipped);
            Schedule(task, task.RepeatSeconds);
            return;
        }

        runtime.Execute();
        Schedule(task, task.RepeatSeconds);

        if (!runtime.IsStopped)
            runtime.SetStatus(BackgroundTaskStatus.WaitingNextRun);
    }

    /// <summary>
    /// Вызывает <c>CanExecute()</c> так, чтобы исключение в проверке не роняло весь проход.
    /// </summary>
    private static bool SafeCanExecute(IBackgroundTask task)
    {
        try
        {
            return task.CanExecute();
        }
        catch (Exception exception)
        {
            PRLog.WriteError(task, $"Background task '{task.Name}' CanExecute failed. {exception}");
            return false;
        }
    }

    #endregion

    #region Автоматическая регистрация

    /// <summary>
    /// Находит и регистрирует все задачи, помеченные <see cref="AutoBackgroundTaskAttribute"/>.
    /// Вызывается из <c>PRUnitySDK.InitializeSDK</c>; повторные вызовы игнорируются.
    /// </summary>
    /// <returns>Количество зарегистрированных задач.</returns>
    public int RegisterAutoTasks()
    {
        if (autoTasksRegistered)
        {
            PRLog.WriteWarning(this, "Auto background tasks already registered.");
            return 0;
        }

        autoTasksRegistered = true;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        autoTaskTypes ??= FindAutoTaskTypes();

        int registered = 0;
        foreach (Type taskType in autoTaskTypes)
        {
            // Создание изолировано: ошибка в конструкторе одной задачи не должна
            // прерывать регистрацию остальных и тем более ронять инициализацию SDK.
            IBackgroundTask task;
            try
            {
                task = (IBackgroundTask)Activator.CreateInstance(taskType);
            }
            catch (Exception exception)
            {
                PRLog.WriteError(this,
                    $"Cannot create background task <color={Color.yellow}>{taskType.Name}</color>. {exception}");
                continue;
            }

            if (Register(task))
                registered++;
        }

        stopwatch.Stop();
        PRLog.WriteDebug(this,
            $"Registered {registered} auto background tasks in {stopwatch.Elapsed.TotalMilliseconds:F2} ms.");

        return registered;
    }

    /// <summary>
    /// Собирает типы задач, помеченные атрибутом и пригодные к созданию.
    /// </summary>
    /// <remarks>
    /// Сканируется только сборка, в которой объявлен <see cref="IBackgroundTask"/>:
    /// обход всех сборок домена стоил бы заметного времени на старте, а задачи
    /// проекта всё равно живут здесь.
    /// <para>
    /// Компоненты сцены исключены: их создаёт Unity вместе с объектом, и регистрируются
    /// они сами при включении.
    /// </para>
    /// </remarks>
    private List<Type> FindAutoTaskTypes()
    {
        var result = new List<(Type Type, int Order)>();

        Type[] assemblyTypes;
        try
        {
            assemblyTypes = typeof(IBackgroundTask).Assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            PRLog.WriteError(this, $"Cannot scan assembly for background tasks. {exception}");
            assemblyTypes = exception.Types.Where(type => type != null).ToArray();
        }

        foreach (Type type in assemblyTypes)
        {
            var attribute = type.GetCustomAttribute<AutoBackgroundTaskAttribute>(inherit: false);
            if (attribute == null || !attribute.Enabled)
                continue;

            if (!typeof(IBackgroundTask).IsAssignableFrom(type))
            {
                PRLog.WriteWarning(this,
                    $"Type {type.Name} is marked as auto background task but does not implement {nameof(IBackgroundTask)}.");
                continue;
            }

            if (typeof(Component).IsAssignableFrom(type))
            {
                PRLog.WriteWarning(this,
                    $"Background task {type.Name} is a component and registers itself on enable; the attribute is ignored.");
                continue;
            }

            if (type.IsAbstract || type.ContainsGenericParameters)
            {
                PRLog.WriteWarning(this,
                    $"Background task {type.Name} cannot be created automatically: type is abstract or generic.");
                continue;
            }

            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                PRLog.WriteWarning(this,
                    $"Background task {type.Name} has no public parameterless constructor and must be registered manually.");
                continue;
            }

            result.Add((type, attribute.Order));
        }

        result.Sort((left, right) => left.Order != right.Order
            ? left.Order.CompareTo(right.Order)
            : string.CompareOrdinal(left.Type.Name, right.Type.Name));

        return result.Select(entry => entry.Type).ToList();
    }

    #endregion

    #region Методы

    /// <summary>
    /// Ищет задачу по ключу.
    /// </summary>
    public bool TryGet(Enumeration key, out IBackgroundTask task)
    {
        task = null;
        if (key == null)
            return false;

        foreach (IBackgroundTask element in elements)
        {
            if (element != null && element.Key == key)
            {
                task = element;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Ищет задачу по ключу и приводит её к нужному типу.
    /// </summary>
    public bool TryGet<T>(Enumeration key, out T task) where T : class, IBackgroundTask
    {
        task = null;

        if (TryGet(key, out IBackgroundTask found) && found is T typed)
        {
            task = typed;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Выполняет указанную задачу немедленно и переносит расписание.
    /// Проверка <c>CanExecute()</c> при этом не выполняется.
    /// </summary>
    /// <returns><see langword="true"/>, если задача найдена и запуск прошёл без ошибок.</returns>
    public bool ForceExecute(Enumeration key)
    {
        if (!TryGet(key, out IBackgroundTask task))
            return false;

        bool result = task.Runtime.Execute();
        Schedule(task, task.RepeatSeconds);

        if (!task.Runtime.IsStopped)
            task.Runtime.SetStatus(BackgroundTaskStatus.WaitingNextRun);

        return result;
    }

    /// <summary>
    /// Возвращает задачи в указанном состоянии.
    /// </summary>
    public List<IBackgroundTask> GetByStatus(BackgroundTaskStatus status)
    {
        var result = new List<IBackgroundTask>();

        foreach (IBackgroundTask element in elements)
        {
            if (element != null && element.Runtime.Status == status)
                result.Add(element);
        }

        return result;
    }

    /// <summary>
    /// Возвращает задачи, отключённые из-за череды ошибок.
    /// </summary>
    public List<IBackgroundTask> GetFaulted()
    {
        return GetByStatus(BackgroundTaskStatus.Faulted);
    }

    /// <summary>
    /// Планирует следующий запуск через указанный интервал от текущего момента.
    /// </summary>
    /// <remarks>
    /// Отсчёт ведётся от фактического выполнения, а не от расписания, поэтому
    /// пропущенные интервалы не копятся и не вызывают серию догоняющих запусков.
    /// </remarks>
    private void Schedule(IBackgroundTask task, float interval)
    {
        task.Runtime.NextRunTime = task.Runtime.CurrentTime + Mathf.Max(0f, interval);
    }

    /// <summary>
    /// Ставит трекер в тиковый цикл хоста при первой регистрации.
    /// </summary>
    private void EnsureHostRegistration()
    {
        if (registeredInHost)
            return;

        PRMonoBehaviourHost.Instance.Register(this);
        registeredInHost = true;
    }

    #endregion
}
