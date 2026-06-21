/// <summary>
/// Предоставляет высокоуровневые доменные события, связанные с изменением состояния сущности.
/// </summary>
public static class EntityEvents
{
    /// <summary>
    /// Уведомляет подписчиков о необходимости общего обновления сущности.
    /// </summary>
    public static void RaiseRefresh(IEntity entity) => EventBus.RaiseEvent<IEntityRefreshEvent>(invoke => invoke.RefreshEntity(new EntityRefreshEventArgs(entity)));

    /// <summary>
    /// Уведомляет подписчиков о том, что характеристики сущности изменились и требуют пересчёта.
    /// </summary>
    public static void RaiseRefreshStats(IEntity entity) => EventBus.RaiseEvent<IEntityRefreshEvent>(invoke => invoke.RefreshEntity(new EntityRefreshStatsEventArgs(entity)));

    /// <summary>
    /// Уведомляет подписчиков о том, что флаги состояния сущности изменились и требуют переоценки.
    /// </summary>
    public static void RaiseRefreshFlags(IEntity entity) => EventBus.RaiseEvent<IEntityRefreshEvent>(invoke => invoke.RefreshEntity(new EntityRefreshFlagsEventArgs(entity)));

    /// <summary>
    /// Вызывает событие изменения экипировки сущности (оружие, броня, питомец и т.д.).
    /// </summary>
    public static void RaiseEquipmentChanged(IEntity entity) => EventBus.RaiseEvent<IEntityEquipmentChangedEvent>(invoke => invoke.OnEntityEquipmentChanged(new EntityEquipmentChangedEventArgs(entity)));
}
