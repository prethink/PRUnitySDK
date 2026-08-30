/// <summary>
/// Пример обычной фоновой задачи: считает, сколько минут игрок провёл в игре,
/// и складывает результат в свойства проекта.
/// </summary>
/// <remarks>
/// Показывает три вещи, ради которых задача лучше корутины:
/// <list type="bullet">
/// <item>работу по игровому времени - на паузе счётчик не капает;</item>
/// <item><see cref="CanExecute"/> как штатный пропуск, пока данные не загружены;</item>
/// <item>запись без немедленного сохранения, чтобы не дёргать диск каждую минуту.</item>
/// </list>
/// <para>
/// Задача выключена: у атрибута стоит <c>Enabled = false</c>, поэтому SDK её не
/// регистрирует. Чтобы попробовать - уберите этот параметр либо зарегистрируйте
/// задачу вручную:
/// <code>PRUnitySDK.Trackers.BackgroundTasks.Register(new PlaytimeTrackerTask());</code>
/// </para>
/// </remarks>
[AutoBackgroundTask]
public class PlaytimeTrackerTask : BackgroundTask
{
    /// <summary>
    /// Имя свойства, в котором накапливается суммарное время игры в минутах.
    /// </summary>
    public const string PlaytimeMinutesProperty = nameof(PlaytimeMinutesProperty);

    /// <inheritdoc />
    public override Enumeration Key => BackgroundTaskKeyEnumerations.PlaytimeTracker;

    /// <inheritdoc />
    public override string Name => "Учёт времени в игре";

    /// <inheritdoc />
    public override float RepeatSeconds => 60f;

    /// <summary>
    /// Считаем по игровому времени: на логической паузе и в меню счётчик стоит,
    /// а при замедлении времени идёт медленнее - это именно «время игры», а не хронометр.
    /// </summary>
    public override bool UseGameTime => true;

    /// <summary>
    /// Пока сохранение не загружено, писать в свойства нельзя: значение затёрлось бы
    /// при загрузке. Возврат false не считается ошибкой - задача просто ждёт.
    /// </summary>
    public override bool CanExecute()
    {
        return GameManager.Instance != null && GameManager.Instance.ReadySignal.IsReady;
    }

    /// <inheritdoc />
    protected override void OnExecute()
    {
        ProjectPropertiesManager properties = PRUnitySDK.Managers.ProjectProperties;

        long minutes = properties.GetLong(PlaytimeMinutesProperty);

        // save: false - на диск попадёт при ближайшем автосохранении GameManager.
        // Иначе запись шла бы каждую минуту без всякой необходимости.
        properties.SetValue(PlaytimeMinutesProperty, minutes + 1, save: false);
    }
}
