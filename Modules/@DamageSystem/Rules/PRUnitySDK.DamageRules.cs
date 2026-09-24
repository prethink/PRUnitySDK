public partial class PRUnitySDK
{
    /// <summary>
    /// Приоритет.
    /// </summary>
    private const int PRIORITY_DAMAGE_RULES = 20;

    /// <summary>
    /// Ставит правила урона проекта из настроек.
    /// </summary>
    /// <remarks>
    /// На старте SDK, до того как на сценах начнутся удары. Сценовые правила приходят
    /// сами, когда включаются их компоненты.
    /// </remarks>
    [MethodHook(MethodHookStage.SDK, PRIORITY_DAMAGE_RULES)]
    private static void InitializeDamageRules()
    {
        DamageRules.Instance.SetProjectRules(Settings.DamageRules.Rules);
    }
}
