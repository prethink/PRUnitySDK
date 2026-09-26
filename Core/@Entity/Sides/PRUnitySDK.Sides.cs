public partial class PRUnitySDK
{
    /// <summary>
    /// Приоритет: после правил урона проекта.
    /// </summary>
    private const int PRIORITY_ENTITY_SIDES = 21;

    /// <summary>
    /// Владелец правила сторон в <see cref="DamageRules"/>: по нему оно снимается при перечитывании.
    /// </summary>
    private static readonly object EntitySidesOwner = new();

    /// <summary>
    /// Ставит правило «свой-чужой».
    /// </summary>
    /// <remarks>
    /// Ставится всегда, а выключенные стороны правило просто пропускает
    /// (<see cref="EntitySides.ResolveDamage"/>): так их можно включить и выключить на ходу
    /// (<see cref="EntitySidesSettings.SetEnabled"/>). Прежнее снимается: настройки
    /// перечитываются при каждом старте SDK, и без этого правила копились бы при входе
    /// в Play Mode без перезагрузки домена.
    /// </remarks>
    [MethodHook(MethodHookStage.SDK, PRIORITY_ENTITY_SIDES)]
    private static void InitializeEntitySides()
    {
        DamageRules.Instance.Remove(EntitySidesOwner);
        DamageRules.Instance.Add(new EntitySideDamageRule(), EntitySidesOwner);
    }
}
