public partial class PRManagerContainer
{
    /// <summary>
    /// Статистика игрока за всё время.
    /// </summary>
    public StatisticsManager Statistics;

    /// <summary>
    /// Раньше менеджера ресурсов: тот пишет в статистику каждое начисление.
    /// </summary>
    [MethodHook(MethodHookStage.PostOperation, 15)]
    public void InitializeStatisticsManager()
    {
        PRUnitySDK.InitializeManager(() =>
        {
            Statistics = StatisticsManager.Instance;
            return Statistics;
        });
    }
}
