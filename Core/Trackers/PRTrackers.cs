using System.Collections.Generic;

/// <summary>
/// Предоставляет единый доступ к основным трекерам SDK.
/// </summary>
public partial class PRTrackers
{
    /// <summary>
    /// Фоновые задачи, выполняемые по расписанию.
    /// </summary>
    public BackgroundTaskTracker BackgroundTasks => BackgroundTaskService.Instance;

    /// <summary>
    /// Игроки текущей сессии.
    /// </summary>
    public PlayerTracker Players => PlayerService.Instance;

    /// <summary>
    /// Все зарегистрированные игровые сущности.
    /// </summary>
    public EntityTracker Entities => EntityService.Instance;

    /// <summary>
    /// Зарегистрированные UI-окна.
    /// </summary>
    public MonoWindowsTracker MonoWindows => MonoWindowsService.Instance;
    /// <summary>
    /// Зарегистрированные UI-уведомители.
    /// </summary>
    public NotifierTracker Notifiers => NotifierService.Instance;

    /// <summary>
    /// Стек камер и игровые камеры.
    /// </summary>
    public CameraTracker CameraTracker => CameraTracker.Instance;

    /// <summary>
    /// Объекты, участвующие в сохранении состояния.
    /// </summary>
    public HashSet<ISaveable> Saveables = new HashSet<ISaveable>();
}
