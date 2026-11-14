using System.Collections.Generic;

/// <summary>
/// Отвечает, принадлежит ли запись экземпляра какой-нибудь сущности на сцене.
/// </summary>
/// <remarks>
/// Проверка для мест, где запись меняет владельца: прежде чем положить её в контейнер
/// или создать с ней сущность, убеждаются, что экземпляр не живёт где-то ещё.
/// Зарегистрированных сущностей спрашивает у <c>EntityTracker</c>. Своим списком ведёт только тех,
/// кого в трекере ещё нет: сущность получает запись в <c>Awake</c> или раньше, а регистрируется в <c>Start</c>.
/// </remarks>
public static class EntityInstanceOwners
{
    /// <summary>
    /// Сущности с записью, у которых <c>Start</c> ещё не прошёл.
    /// </summary>
    private static readonly List<EntityBase> beforeStart = new();

    /// <summary>
    /// Сущность на сцене, которая держит экземпляр с этим идентификатором.
    /// </summary>
    /// <remarks>
    /// Не считаются: сущность в пуле — запись прошлой жизни у неё ещё лежит, но ею уже не используется;
    /// уничтоженная сущность, которая доживает кадр до отложенного <c>Destroy</c>, — её запись свободна,
    /// и предмет можно пересоздать в том же кадре. Спрятанная сущность владельцем остаётся: её вернут тем же
    /// экземпляром.
    /// </remarks>
    /// <param name="instanceId">Идентификатор экземпляра.</param>
    /// <param name="except">Сущность, которую не считать: та, что сама отдаёт запись.</param>
    /// <returns>Владелец либо <c>null</c>.</returns>
    public static IEntityInstance Find(string instanceId, IEntityInstance except = null)
    {
        if (string.IsNullOrEmpty(instanceId))
            return null;

        EntityTracker tracker = PRUnitySDK.Trackers?.Entities;

        if (tracker != null)
        {
            foreach (IEntity entity in tracker.GetEntities<IEntity>(EntitySearchFlags.NotInPool))
            {
                if (Holds(entity, instanceId, except))
                    return (IEntityInstance)entity;
            }
        }

        for (int i = beforeStart.Count - 1; i >= 0; i--)
        {
            EntityBase entity = beforeStart[i];

            // Объект, уничтоженный до своего Awake, о себе не сообщает: убираем его здесь.
            if (entity == null)
            {
                beforeStart.RemoveAt(i);
                continue;
            }

            if (!entity.InPool && Holds(entity, instanceId, except))
                return (IEntityInstance)entity;
        }

        return null;
    }

    /// <summary>
    /// Запоминает сущность с записью, которой ещё нет в трекере.
    /// </summary>
    /// <remarks>
    /// Зовут <see cref="EntityBase"/> в начале жизни и <see cref="EntityInstanceSpawn"/>, когда запись
    /// ставится объекту, созданному выключенным. Без этого два создания с одной записью в одном кадре
    /// давали двух владельцев: проверка первого ещё не видела.
    /// </remarks>
    internal static void TrackBeforeStart(EntityBase entity)
    {
        if (entity != null && !beforeStart.Contains(entity))
            beforeStart.Add(entity);
    }

    /// <summary>
    /// Сущность зарегистрировалась в трекере или уничтожена: отдельно помнить её больше не нужно.
    /// </summary>
    internal static void Forget(EntityBase entity)
    {
        beforeStart.Remove(entity);
    }

    private static bool Holds(IEntity entity, string instanceId, IEntityInstance except)
    {
        if (entity is not IEntityInstance candidate || ReferenceEquals(candidate, except) || candidate.Instance == null)
            return false;

        if (entity is EntityBase { LifeState: EntityLifeState.Destroyed })
            return false;

        return candidate.Instance.InstanceId == instanceId;
    }
}
