using System;
using System.Collections.Generic;
using System.Linq;

public class EntityTracker : EntityTrackerBase<IEntity>
{
    #region Поля и свойства

    public List<IEntity> Entities => elements.ToList();

    public long GetEntitiesCount() => elements.Count;
    public long GetExistsEntityCount() => elements.Count(x => x != null);

    public long GetEntityOnSceneCount() => elements.Count(x => x != null && x.OnScene);
    public long GetEntityInPoolCount() => elements.Count(x => x != null && x.InPool);
    public long GetExactExistsEntityCount(Enumeration type) => elements.Count(x => x != null && x.EntityType == type);
    public long GetExactEntityOnSceneCount(Enumeration type) => elements.Count(x => x != null && x.EntityType == type && x.OnScene);
    public long GetExactEntityInPoolCount(Enumeration type) => elements.Count(x => x != null && x.EntityType == type && x.InPool);
    //TODO:
    public long GetInheritedExistsEntityCount(Type type) => elements.Count(x => x != null && x.GetType().IsAssignableFrom(type));

    public long GetInheritedEntityOnSceneCount(Type type) => elements.Count(x => x != null && x.GetType().IsAssignableFrom(type) && x.OnScene);

    public long GetInheritedEntityInPoolCount(Type type) => elements.Count(x => x != null && x.GetType().IsAssignableFrom(type) && x.InPool);

    private Dictionary<Enumeration, long> registeredEntity = new Dictionary<Enumeration, long>();

    public IReadOnlyDictionary<Enumeration, long> RegisteredEntity => registeredEntity;

    #endregion

    public override bool Register(IEntity entity)
    {
        entity.GenerateId(EntityIdGenerator.Instance.RegisterId);
        elements.Add(entity);
        RegisterEntityType(entity.EntityType);

        //if (gameSessionManager.Settings.isActiveDebugLog)
        //    PRLog.WriteDebug(this, $"Сущность {entity.EntityType} - ID:{entity.Id} зарегистрирована в entityTracker.");

        return true;
    }

    public override bool Unregister(IEntity entity)
    {
        elements.Remove(entity);
        UnRegisterEntityType(entity.EntityType);
        //if (gameSessionManager.Settings.isActiveDebugLog)
        //    PRLog.WriteDebug(this, $"Сущность {entity.EntityType} - ID:{entity.Id} удалена из entityTracker.");

        return true;
    }

    private void RegisterEntityType(Enumeration type)
    {
        long currentEntitiesCount = GetRegisteredEntityCount(type);
        registeredEntity[type] = currentEntitiesCount + 1;
    }

    private void UnRegisterEntityType(Enumeration type)
    {
        long currentEntitiesCount = GetRegisteredEntityCount(type);
        registeredEntity[type] = currentEntitiesCount - 1;
    }

    public long GetRegisteredEntityCount(Enumeration type)
    {
        long currentEntitiesCount = 0;
        registeredEntity.TryGetValue(type, out currentEntitiesCount);
        return currentEntitiesCount;
    }

    public override void Clear()
    {
        foreach (var entity in elements.ToList())
        {
            entity.DestroyEntity(new EntityDestroyOptions() { FullDestroy = true });
            elements.Remove(entity);
        }
    }
}