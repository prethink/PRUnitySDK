/// <summary>
/// Отвечает, принадлежит ли запись экземпляра какой-нибудь сущности на сцене.
/// </summary>
/// <remarks>
/// Проверка для мест, где запись меняет владельца: прежде чем положить её в контейнер
/// или создать с ней сущность, убеждаются, что экземпляр не живёт где-то ещё.
/// Своего учёта не ведёт: спрашивает <c>EntityTracker</c>.
/// </remarks>
public static class EntityInstanceOwners
{
    /// <summary>
    /// Сущность на сцене, которая держит экземпляр с этим идентификатором.
    /// </summary>
    /// <remarks>
    /// Сущности в пуле не считаются: запись прошлой жизни у них ещё лежит, но ими уже
    /// не используется. Сущность, созданная в этом же кадре, в трекере появится только
    /// со своим <c>Start</c> и сюда ещё не попадёт.
    /// </remarks>
    /// <param name="instanceId">Идентификатор экземпляра.</param>
    /// <param name="except">Сущность, которую не считать: та, что сама отдаёт запись.</param>
    /// <returns>Владелец либо <c>null</c>.</returns>
    public static IEntityInstance Find(string instanceId, IEntityInstance except = null)
    {
        EntityTracker tracker = PRUnitySDK.Trackers?.Entities;

        if (tracker == null || string.IsNullOrEmpty(instanceId))
            return null;

        foreach (IEntity entity in tracker.GetEntities<IEntity>(EntitySearchFlags.NotInPool))
        {
            if (entity is not IEntityInstance candidate || ReferenceEquals(candidate, except) || candidate.Instance == null)
                continue;

            if (candidate.Instance.InstanceId == instanceId)
                return candidate;
        }

        return null;
    }
}
