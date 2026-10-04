public partial class ProjectData
{
    /// <summary>
    /// Статистика игрока за всё время. Ведёт <c>StatisticsManager</c>.
    /// </summary>
    public GameStatistics Statistics { get; set; } = new();

    [MethodHook(MethodHookStage.Cloning)]
    public void CloneStatistics(ProjectData clone)
    {
        clone.Statistics = (GameStatistics)(Statistics ?? new GameStatistics()).Clone();
    }

    [MethodHook(MethodHookStage.Initializing)]
    public void InitializeStatistics()
    {
        Statistics = new GameStatistics();
    }
}
