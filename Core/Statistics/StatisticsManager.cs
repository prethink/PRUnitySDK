using System;
using System.Collections.Generic;

/// <summary>
/// Статистика игрока за всё время: запуски, время в игре, полученные ресурсы и счётчики модулей.
/// </summary>
/// <remarks>
/// Данные лежат в <see cref="ProjectData.Statistics"/> и уходят на диск с обычным сохранением:
/// сама статистика запись не запрашивает. Читать и менять можно после
/// <c>GameManager.ReadySignal</c>, когда сохранение загружено.
/// </remarks>
public class StatisticsManager : SingletonProviderBase<StatisticsManager>
{
    // Свойства проекта, в которых запуски и время лежали до появления статистики.
    private const string LegacyLaunchCountProperty = "LAUNCH_COUNT";
    private const string LegacyPlaytimeProperty = "PlaytimeMinutesProperty";

    private ProjectData migratedData;
    private bool isLaunchCounted;

    /// <summary>
    /// Сколько раз игрок заходил в игру, считая текущий заход.
    /// </summary>
    public long LaunchCount => CountLaunch();

    /// <summary>
    /// Время в игре в минутах.
    /// </summary>
    public long PlaytimeMinutes => GetData().PlaytimeMinutes;

    /// <summary>
    /// Ждёт загрузки сохранения, чтобы засчитать заход.
    /// </summary>
    public StatisticsManager()
    {
        // До готовности SDK обращение к GameManager создало бы его раньше загрузчика.
        PRUnitySDK.ReadySignal.SubscribeOnReady(() =>
            GameManager.Instance.ReadySignal.SubscribeOnReady(() => CountLaunch()));
    }

    /// <summary>
    /// Засчитывает текущий заход, если он ещё не засчитан, и возвращает его номер.
    /// </summary>
    public long CountLaunch()
    {
        GameStatistics data = GetData();

        if (!isLaunchCounted)
        {
            isLaunchCounted = true;
            data.LaunchCount++;
        }

        return data.LaunchCount;
    }

    /// <summary>
    /// Прибавляет время в игре.
    /// </summary>
    public void AddPlaytimeMinutes(long minutes)
    {
        if (minutes > 0)
            GetData().PlaytimeMinutes += minutes;
    }

    /// <summary>
    /// Сколько ресурса получено за всё время.
    /// </summary>
    /// <returns><c>false</c>, если счётчика этого ресурса в сохранении ещё нет.</returns>
    public bool TryGetResourceTotal(string resourceName, out long total)
    {
        total = 0;
        return !string.IsNullOrEmpty(resourceName) && GetResources().TryGetValue(resourceName, out total);
    }

    /// <summary>
    /// Прибавляет полученный ресурс к счётчику за всё время.
    /// </summary>
    /// <param name="resourceName">Имя ресурса.</param>
    /// <param name="previousBalance">Остаток до начисления. С него начинается счётчик, которого ещё нет:
    /// ресурс мог копиться до появления статистики.</param>
    /// <param name="gained">Сколько получено.</param>
    public void AddResource(string resourceName, long previousBalance, long gained)
    {
        if (string.IsNullOrEmpty(resourceName) || gained <= 0)
            return;

        Dictionary<string, long> resources = GetResources();
        long total = resources.TryGetValue(resourceName, out long saved) ? saved : Math.Max(0L, previousBalance);

        resources[resourceName] = total + gained;
    }

    /// <summary>
    /// Значение счётчика модуля.
    /// </summary>
    /// <returns><c>false</c>, если счётчика в сохранении ещё нет.</returns>
    public bool TryGetCounter(string key, out long value)
    {
        value = 0;
        return !string.IsNullOrEmpty(key) && GetCounters().TryGetValue(key, out value);
    }

    /// <summary>
    /// Значение счётчика модуля или <paramref name="fallback"/>, если его ещё нет.
    /// </summary>
    public long GetCounter(string key, long fallback = 0)
    {
        return TryGetCounter(key, out long value) ? value : fallback;
    }

    /// <summary>
    /// Записывает значение счётчика модуля.
    /// </summary>
    public void SetCounter(string key, long value)
    {
        if (!string.IsNullOrEmpty(key))
            GetCounters()[key] = value;
    }

    /// <summary>
    /// Прибавляет к счётчику модуля и возвращает новое значение.
    /// </summary>
    public long AddCounter(string key, long value = 1)
    {
        long current = GetCounter(key) + value;
        SetCounter(key, current);
        return current;
    }

    private Dictionary<string, long> GetResources()
    {
        GameStatistics data = GetData();
        return data.Resources ??= new Dictionary<string, long>();
    }

    private Dictionary<string, long> GetCounters()
    {
        GameStatistics data = GetData();
        return data.Counters ??= new Dictionary<string, long>();
    }

    private GameStatistics GetData()
    {
        ProjectData projectData = GameManager.Instance.GetProjectData();
        GameStatistics data = projectData.Statistics ??= new GameStatistics();

        if (!ReferenceEquals(migratedData, projectData) && TryMigrateLegacyProperties(data))
            migratedData = projectData;

        return data;
    }

    /// <summary>
    /// Переносит запуски и время из свойств проекта, где они лежали раньше.
    /// </summary>
    /// <returns><c>false</c>, если менеджера свойств ещё нет и перенос надо повторить.</returns>
    private static bool TryMigrateLegacyProperties(GameStatistics data)
    {
        ProjectPropertiesManager properties = PRUnitySDK.Managers?.ProjectProperties;

        if (properties == null)
            return false;

        if (properties.TryGetLong(LegacyLaunchCountProperty, out long launches))
        {
            data.LaunchCount = Math.Max(data.LaunchCount, launches);
            properties.RemoveProperty<long>(LegacyLaunchCountProperty, save: false, requiredNotify: false);
        }

        if (properties.TryGetLong(LegacyPlaytimeProperty, out long minutes))
        {
            data.PlaytimeMinutes = Math.Max(data.PlaytimeMinutes, minutes);
            properties.RemoveProperty<long>(LegacyPlaytimeProperty, save: false, requiredNotify: false);
        }

        return true;
    }
}
