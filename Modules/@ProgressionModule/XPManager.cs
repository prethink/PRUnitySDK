using System;

public class XPManager : SingletonProviderBase<XPManager>
{
    private bool isInitialized;

    /// <summary>
    /// Текущий уровень сохранённого прогресса.
    /// </summary>
    public long CurrentLevel { get; private set; }

    /// <summary>
    /// Общее количество сохранённого опыта.
    /// </summary>
    public long CurrentExperience { get; private set; }

    /// <summary>
    /// Вызывается после любого изменения общего количества опыта.
    /// </summary>
    public event Action<XPData> OnExperienceChanged;

    /// <summary>
    /// Вызывается при изменении уровня в любую сторону.
    /// </summary>
    public event Action<XPData> OnLevelChanged;

    /// <summary>
    /// Вызывается один раз, если после изменения опыта уровень повысился.
    /// </summary>
    public event Action<XPData> OnLevelUp;

    /// <summary>
    /// Инициализирует состояние указанным количеством опыта без отправки событий.
    /// </summary>
    public XPData InitLevelSystem(long score)
    {
        XPData data = CalculateLevel(score);
        ApplyState(data);
        isInitialized = true;
        return data;
    }

    /// <summary>
    /// Возвращает прогресс для сохранённого количества опыта без побочных эффектов.
    /// </summary>
    public XPData GetCurrentData() => CalculateLevel(GetExperiencePoints());

    /// <summary>
    /// Возвращает прогресс указанного игрока без побочных эффектов.
    /// </summary>
    /// <param name="player">Игрок, прогресс которого требуется получить.</param>
    public XPData GetCurrentData(IPlayer player) => CalculateLevel(GetExperiencePoints(player));

    /// <summary>
    /// Рассчитывает прогресс без изменения состояния и отправки событий.
    /// </summary>
    public XPData CalculateLevel(long score) => CalculateLevelHandle(score);

    /// <summary>
    /// Выполняет чистый расчёт прогресса.
    /// </summary>
    public XPData CalculateLevelHandle(long totalScore)
    {
        long normalizedScore = Math.Max(0L, totalScore);
        long startLevel = GetStartLevel();
        long level = FindLevel(normalizedScore, startLevel);
        long levelStartScore = GetRequiredScoreForLevel(level);
        long nextLevel = level == long.MaxValue ? long.MaxValue : level + 1L;
        long nextLevelScore = GetRequiredScoreForLevel(nextLevel);
        long experienceInLevel = Math.Max(0L, normalizedScore - levelStartScore);
        long experienceForNextLevel = nextLevelScore == long.MaxValue
            ? long.MaxValue
            : Math.Max(1L, nextLevelScore - levelStartScore);

        return new XPData(level, normalizedScore, experienceInLevel,
            nextLevelScore, experienceForNextLevel);
    }

    /// <summary>
    /// Добавляет указанное количество уровней, начисляя необходимый опыт.
    /// </summary>
    public void AddLevel(int addValue = 1)
    {
        if (addValue < 1)
            return;

        XPData current = GetCurrentData();
        long targetLevel = current.CurrentLevel > long.MaxValue - addValue
            ? long.MaxValue
            : current.CurrentLevel + addValue;
        long targetExperience = GetRequiredScoreForLevel(targetLevel);
        if (targetExperience != long.MaxValue)
            SetExperiencePoints(targetExperience);
    }

    /// <summary>
    /// Добавляет указанному игроку несколько уровней.
    /// </summary>
    /// <param name="player">Игрок, которому добавляются уровни.</param>
    /// <param name="addValue">Количество добавляемых уровней.</param>
    public void AddLevel(IPlayer player, int addValue = 1)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));

        if (addValue < 1)
            return;

        XPData current = GetCurrentData(player);
        long targetLevel = current.CurrentLevel > long.MaxValue - addValue
            ? long.MaxValue
            : current.CurrentLevel + addValue;
        long targetExperience = GetRequiredScoreForLevel(targetLevel);
        if (targetExperience != long.MaxValue)
            SetExperiencePoints(player, targetExperience);
    }

    /// <summary>
    /// Возвращает общий опыт, необходимый для начала указанного уровня.
    /// </summary>
    public long GetRequiredScoreForLevel(long targetLevel)
    {
        long startLevel = GetStartLevel();
        if (targetLevel <= startLevel)
            return 0L;

        long transitions = targetLevel - startLevel;
        try
        {
            decimal basePoints = GetBasePoints();
            decimal result = basePoints;

            if (transitions > 1)
            {
                decimal count = transitions - 1L;
                decimal firstLevel = startLevel;
                decimal lastLevel = startLevel + count - 1m;
                decimal levelSum = count * (firstLevel + lastLevel) / 2m;
                result += basePoints * GetGrowthFactor() * levelSum;
            }

            if (result >= long.MaxValue)
                return long.MaxValue;

            return Math.Max(0L, (long)decimal.Truncate(result));
        }
        catch (OverflowException)
        {
            return long.MaxValue;
        }
    }

    /// <summary>
    /// Изменяет опыт на указанную величину и сохраняет результат.
    /// Отрицательное значение уменьшает опыт, но результат не может быть меньше нуля.
    /// </summary>
    public long AddExperiencePoints(long addPoints, bool save = true)
    {
        decimal result = (decimal)GetExperiencePoints() + addPoints;
        long totalExperience = result <= 0m
            ? 0L
            : result >= long.MaxValue ? long.MaxValue : (long)result;
        return SetExperiencePoints(totalExperience, save);
    }

    /// <summary>
    /// Изменяет опыт указанного игрока и публикует событие через <see cref="EventBus"/>.
    /// </summary>
    /// <param name="player">Игрок, опыт которого изменяется.</param>
    /// <param name="addPoints">Величина изменения опыта.</param>
    /// <param name="save">Нужно ли сохранить изменение сразу.</param>
    public long AddExperiencePoints(IPlayer player, long addPoints, bool save = true)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));

        decimal result = (decimal)GetExperiencePoints(player) + addPoints;
        long totalExperience = result <= 0m
            ? 0L
            : result >= long.MaxValue ? long.MaxValue : (long)result;
        return SetExperiencePoints(player, totalExperience, save);
    }

    /// <summary>
    /// Устанавливает общее количество опыта.
    /// </summary>
    public long SetExperiencePoints(long totalExperience, bool save = true)
    {
        long normalizedExperience = Math.Max(0L, totalExperience);
        XPData oldData = isInitialized
            ? CalculateLevel(CurrentExperience)
            : CalculateLevel(GetExperiencePoints());
        XPData newData = CalculateLevel(normalizedExperience);

        GetManager().SetLong(PRUnityPropertyConstants.XP_PROPERTY_NAME, normalizedExperience, save);
        ApplyState(newData);

        if (isInitialized)
        {
            OnExperienceChanged?.Invoke(newData);
            if (newData.CurrentLevel != oldData.CurrentLevel)
                OnLevelChanged?.Invoke(newData);
            if (newData.CurrentLevel > oldData.CurrentLevel)
                OnLevelUp?.Invoke(newData);
        }

        isInitialized = true;
        return normalizedExperience;
    }

    /// <summary>
    /// Устанавливает общее количество опыта указанного игрока.
    /// </summary>
    /// <param name="player">Игрок, опыт которого изменяется.</param>
    /// <param name="totalExperience">Новое общее количество опыта.</param>
    /// <param name="save">Нужно ли сохранить изменение сразу.</param>
    public long SetExperiencePoints(IPlayer player, long totalExperience, bool save = true)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));

        long normalizedExperience = Math.Max(0L, totalExperience);
        XPData oldData = GetCurrentData(player);
        XPData newData = CalculateLevel(normalizedExperience);

        GetManager().SetLong(GetPlayerPropertyName(player), normalizedExperience, save);
        XPEvents.RaiseExperienceChanged(player, oldData, newData);

        if (newData.CurrentLevel != oldData.CurrentLevel)
            XPEvents.RaiseLevelChanged(player, oldData, newData);
        if (newData.CurrentLevel > oldData.CurrentLevel)
            XPEvents.RaiseLevelUp(player, oldData, newData);

        return normalizedExperience;
    }

    /// <summary>
    /// Возвращает сохранённое количество опыта.
    /// </summary>
    public long GetExperiencePoints()
    {
        return GetManager().TryGetLong(PRUnityPropertyConstants.XP_PROPERTY_NAME, out long points)
            ? Math.Max(0L, points)
            : 0L;
    }

    /// <summary>
    /// Возвращает сохранённый опыт указанного игрока.
    /// </summary>
    /// <param name="player">Игрок, опыт которого требуется получить.</param>
    public long GetExperiencePoints(IPlayer player)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));

        return GetManager().TryGetLong(GetPlayerPropertyName(player), out long points)
            ? Math.Max(0L, points)
            : 0L;
    }

    public XPSettings GetSettings() => PRUnitySDK.Settings.ExperiencePoints;

    public ProjectPropertiesManager GetManager() => PRUnitySDK.Managers.ProjectProperties;

    /// <summary>
    /// Загружает сохранённый опыт и инициализирует состояние без событий.
    /// </summary>
    public void Init() => InitLevelSystem(GetExperiencePoints());

    private void ApplyState(XPData data)
    {
        CurrentLevel = data.CurrentLevel;
        CurrentExperience = data.CurrentScore;
    }

    private long FindLevel(long totalExperience, long startLevel)
    {
        long low = startLevel;
        long high = startLevel == long.MaxValue ? long.MaxValue : startLevel + 1L;

        while (high < long.MaxValue)
        {
            long required = GetRequiredScoreForLevel(high);
            if (required == long.MaxValue || required > totalExperience)
                break;

            long distance = high - startLevel;
            if (distance > (long.MaxValue - startLevel) / 2L)
            {
                high = long.MaxValue;
                break;
            }

            high = startLevel + distance * 2L;
        }

        while (low < high)
        {
            long middle = low + (high - low + 1L) / 2L;
            long required = GetRequiredScoreForLevel(middle);
            if (required != long.MaxValue && required <= totalExperience)
                low = middle;
            else
                high = middle - 1L;
        }

        return low;
    }

    private long GetStartLevel() => Math.Max(1, GetSettings()?.StartLevel ?? 1);

    private long GetBasePoints() => Math.Max(1, GetSettings()?.BasePoints ?? 1);

    private decimal GetGrowthFactor() =>
        Math.Max(1m, (decimal)(GetSettings()?.GrowthFactor ?? 1f));

    private static string GetPlayerPropertyName(IPlayer player) =>
        $"{PRUnityPropertyConstants.XP_PROPERTY_NAME}_PLAYER_{player.PlayerId}";
}

/// <summary>
/// Неизменяемый снимок прогресса опыта.
/// </summary>
public sealed class XPData
{
    /// <summary>
    /// Текущий уровень.
    /// </summary>
    public long CurrentLevel { get; }

    /// <summary>
    /// Общее количество опыта.
    /// </summary>
    public long CurrentScore { get; }

    /// <summary>
    /// Опыт, набранный внутри текущего уровня.
    /// </summary>
    public long CurrentLevelScore { get; }

    /// <summary>
    /// Общий опыт, необходимый для следующего уровня.
    /// </summary>
    public long RequiredScore { get; }

    /// <summary>
    /// Размер текущего уровня в единицах опыта.
    /// </summary>
    public long RequiredLevelScore { get; }

    /// <summary>
    /// Нормализованный прогресс текущего уровня.
    /// </summary>
    public float NormalizedProgress => RequiredLevelScore <= 0 || RequiredLevelScore == long.MaxValue
        ? 0f
        : (float)Math.Min(1d, CurrentLevelScore / (double)RequiredLevelScore);

    public XPData(long currentLevel, long currentScore, long currentLevelScore,
        long requiredScore, long requiredLevelScore)
    {
        CurrentLevel = currentLevel;
        CurrentScore = currentScore;
        CurrentLevelScore = currentLevelScore;
        RequiredScore = requiredScore;
        RequiredLevelScore = requiredLevelScore;
    }
}
