/// <summary>
/// Считает, сколько минут игрок провёл в игре, и пишет это в статистику.
/// </summary>
[AutoBackgroundTask]
public class PlaytimeTrackerTask : BackgroundTask
{
    /// <inheritdoc />
    public override Enumeration Key => BackgroundTaskKeyEnumerations.PlaytimeTracker;

    /// <inheritdoc />
    public override string Name => "Учёт времени в игре";

    /// <inheritdoc />
    public override float RepeatSeconds => 60f;

    /// <summary>
    /// По игровому времени: на логической паузе и в меню счётчик стоит, а при замедлении
    /// идёт медленнее. Это время игры, а не хронометр.
    /// </summary>
    public override bool UseGameTime => true;

    /// <summary>
    /// Пока сохранение не загружено, статистики ещё нет. Запуск просто пропускается.
    /// </summary>
    public override bool CanExecute()
    {
        return GameManager.HasInstance && GameManager.Instance.ReadySignal.IsReady;
    }

    /// <inheritdoc />
    protected override void OnExecute()
    {
        StatisticsManager.Instance.AddPlaytimeMinutes(1);
    }
}
