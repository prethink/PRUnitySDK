public partial class ProjectData
{
    /// <summary>
    /// Свойства ачивок.
    /// </summary>
    public AchievementProperties AchievementProperties;

    [MethodHook(MethodHookStage.Cloning)]
    public void CloneAchievement()
    {
        clone.AchievementProperties = (AchievementProperties)AchievementProperties.Clone();
    }

    [MethodHook(MethodHookStage.Initializing)]
    public void InitializeAchievement()
    {
        AchievementProperties = new AchievementProperties();
    }
}
