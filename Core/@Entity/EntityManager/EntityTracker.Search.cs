using System;
using System.Collections.Generic;

public partial class EntityTracker
{
    /// <summary>
    /// Снимок существующих сущностей с указанным состоянием.
    /// </summary>
    public List<IEntity> GetEntities(EntitySearchFlags flags = EntitySearchFlags.None)
    {
        return FindEntities<IEntity>(flags);
    }

    /// <summary>
    /// Снимок сущностей точного EntityType; metadata дополнительно ограничивает поиск описанием.
    /// </summary>
    public List<IEntity> GetEntities(Enumeration type, EntitySearchFlags flags = EntitySearchFlags.None,
        IEntityMetadata metadata = null)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        return FindEntities<IEntity>(flags, type, metadata);
    }

    /// <summary>
    /// Снимок сущностей по ссылке на EntityMetadata или Definition, включая переопределённое описание.
    /// </summary>
    public List<IEntity> GetEntities(IEntityMetadata metadata, EntitySearchFlags flags = EntitySearchFlags.None)
    {
        if (metadata.IsNull())
            throw new ArgumentNullException(nameof(metadata));

        return FindEntities<IEntity>(flags, metadata: metadata);
    }

    /// <summary>
    /// Снимок сущностей CLR-типа T, включая наследников и реализации интерфейса.
    /// </summary>
    public List<T> GetEntities<T>(EntitySearchFlags flags = EntitySearchFlags.None)
        where T : class, IEntity
    {
        return FindEntities<T>(flags);
    }

    /// <summary>
    /// Считает существующие сущности с указанным состоянием без создания списка.
    /// </summary>
    public long GetEntitiesCount(EntitySearchFlags flags)
    {
        return CountEntities<IEntity>(flags);
    }

    /// <summary>
    /// Считает сущности точного EntityType с необязательным ограничением по описанию.
    /// </summary>
    public long GetEntitiesCount(Enumeration type, EntitySearchFlags flags = EntitySearchFlags.None,
        IEntityMetadata metadata = null)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        return CountEntities<IEntity>(flags, type, metadata);
    }

    /// <summary>
    /// Считает сущности с указанным EntityMetadata или Definition без создания списка.
    /// </summary>
    public long GetEntitiesCount(IEntityMetadata metadata, EntitySearchFlags flags = EntitySearchFlags.None)
    {
        if (metadata.IsNull())
            throw new ArgumentNullException(nameof(metadata));

        return CountEntities<IEntity>(flags, metadata: metadata);
    }

    /// <summary>
    /// Считает сущности CLR-типа T с учётом наследования без создания списка.
    /// </summary>
    public long GetEntitiesCount<T>(EntitySearchFlags flags = EntitySearchFlags.None)
        where T : class, IEntity
    {
        return CountEntities<T>(flags);
    }

    private List<T> FindEntities<T>(EntitySearchFlags flags, Enumeration type = null,
        IEntityMetadata metadata = null) where T : class, IEntity
    {
        var result = new List<T>();
        foreach (var entity in elements)
        {
            if (entity is T typed && MatchesSearch(entity, flags, type, metadata))
                result.Add(typed);
        }

        return result;
    }

    private long CountEntities<T>(EntitySearchFlags flags, Enumeration type = null,
        IEntityMetadata metadata = null) where T : class, IEntity
    {
        long count = 0;
        foreach (var entity in elements)
        {
            if (entity is T && MatchesSearch(entity, flags, type, metadata))
                count++;
        }

        return count;
    }

    private static bool MatchesSearch(IEntity entity, EntitySearchFlags flags,
        Enumeration type, IEntityMetadata metadata)
    {
        if (entity.IsNull())
            return false;

        if (type != null && entity.EntityType != type)
            return false;

        if (metadata != null &&
            !ReferenceEquals(entity.Description?.Base, metadata) &&
            !ReferenceEquals(entity.Description?.Override, metadata) &&
            !(entity is EntityBase<EntityMetadata> configured &&
              ReferenceEquals(configured.Metadata, metadata)))
            return false;

        if ((flags & EntitySearchFlags.OnScene) != 0 && !entity.OnScene)
            return false;
        if ((flags & EntitySearchFlags.InPool) != 0 && !entity.InPool)
            return false;
        if ((flags & EntitySearchFlags.NotInPool) != 0 && entity.InPool)
            return false;

        if ((flags & (EntitySearchFlags.Visible | EntitySearchFlags.NotVisible)) != 0)
        {
            bool visible = entity.OnScene && !entity.InPool;
            if ((flags & EntitySearchFlags.Visible) != 0 && !visible)
                return false;
            if ((flags & EntitySearchFlags.NotVisible) != 0 && visible)
                return false;
        }

        EntitySearchFlags hideFlags = flags & EntitySearchFlags.Hidden;
        if (hideFlags != EntitySearchFlags.None)
        {
            if (entity.OnScene || entity.InPool || entity is not EntityBase hidden)
                return false;

            Enumeration disposeAction = hidden.DisposeAction;
            EntitySearchFlags mode = disposeAction == EntityDisposeEnumerations.Hide
                ? EntitySearchFlags.Hide
                : disposeAction == EntityDisposeEnumerations.HideWire
                    ? EntitySearchFlags.HideWire
                    : disposeAction == EntityDisposeEnumerations.HideWirePolygons
                        ? EntitySearchFlags.HideWirePolygons
                        : EntitySearchFlags.None;

            if ((hideFlags & mode) == EntitySearchFlags.None)
                return false;
        }

        if ((flags & (EntitySearchFlags.Alive | EntitySearchFlags.Dead)) != 0)
        {
            if (entity is not IHealthProvider provider)
                return false;

            HealthComponent health = provider.Health;
            if (health == null)
                return false;

            bool alive = health.IsAlive();
            if ((flags & EntitySearchFlags.Alive) != 0 && !alive)
                return false;
            if ((flags & EntitySearchFlags.Dead) != 0 && alive)
                return false;
        }

        return true;
    }
}
