using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Хранит игровые сущности, назначает им идентификаторы и ведёт статистику по типам.
/// </summary>
public class EntityTracker : EntityTrackerBase<IEntity>
{
    #region Поля и свойства

    /// <summary>
    /// Возвращает снимок всех зарегистрированных сущностей.
    /// </summary>
    public List<IEntity> Entities => elements.ToList();

    public long GetEntitiesCount() => elements.Count;
    public long GetExistsEntityCount() => elements.Count(x => !x.IsNull());

    public long GetEntityOnSceneCount() => elements.Count(x => !x.IsNull() && x.OnScene);
    public long GetEntityInPoolCount() => elements.Count(x => !x.IsNull() && x.InPool);
    public long GetExactExistsEntityCount(Enumeration type) => elements.Count(x => !x.IsNull() && x.EntityType == type);
    public long GetExactEntityOnSceneCount(Enumeration type) => elements.Count(x => !x.IsNull() && x.EntityType == type && x.OnScene);
    public long GetExactEntityInPoolCount(Enumeration type) => elements.Count(x => !x.IsNull() && x.EntityType == type && x.InPool);
    //TODO:
    public long GetInheritedExistsEntityCount(Type type) => elements.Count(x => !x.IsNull() && type.IsAssignableFrom(x.GetType()));

    public long GetInheritedEntityOnSceneCount(Type type) => elements.Count(x => !x.IsNull() && type.IsAssignableFrom(x.GetType()) && x.OnScene);

    public long GetInheritedEntityInPoolCount(Type type) => elements.Count(x => !x.IsNull() && type.IsAssignableFrom(x.GetType()) && x.InPool);

    /// <summary>
    /// Количество зарегистрированных сущностей для каждого EntityType.
    /// </summary>
    private Dictionary<Enumeration, long> registeredEntity = new Dictionary<Enumeration, long>();

    /// <summary>
    /// Предоставляет доступ только для чтения к статистике регистраций по типам.
    /// </summary>
    public IReadOnlyDictionary<Enumeration, long> RegisteredEntity => registeredEntity;

    #endregion

    /// <summary>
    /// Регистрирует живую сущность, если она ещё не присутствует в трекере.
    /// </summary>
    public override bool Register(IEntity entity)
    {
        if (entity == null || entity.IsNull() || elements.Contains(entity))
            return false;

        entity.GenerateId(EntityIdGenerator.Instance.RegisterId);
        elements.Add(entity);
        RegisterEntityType(entity.EntityType);

        //if (gameSessionManager.Settings.isActiveDebugLog)
        //    PRLog.WriteDebug(this, $"Сущность {entity.EntityType} - ID:{entity.Id} зарегистрирована в entityTracker.");

        return true;
    }

    /// <summary>
    /// Удаляет сущность и уменьшает счётчик её типа.
    /// </summary>
    public override bool Unregister(IEntity entity)
    {
        if (entity == null || !elements.Remove(entity))
            return false;

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
        if (currentEntitiesCount <= 1)
            registeredEntity.Remove(type);
        else
            registeredEntity[type] = currentEntitiesCount - 1;
    }

    public long GetRegisteredEntityCount(Enumeration type)
    {
        long currentEntitiesCount = 0;
        registeredEntity.TryGetValue(type, out currentEntitiesCount);
        return currentEntitiesCount;
    }

    /// <summary>
    /// Полностью уничтожает все живые сущности и сбрасывает статистику.
    /// </summary>
    public override void Clear()
    {
        foreach (var entity in elements.ToList())
        {
            if (entity == null || entity.IsNull())
            {
                elements.Remove(entity);
                continue;
            }

            entity.DestroyEntity(new EntityDestroyOptions() { FullDestroy = true });
            Unregister(entity);
        }

        registeredEntity.Clear();
    }
}
