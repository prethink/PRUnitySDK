#if PRSDK_TESTS
using NUnit.Framework;
using UnityEngine.TestTools;

/// <summary>
/// Передача записи создаваемой сущности: запись получает ровно одна сущность — та, что вернулась.
/// </summary>
public class EntityInstanceSpawnTests
{
    private const string Definition = "pickaxe";

    [Test]
    public void EntityBornInsideCreate_GetsRecordBeforeInitialization()
    {
        EntityInstanceData record = EntityInstanceData.Create(Definition);
        var entity = new FakeEntity(Definition);

        FakeEntity created = EntityInstanceSpawn.Create(record, () =>
        {
            entity.BeginLife();
            return entity;
        });

        Assert.AreSame(entity, created);
        Assert.AreSame(record, created.Instance);
        Assert.AreSame(record, created.InstanceSeenByInitialization);
    }

    [Test]
    public void EntityOfAnotherDefinition_DoesNotTakeRecord()
    {
        EntityInstanceData record = EntityInstanceData.Create(Definition);
        var child = new FakeEntity("handle");
        var entity = new FakeEntity(Definition);

        EntityInstanceSpawn.Create(record, () =>
        {
            // Дочерняя сущность префаба просыпается раньше корня.
            child.BeginLife();
            entity.BeginLife();
            return entity;
        });

        Assert.AreNotSame(record, child.Instance);
        Assert.AreSame(record, entity.Instance);
    }

    /// <remarks>
    /// Итоговая принадлежность верна, но инициализацию обе сущности прошли не со своей
    /// записью: об этом пишется предупреждение, а пересчёт — обязанность SetInstance.
    /// Случай вложенной сущности, где этого можно избежать, проверяют
    /// <see cref="NestedEntity_DoesNotTakeRecord"/> и EntityInstanceLifeTests.
    /// </remarks>
    [Test]
    public void RecordTakenByAnotherEntity_ReturnsToCreatedOne()
    {
        LogAssert.ignoreFailingMessages = true;

        EntityInstanceData record = EntityInstanceData.Create(Definition);
        var sibling = new FakeEntity(Definition);
        var entity = new FakeEntity(Definition);

        EntityInstanceSpawn.Create(record, () =>
        {
            // Сосед того же определения начал жизнь первым и забрал запись.
            sibling.BeginLife();
            entity.BeginLife();
            return entity;
        });

        Assert.AreSame(record, entity.Instance);
        Assert.AreNotSame(record, sibling.Instance);
        Assert.AreNotEqual(record.InstanceId, sibling.Instance.InstanceId);
    }

    [Test]
    public void NestedEntity_DoesNotTakeRecord()
    {
        EntityInstanceData record = EntityInstanceData.Create(Definition);
        var inner = new FakeEntity(Definition);
        var outer = new FakeEntity(Definition);

        EntityInstanceSpawn.Create(record, () =>
        {
            // Вложенная сущность того же определения просыпается раньше внешней.
            inner.BeginLife(nested: true);
            outer.BeginLife();
            return outer;
        });

        Assert.AreSame(record, outer.Instance);
        Assert.AreSame(record, outer.InstanceSeenByInitialization);
        Assert.AreNotSame(record, inner.InstanceSeenByInitialization);
    }

    [Test]
    public void EntityOfAnotherDefinitionReturned_DoesNotGetRecord()
    {
        LogAssert.ignoreFailingMessages = true;

        EntityInstanceData record = EntityInstanceData.Create(Definition);
        var pet = new FakeEntity("pet");

        FakeEntity returned = EntityInstanceSpawn.Create(record, () => pet);

        Assert.AreSame(pet, returned);
        Assert.AreNotSame(record, pet.Instance);
    }

    [Test]
    public void EntityNotBornInsideCreate_GetsRecordAfter()
    {
        EntityInstanceData record = EntityInstanceData.Create(Definition);
        var entity = new FakeEntity(Definition);

        // Объект создан выключенным: жизнь внутри create не началась.
        EntityInstanceSpawn.Create(record, () => entity);

        Assert.AreSame(record, entity.Instance);
    }

    [Test]
    public void RecordIsNotLeftForNextEntity()
    {
        EntityInstanceData record = EntityInstanceData.Create(Definition);
        var entity = new FakeEntity(Definition);
        var next = new FakeEntity(Definition);

        EntityInstanceSpawn.Create(record, () => entity);
        next.BeginLife();

        Assert.AreNotSame(record, next.Instance);
    }

    [Test]
    public void FailedCreate_DoesNotLeaveRecord()
    {
        EntityInstanceData record = EntityInstanceData.Create(Definition);
        var next = new FakeEntity(Definition);

        Assert.Throws<System.InvalidOperationException>(() =>
            EntityInstanceSpawn.Create<FakeEntity>(record, () => throw new System.InvalidOperationException()));

        next.BeginLife();

        Assert.AreNotSame(record, next.Instance);
    }

    [Test]
    public void NestedCreate_KeepsOuterRecord()
    {
        EntityInstanceData outerRecord = EntityInstanceData.Create(Definition);
        EntityInstanceData innerRecord = EntityInstanceData.Create(Definition);
        var outer = new FakeEntity(Definition);
        var inner = new FakeEntity(Definition);

        EntityInstanceSpawn.Create(outerRecord, () =>
        {
            EntityInstanceSpawn.Create(innerRecord, () =>
            {
                inner.BeginLife();
                return inner;
            });

            outer.BeginLife();
            return outer;
        });

        Assert.AreSame(innerRecord, inner.Instance);
        Assert.AreSame(outerRecord, outer.Instance);
    }

    /// <summary>
    /// Сущность без Unity: начало жизни повторяет то, что делает <c>EntityBase</c>.
    /// </summary>
    private sealed class FakeEntity : IEntityInstance
    {
        private readonly string definitionId;

        public FakeEntity(string definitionId)
        {
            this.definitionId = definitionId;
        }

        public EntityInstanceData Instance { get; private set; }

        public EntityInstanceData InstanceSeenByInitialization { get; private set; }

        public EntityInstanceData CreateInstance()
        {
            return EntityInstanceData.Create(definitionId);
        }

        public void SetInstance(EntityInstanceData data)
        {
            Instance = data;
        }

        public void BeginLife(bool nested = false)
        {
            EntityInstanceData fresh = CreateInstance();

            SetInstance(EntityInstanceSpawn.Take(fresh.DefinitionId, this, nested) ?? fresh);
            InstanceSeenByInitialization = Instance;
        }
    }
}
#endif
