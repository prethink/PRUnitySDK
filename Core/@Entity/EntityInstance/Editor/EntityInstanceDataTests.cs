#if PRSDK_TESTS
using Newtonsoft.Json;
using NUnit.Framework;

/// <summary>
/// Запись экземпляра: цикл сохранения, снимок и самостоятельная копия.
/// </summary>
public class EntityInstanceDataTests
{
    private static readonly EnumerationType<long> Level = new("Level");
    private static readonly EnumerationType<string> Skin = new("Skin");

    [Test]
    public void SaveAndLoad_KeepsIdentityAndValues()
    {
        EntityInstanceData record = EntityInstanceData.Create("definition");
        record.Set(Level, 7L);
        record.Set(Skin, "gold");

        string json = JsonConvert.SerializeObject(record);
        EntityInstanceData loaded = JsonConvert.DeserializeObject<EntityInstanceData>(json);

        Assert.AreEqual(record.InstanceId, loaded.InstanceId);
        Assert.AreEqual("definition", loaded.DefinitionId);
        Assert.AreEqual(7L, loaded.Get(Level));
        Assert.AreEqual("gold", loaded.Get(Skin));
    }

    [Test]
    public void Clone_IsSameInstanceWithOwnValues()
    {
        EntityInstanceData record = EntityInstanceData.Create("definition");
        record.Set(Level, 3L);

        var snapshot = (EntityInstanceData)record.Clone();
        record.Set(Level, 9L);

        Assert.AreEqual(record.InstanceId, snapshot.InstanceId);
        Assert.AreEqual(3L, snapshot.Get(Level));
    }

    [Test]
    public void Duplicate_IsAnotherInstanceWithSameValues()
    {
        EntityInstanceData record = EntityInstanceData.Create("definition");
        record.Set(Level, 3L);

        EntityInstanceData copy = record.Duplicate();
        copy.Set(Level, 5L);

        Assert.AreNotEqual(record.InstanceId, copy.InstanceId);
        Assert.AreEqual("definition", copy.DefinitionId);
        Assert.AreEqual(3L, record.Get(Level));
        Assert.AreEqual(5L, copy.Get(Level));
    }

    [Test]
    public void Changed_IsRaisedOnlyWhenValueChanges()
    {
        EntityInstanceData record = EntityInstanceData.Create("definition");
        int raised = 0;
        record.Changed += () => raised++;

        record.Set(Level, 2L);
        record.Set(Level, 2L);
        record.Set(Level, 3L);
        record.Remove(Level);
        record.Remove(Level);

        Assert.AreEqual(3, raised);
    }

    [Test]
    public void Changed_DoesNotPassToCopies()
    {
        EntityInstanceData record = EntityInstanceData.Create("definition");
        int raised = 0;
        record.Changed += () => raised++;

        ((EntityInstanceData)record.Clone()).Set(Level, 2L);
        record.Duplicate().Set(Level, 2L);

        Assert.AreEqual(0, raised);
    }

    [Test]
    public void IsValid_NeedsBothIdentifiers()
    {
        Assert.IsTrue(EntityInstanceData.Create("definition").IsValid());
        Assert.IsFalse(EntityInstanceData.Create(null).IsValid());
        Assert.IsFalse(new EntityInstanceData { DefinitionId = "definition" }.IsValid());
    }

    [Test]
    public void Fits_ComparesDefinition()
    {
        EntityInstanceData record = EntityInstanceData.Create("definition");

        Assert.IsTrue(record.Fits("definition"));
        Assert.IsFalse(record.Fits("another"));
        Assert.IsFalse(record.Fits(null));
    }
}
#endif
