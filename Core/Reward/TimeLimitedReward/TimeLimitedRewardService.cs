using System;
using System.Collections.Generic;

/// <summary>
/// Хранилище временных наград: VIP, бустеры и всё, что действует до определённого момента.
/// <para>
/// Данные лежат в собственном словаре <see cref="ProjectData.TimeLimitedRewards"/>, а не
/// среди дат ProjectProperties. Благодаря этому награды можно перечислить, проверить
/// на истечение и очистить, а их ключи не пересекаются с произвольными свойствами.
/// </para>
/// <para>
/// Все проверки идут по серверному времени: локальные часы можно перевести и продлить
/// награду бесплатно.
/// </para>
/// </summary>
public class TimeLimitedRewardService : SingletonProviderBase<TimeLimitedRewardService>
{
    #region Поля и свойства

    private readonly ProjectDataMap<string, DateTime> rewards;

    public TimeLimitedRewardService()
    {
        rewards = new ProjectDataMap<string, DateTime>(
            () => GameManager.Instance.GetProjectData(),
            projectData => projectData.TimeLimitedRewards ??= new Dictionary<string, DateTime>());
    }

    #endregion

    #region Чтение

    /// <summary>
    /// Действует ли награда сейчас.
    /// </summary>
    /// <param name="key">Ключ награды.</param>
    /// <param name="endTime">Момент окончания действия.</param>
    public bool IsActive(string key, out DateTime endTime)
    {
        endTime = DateTime.MinValue;

        if (string.IsNullOrEmpty(key) || !rewards.TryGetValue(key, out var storedEndTime))
            return false;

        if (PRUnitySDK.ServerTime.GetNow() > storedEndTime)
            return false;

        endTime = storedEndTime;
        return true;
    }

    /// <summary>
    /// Сколько осталось до окончания награды. Ноль, если награда не выдана или истекла.
    /// </summary>
    public TimeSpan GetRemaining(string key)
    {
        if (!IsActive(key, out var endTime))
            return TimeSpan.Zero;

        return endTime - PRUnitySDK.ServerTime.GetNow();
    }

    /// <summary>
    /// Состояние конкретной награды, включая истёкшую.
    /// </summary>
    public bool TryGetState(string key, out TimeLimitedRewardState state)
    {
        state = default;

        if (string.IsNullOrEmpty(key) || !rewards.TryGetValue(key, out var endTime))
            return false;

        state = CreateState(key, endTime);
        return true;
    }

    /// <summary>
    /// Все награды, действующие прямо сейчас. Раньше такой список получить было нельзя:
    /// даты наград лежали вперемешку с прочими свойствами проекта.
    /// </summary>
    public IReadOnlyList<TimeLimitedRewardState> GetActive()
    {
        var result = new List<TimeLimitedRewardState>();
        var now = PRUnitySDK.ServerTime.GetNow();

        foreach (var pair in GetStorage())
        {
            if (pair.Value > now)
                result.Add(CreateState(pair.Key, pair.Value));
        }

        return result;
    }

    #endregion

    #region Изменение

    /// <summary>
    /// Продлить награду или выдать её заново, если срок уже вышел.
    /// </summary>
    /// <param name="key">Ключ награды.</param>
    /// <param name="addTime">Добавляемое время.</param>
    /// <param name="save">Сохранять ли данные сразу.</param>
    /// <param name="requiredNotify">Публиковать ли событие изменения.</param>
    /// <returns>Новый момент окончания действия.</returns>
    public DateTime AddTime(string key, TimeSpan addTime, bool save = true, bool requiredNotify = true)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        // Активная награда продлевается от своего конца, истёкшая - от текущего момента:
        // иначе давно закончившийся бустер вернул бы всё накопленное время.
        var wasActive = IsActive(key, out var currentEndTime);
        var endTime = (wasActive ? currentEndTime : PRUnitySDK.ServerTime.GetNow()).Add(addTime);

        rewards.SetValue(key, endTime);

        if (save)
            GameManager.Instance.SaveProjectData();

        if (requiredNotify)
            TimeLimitedRewardEvents.RaiseChanged(key, endTime, wasActive);

        return endTime;
    }

    /// <summary>
    /// Задать момент окончания напрямую.
    /// </summary>
    public void SetEndTime(string key, DateTime endTime, bool save = true, bool requiredNotify = true)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        var wasActive = IsActive(key, out _);
        var change = rewards.SetValue(key, endTime);

        if (!change.Changed)
            return;

        if (save)
            GameManager.Instance.SaveProjectData();

        if (requiredNotify)
            TimeLimitedRewardEvents.RaiseChanged(key, endTime, wasActive);
    }

    /// <summary>
    /// Снять награду досрочно.
    /// </summary>
    public bool Remove(string key, bool save = true, bool requiredNotify = true)
    {
        if (string.IsNullOrEmpty(key) || !rewards.TryRemoveValue(key, out var endTime))
            return false;

        if (save)
            GameManager.Instance.SaveProjectData();

        if (requiredNotify)
            TimeLimitedRewardEvents.RaiseExpired(key, endTime);

        return true;
    }

    /// <summary>
    /// Удалить записи наград, срок которых уже вышел, и уведомить об окончании.
    /// <para>
    /// Вызывайте периодически (например, раз в секунду из игрового цикла) - тогда UI
    /// узнает об окончании сам, а не при следующей проверке IsActive.
    /// </para>
    /// </summary>
    /// <returns>Сколько наград было снято.</returns>
    public int RemoveExpired(bool save = true, bool requiredNotify = true)
    {
        var now = PRUnitySDK.ServerTime.GetNow();
        List<KeyValuePair<string, DateTime>> expired = null;

        foreach (var pair in GetStorage())
        {
            if (pair.Value > now)
                continue;

            expired ??= new List<KeyValuePair<string, DateTime>>();
            expired.Add(pair);
        }

        if (expired == null)
            return 0;

        // Удаление идёт отдельным проходом: менять словарь во время перебора нельзя.
        foreach (var pair in expired)
            rewards.TryRemoveValue(pair.Key, out _);

        if (save)
            GameManager.Instance.SaveProjectData();

        if (requiredNotify)
        {
            foreach (var pair in expired)
                TimeLimitedRewardEvents.RaiseExpired(pair.Key, pair.Value);
        }

        return expired.Count;
    }

    /// <summary>
    /// Снять все временные награды - для отладки и сброса прогресса.
    /// </summary>
    public void Clear(bool save = true)
    {
        GetStorage().Clear();

        if (save)
            GameManager.Instance.SaveProjectData();
    }

    #endregion

    #region Внутреннее

    private Dictionary<string, DateTime> GetStorage()
    {
        var projectData = GameManager.Instance.GetProjectData();

        if (projectData == null)
            throw new InvalidOperationException(
                "TimeLimitedRewardService: данные проекта ещё не загружены.");

        return projectData.TimeLimitedRewards ??= new Dictionary<string, DateTime>();
    }

    private static TimeLimitedRewardState CreateState(string key, DateTime endTime)
    {
        var now = PRUnitySDK.ServerTime.GetNow();
        var remaining = endTime > now ? endTime - now : TimeSpan.Zero;

        return new TimeLimitedRewardState(key, endTime, remaining);
    }

    #endregion
}
