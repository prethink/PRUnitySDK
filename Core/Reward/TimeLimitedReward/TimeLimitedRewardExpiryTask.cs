/// <summary>
/// Раз в секунду снимает истёкшие временные награды через
/// <see cref="TimeLimitedRewardService.RemoveExpired"/>, и подписчики
/// <see cref="ITimeLimitedRewardExpiredEvent"/> узнают об окончании сами.
/// </summary>
/// <remarks>
/// Начинает работать после загрузки сохранения. Первый запуск снимает и то,
/// что истекло, пока игра была закрыта.
/// </remarks>
[AutoBackgroundTask]
public class TimeLimitedRewardExpiryTask : BackgroundTask
{
    #region Константы

    /// <summary>
    /// Интервал проверки в секундах реального времени.
    /// </summary>
    /// <remarks>
    /// Сроки наград показываются с точностью до секунды. Проверка перебирает несколько
    /// записей и пишет на диск только когда что-то снято, так что чаще проверять можно,
    /// но заметно это не будет.
    /// </remarks>
    public const float CheckIntervalSeconds = 1f;

    #endregion

    #region Поля и свойства

    /// <summary>
    /// Сохранение загружено. До этого <see cref="GameManager.GetProjectData"/> бросает исключение.
    /// </summary>
    private bool isDataReady;

    /// <inheritdoc />
    public override Enumeration Key => BackgroundTaskKeyEnumerations.TimeLimitedRewardExpiry;

    /// <inheritdoc />
    public override string Name => "Окончание временных наград";

    /// <inheritdoc />
    public override float RepeatSeconds => CheckIntervalSeconds;

    /// <summary>
    /// Работает и на логической паузе: срок идёт по серверным часам и на паузе тоже кончается.
    /// </summary>
    public override bool UseGameTime => false;

    #endregion

    #region Конструктор

    /// <summary>
    /// Трекер создаёт задачу после менеджеров, поэтому <see cref="GameManager"/> уже есть.
    /// </summary>
    public TimeLimitedRewardExpiryTask()
    {
        if (!GameManager.HasInstance)
        {
            PRLog.WriteWarning(this, "GameManager не создан: истёкшие награды снимать некому.");
            return;
        }

        // Здесь только флаг, запуск будет на ближайшем тике трекера. Если снять награды
        // прямо в обработчике, сохранение начнётся посреди загрузки, когда часть
        // подписчиков сигнала ещё не восстановила своё состояние.
        GameManager.Instance.ReadySignal.SubscribeOnReady(() => isDataReady = true);
    }

    #endregion

    #region BackgroundTask

    /// <inheritdoc />
    public override bool CanExecute()
    {
        return isDataReady;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Если снимать нечего, <c>RemoveExpired</c> выходит до сохранения.
    /// </remarks>
    protected override void OnExecute()
    {
        TimeLimitedRewardService.Instance.RemoveExpired(save: true, requiredNotify: true);
    }

    #endregion
}
