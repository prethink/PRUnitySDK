#if PRSDK_TESTS
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Запись экземпляра на настоящей сущности: выключенный объект, вложенная сущность,
/// повторная выдача из пула, занятый экземпляр и чужое определение.
/// </summary>
public class EntityInstanceLifeTests
{
    private const string Definition = "pickaxe";

    private readonly List<Object> created = new();

    private EntityTracker previousEntities;
    private EntityTracker entities;
    private EntityMetadata metadata;

    [SetUp]
    public void SetUp()
    {
        previousEntities = EntityService.Instance;
        entities = new EntityTracker();
        EntityService.Override(entities);

        metadata = ScriptableObject.CreateInstance<EntityMetadata>();
        created.Add(metadata);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (Object item in created)
            Object.DestroyImmediate(item);

        created.Clear();
        EntityService.Override(previousEntities);
    }

    [Test]
    public void InactiveObject_KeepsRecordSetBeforeAwake()
    {
        EntityInstanceData record = EntityInstanceData.Create(Definition);
        EntityInstanceTestEntity entity = Add(Definition, active: false);

        // Объект создан выключенным: Awake ещё не было, запись ставится следом за созданием.
        EntityInstanceSpawn.Create(record, () => entity);
        Assert.AreSame(record, entity.Instance);

        // Объект включили: Awake не должен заменить запись новой.
        entity.AwakeForTest(metadata);

        Assert.AreSame(record, entity.Instance);
        Assert.AreSame(record, entity.InstanceSeenByInitialization);
    }

    [Test]
    public void NestedEntityOfSameDefinition_DoesNotTakeRecord()
    {
        EntityInstanceData record = EntityInstanceData.Create(Definition);
        EntityInstanceTestEntity outer = Add(Definition);
        EntityInstanceTestEntity inner = Add(Definition, parent: outer.transform);

        EntityInstanceSpawn.Create(record, () =>
        {
            // Вложенная сущность просыпается раньше внешней.
            inner.AwakeForTest(metadata);
            outer.AwakeForTest(metadata);
            return outer;
        });

        Assert.AreSame(record, outer.Instance);
        Assert.AreSame(record, outer.InstanceSeenByInitialization);
        Assert.AreNotSame(record, inner.Instance);
        Assert.AreNotSame(record, inner.InstanceSeenByInitialization);
    }

    [Test]
    public void ShownFromPoolAgain_GetsNewRecord()
    {
        EntityInstanceData record = EntityInstanceData.Create(Definition);
        EntityInstanceTestEntity entity = Add(Definition);

        EntityInstanceSpawn.Create(record, () =>
        {
            entity.AwakeForTest(metadata);
            return entity;
        });

        entity.ShowFromPoolForTest();

        Assert.AreNotSame(record, entity.Instance);
        Assert.AreNotEqual(record.InstanceId, entity.Instance.InstanceId);
    }

    [Test]
    public void ShownFromPoolWithRecord_GetsThatRecordBeforeInitialization()
    {
        EntityInstanceData saved = EntityInstanceData.Create(Definition);
        EntityInstanceTestEntity entity = Add(Definition);
        entity.AwakeForTest(metadata);

        EntityInstanceSpawn.Create(saved, () =>
        {
            entity.ShowFromPoolForTest();
            return entity;
        });

        Assert.AreSame(saved, entity.Instance);
        Assert.AreSame(saved, entity.InstanceSeenByInitialization);
    }

    [Test]
    public void InstanceAlreadyOnScene_IsNotCreatedAgain()
    {
        LogAssert.ignoreFailingMessages = true;

        EntityInstanceData record = EntityInstanceData.Create(Definition);
        EntityInstanceTestEntity owner = Add(Definition);

        EntityInstanceSpawn.Create(record, () =>
        {
            owner.AwakeForTest(metadata);
            return owner;
        });

        entities.Register(owner);

        bool factoryCalled = false;

        EntityInstanceTestEntity second = EntityInstanceSpawn.Create(record, () =>
        {
            factoryCalled = true;
            return Add(Definition);
        });

        Assert.IsNull(second);
        Assert.IsFalse(factoryCalled);
        Assert.AreSame(record, owner.Instance);
    }

    [Test]
    public void EntityOfAnotherDefinition_DoesNotGetRecord()
    {
        LogAssert.ignoreFailingMessages = true;

        EntityInstanceData record = EntityInstanceData.Create(Definition);
        EntityInstanceTestEntity pet = Add("pet", active: false);

        EntityInstanceTestEntity returned = EntityInstanceSpawn.Create(record, () => pet);

        Assert.AreSame(pet, returned);
        Assert.AreNotSame(record, pet.Instance);
    }

    private EntityInstanceTestEntity Add(string definitionId, bool active = true, Transform parent = null)
    {
        var gameObject = new GameObject("EntityInstanceTest");
        created.Add(gameObject);

        if (parent != null)
            gameObject.transform.SetParent(parent, false);

        gameObject.SetActive(active);

        var entity = gameObject.AddComponent<EntityInstanceTestEntity>();
        entity.DefinitionId = definitionId;

        return entity;
    }
}
#endif
