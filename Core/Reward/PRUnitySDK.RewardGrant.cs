/// <summary>
/// Подключение системы выдачи наград к общему SDK.
/// </summary>
public partial class PRUnitySDK
{
    private const int RewardGrantServicePriority = 60;

    /// <summary>
    /// Единый сервис выдачи наград.
    /// </summary>
    public static IRewardGrantService RewardGrantService;

    /// <summary>
    /// Инициализирует и регистрирует сервис выдачи наград.
    /// </summary>
    [MethodHook(MethodHookStage.SDK, RewardGrantServicePriority)]
    private static void InitializeRewardGrantService()
    {
        InitializeModuleSDK(nameof(IRewardGrantService), () =>
        {
            typeof(PRUnitySDK).TryOverrideStaticProperty(typeof(IRewardGrantService));

            InitializeDefault(
                nameof(RewardGrantService),
                () => RewardGrantService,
                () => RewardGrantService = new global::RewardGrantService());

            return RewardGrantService;
        });
    }
}
