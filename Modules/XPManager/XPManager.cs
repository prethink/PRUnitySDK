using System;
using UnityEngine;

public class XPManager : SingletonProviderBase<XPManager>
{
    #region Поля и свойства

    private bool isInitialized;
    public long CurrentLevel { get; private set; }

    public long CurrentExperience { get; private set; }

    #endregion

    #region События

    public event Action<XPData> OnLevelUp;

    #endregion

    #region Методы

    public XPData InitLevelSystem(long score)
    {
        var data = CalculateLevel(score);
        isInitialized = true;
        return data;
    }

    public XPData GetCurrentData()
    {
        return CalculateLevel(GetExperiencePoints());
    }

    /// <summary>
    /// Рассчитать уровень.
    /// </summary>
    /// <param name="score">Количество очков.</param>
    /// <returns>Уровень.</returns>
    public XPData CalculateLevel(long score)
    {
        return CalculateLevelHandle(score);
    }

    public XPData CalculateLevelHandle(long totalScore)
    {
        float growthFactor = GetSettings().GrowthFactor >= 1 
            ? GetSettings().GrowthFactor 
            : 1 + GetSettings().GrowthFactor;

        long requiredNextLevelExperience = GetSettings().BasePoints;
        long nextRequiredExperience = requiredNextLevelExperience;
        long previousNextRequiredExperience = nextRequiredExperience;

        long calculateScore = totalScore;
        if(CurrentLevel < GetSettings().StartLevel)
            CurrentLevel = GetSettings().StartLevel;

        var calculateLevel = GetSettings().StartLevel;

        while (calculateScore >= requiredNextLevelExperience)
        {
            calculateScore -= requiredNextLevelExperience;
            previousNextRequiredExperience = nextRequiredExperience;
            nextRequiredExperience = (long)(calculateLevel * GetSettings().BasePoints * growthFactor + previousNextRequiredExperience);
            requiredNextLevelExperience = nextRequiredExperience - previousNextRequiredExperience;
            calculateLevel++;

            if (calculateLevel > CurrentLevel)
            {
                CurrentLevel = calculateLevel;
                if(isInitialized)
                {
                    OnLevelUp?.Invoke(new XPData(CurrentLevel, totalScore, calculateScore, nextRequiredExperience, requiredNextLevelExperience));
                    //TODO: metric.Send("common", "level", CurrentLevel.ToString());
                }
            }
        }

        return new XPData(CurrentLevel, totalScore, calculateScore, nextRequiredExperience, requiredNextLevelExperience);
    }

    public void AddLevel(int addValue = 1)
    {
        if (addValue < 1)
            return;

        var currentData = GetCurrentData();
        var requiredLevel = currentData.CurrentLevel + addValue;
        var requiredXP = GetRequiredScoreForLevel(requiredLevel) - currentData.CurrentScore;
        if (requiredXP < 0)
            return;
        AddExperiencePoints(requiredXP);
    }

    public long GetRequiredScoreForLevel(long targetLevel)
    {
        if (targetLevel < GetSettings().StartLevel)
            return 0;

        float growthFactor = GetSettings().GrowthFactor >= 1
            ? GetSettings().GrowthFactor
            : 1 + GetSettings().GrowthFactor;

        long requiredNextLevelExperience = GetSettings().BasePoints;
        long nextRequiredExperience = requiredNextLevelExperience;
        long previousNextRequiredExperience = 0;

        long totalRequiredScore = 0;
        int level = GetSettings().StartLevel;

        while (level < targetLevel)
        {
            long currentLevelXP = nextRequiredExperience - previousNextRequiredExperience;
            totalRequiredScore += currentLevelXP;

            previousNextRequiredExperience = nextRequiredExperience;
            nextRequiredExperience = (long)(level * GetSettings().BasePoints * growthFactor + previousNextRequiredExperience);
            level++;
        }

        return totalRequiredScore;
    }

    private long GetCommonRequiredExperience(long level)
    {
        var nextRequiredExperience = GetSettings().BasePoints;
        float growthFactor = GetSettings().GrowthFactor;
        for (int i = 0; i < level; i++)
        {
            if (i == 0)
                continue;

            nextRequiredExperience = Mathf.CeilToInt(nextRequiredExperience * growthFactor);
        }

        return nextRequiredExperience;
    }

    private bool IsStartLevel()
    {
        return CurrentLevel == GetSettings().StartLevel;
    }

    public long AddExperiencePoints(long addPoints)
    {
        var currentPoints = GetExperiencePoints();
        var resultPoints = currentPoints + addPoints;
        GetManager().SetLong(PRUnityPropertyConstants.XP_PROPERTY_NAME, currentPoints + addPoints, false);

        bool onChangeLevel = false;
        OnLevelUp += (XPData data) =>
        {
            onChangeLevel = true;
            //TODO:EventBus.RaiseEvent<IGlobalBarEvent>(invoke => invoke.OnChangeValue(Constants.GLOBAL_EVENT_BAR_XP, data.CurrentLevelScore, data.RequiredLevelScore));
            //TODO:EventBus.RaiseEvent<INotifyEventUI>(ui => ui.ChangeStateUI(StatDropDown.STAT_LEVEL_NAME, data.CurrentLevel.ToString()));
        };

        if (!onChangeLevel)
        {
            var currentData = CalculateLevel(resultPoints);
            //TODO:EventBus.RaiseEvent<IGlobalBarEvent>(invoke => invoke.OnChangeValue(Constants.GLOBAL_EVENT_BAR_XP, currentData.CurrentLevelScore));
        }


        return resultPoints;
    }

    public long GetExperiencePoints()
    {
        return GetManager().TryGetLong(PRUnityPropertyConstants.XP_PROPERTY_NAME, out var points)
            ? points
            : 0;
    }

    public XPSettings GetSettings()
    {
        return PRUnitySDK.Settings.ExperiencePoints;
    }

    public ProjectPropertiesManager GetManager()
    {
        return PRUnitySDK.Managers.ProjectPropertiesManager;
    }

    public void Init()
    {
        CurrentLevel = GetSettings().StartLevel;
    }

    #endregion
}

public class XPData
{
    public long CurrentLevel;
    public long CurrentScore;
    public long CurrentLevelScore;
    public long RequiredLevelScore;
    public long RequiredScore;

    public XPData(long currentLevel, long currentScore, long currentLevelScore, long requiredScore, long requiredLevelScore)
    {
        CurrentLevel = currentLevel;
        CurrentScore = currentScore;
        CurrentLevelScore = currentLevelScore;
        RequiredScore = requiredScore;
        RequiredLevelScore = requiredLevelScore;
    }
}
