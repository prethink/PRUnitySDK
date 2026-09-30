#if PRSDK_TESTS
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class EntityTrackerSearchTests
{
    private EntityTracker entities;
    private PlayerTracker players;
    private EntityTracker previousEntities;
    private PlayerTracker previousPlayers;
    private readonly List<EntityMetadata> metadataAssets = new();
    private readonly List<GameObject> gameObjects = new();

    [SetUp]
    public void SetUp()
    {
        previousEntities = EntityService.Instance;
        previousPlayers = PlayerService.Instance;
        entities = new EntityTracker();
        players = new PlayerTracker();
        EntityService.Override(entities);
        PlayerService.Override(players);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var gameObject in gameObjects)
            UnityEngine.Object.DestroyImmediate(gameObject);
        gameObjects.Clear();
        EntityService.Override(previousEntities);
        PlayerService.Override(previousPlayers);
        foreach (var metadata in metadataAssets)
            UnityEngine.Object.DestroyImmediate(metadata);
        metadataAssets.Clear();
    }

    [Test]
    public void DefaultSearch_IncludesHiddenAndPooledEntities()
    {
        var visible = Add(new TestEntity());
        var hidden = Add(new TestEntity { OnScene = false });
        var pooled = Add(new TestEntity { OnScene = false, InPool = true });
        CollectionAssert.AreEqual(new[] { visible, hidden, pooled }, entities.GetEntities());
        entities.GetEntities().Clear();
        Assert.AreEqual(3, entities.GetEntitiesCount());
    }

    [TestCase(EntitySearchFlags.None, 4)]
    [TestCase(EntitySearchFlags.OnScene, 3)]
    [TestCase(EntitySearchFlags.InPool, 1)]
    [TestCase(EntitySearchFlags.NotInPool, 3)]
    [TestCase(EntitySearchFlags.Visible, 2)]
    [TestCase(EntitySearchFlags.Hidden, 0)]
    [TestCase(EntitySearchFlags.NotVisible, 2)]
    [TestCase(EntitySearchFlags.Alive, 2)]
    [TestCase(EntitySearchFlags.Dead, 1)]
    [TestCase(EntitySearchFlags.Visible | EntitySearchFlags.Alive, 1)]
    [TestCase(EntitySearchFlags.Visible | EntitySearchFlags.Hidden, 0)]
    [TestCase(EntitySearchFlags.Alive | EntitySearchFlags.Dead, 0)]
    [TestCase(EntitySearchFlags.InPool | EntitySearchFlags.NotInPool, 0)]
    public void Flags_FilterStateAndCountsAgree(EntitySearchFlags flags, int expected)
    {
        Add(NewHealthEntity(true));
        Add(NewHealthEntity(false));
        Add(NewHealthEntity(true, inPool: true));
        Add(new TestEntity { OnScene = false });
        Assert.AreEqual(expected, entities.GetEntities(flags).Count);
        Assert.AreEqual(expected, entities.GetEntitiesCount(flags));
    }

    [Test]
    public void StateChanges_AreReadAtSearchTime()
    {
        var entity = Add(NewHealthEntity(true));
        Assert.AreEqual(1, entities.GetEntitiesCount(EntitySearchFlags.Visible | EntitySearchFlags.Alive));
        entity.Alive = false;
        Assert.AreEqual(0, entities.GetEntitiesCount(EntitySearchFlags.Alive));
        Assert.AreEqual(1, entities.GetEntitiesCount(EntitySearchFlags.Dead));
        entity.InPool = true;
        Assert.AreEqual(0, entities.GetEntitiesCount(EntitySearchFlags.Visible));
    }

    [Test]
    public void HealthFilter_RequiresProviderWithExistingHealth()
    {
        var gameObject = new GameObject("EntityTracker unprovided health test");
        gameObjects.Add(gameObject);
        var entity = gameObject.AddComponent<Entity>();
        gameObject.AddComponent<HealthComponent>().InitHealth();
        Assert.IsTrue(entities.Register(entity));
        Add(new TestHealthEntity());
        Assert.IsEmpty(entities.GetEntities(EntitySearchFlags.Alive));
        Assert.IsEmpty(entities.GetEntities(EntitySearchFlags.Dead));

        var provider = Add(NewHealthEntity(true));
        UnityEngine.Object.DestroyImmediate(provider.Health);
        Assert.IsEmpty(entities.GetEntities(EntitySearchFlags.Alive));
        Assert.IsEmpty(entities.GetEntities(EntitySearchFlags.Dead));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PlayerInitialization_ProvidesOneProtectedHealthComponent(bool healthAlreadyExists)
    {
        var gameObject = new GameObject("EntityTracker protected player test");
        gameObjects.Add(gameObject);
        var player = gameObject.AddComponent<EntityTrackerPlayerSearchTestEntity>();
        var existing = healthAlreadyExists ? gameObject.AddComponent<HealthComponent>() : null;
        player.InitializeForTest(NewMetadata());
        IHealthProvider provider = player;
        Assert.IsNotNull(provider.Health);
        if (existing != null)
            Assert.AreSame(existing, provider.Health);
        Assert.AreEqual(1, gameObject.GetComponents<HealthComponent>().Length);
        Assert.IsTrue(provider.Health.IsImmortal);
        Assert.IsTrue(provider.Health.IsBlockDamage);
        Assert.IsTrue(provider.Health.IsAlive());
        Assert.IsTrue(entities.Register(player));
        CollectionAssert.AreEqual(new[] { player }, entities.GetEntities(EntitySearchFlags.Visible | EntitySearchFlags.Alive));
    }

    [Test]
    public void ImmortalPlayer_RequiresForceToKillAndCanRevive()
    {
        var gameObject = new GameObject("EntityTracker forced player death test");
        gameObjects.Add(gameObject);
        var player = gameObject.AddComponent<EntityTrackerPlayerSearchTestEntity>();
        player.InitializeForTest(NewMetadata());
        entities.Register(player);
        var health = player.Health;
        int deaths = 0;
        int healthChanges = 0;
        health.OnEntityDead += (_, _) => deaths++;
        health.OnHealthChange += _ => healthChanges++;

        DamageOutcome blocked = health.Kill(player);
        Assert.AreEqual(DamageResult.Blocked, blocked.Result);
        Assert.IsFalse(blocked.WasApplied);
        Assert.AreEqual(health.MaxHealth, health.Health);
        Assert.IsTrue(health.IsAlive());
        Assert.AreEqual(0, deaths);
        Assert.AreEqual(0, healthChanges);
        Assert.IsNull(health.Killer);

        DamageOutcome killed = health.Kill(player, force: true);
        Assert.AreEqual(DamageResult.Killed, killed.Result);
        Assert.IsFalse(health.IsAlive());
        Assert.AreEqual(0, health.Health);
        Assert.AreSame(player, health.Killer);
        Assert.AreEqual(1, deaths);
        Assert.AreEqual(1, healthChanges);
        Assert.AreEqual(1, entities.GetEntitiesCount(EntitySearchFlags.Dead));
        Assert.AreEqual(DamageResult.NotHandled, health.Kill(player, force: true).Result);
        Assert.AreEqual(1, deaths);

        health.Revive(null, health.MaxHealth, Vector3.zero, Quaternion.identity);
        Assert.IsTrue(health.IsAlive());
        Assert.IsNull(health.Killer);
        Assert.IsTrue(health.IsImmortal);
        Assert.IsTrue(health.IsBlockDamage);
        Assert.AreEqual(1, entities.GetEntitiesCount(EntitySearchFlags.Alive));
        Assert.AreEqual(0, entities.GetEntitiesCount(EntitySearchFlags.Dead));
    }

    [Test]
    public void Kill_RespectsImmortalityWithoutSourceAndIgnoresAttackBlock()
    {
        var entity = NewHealthEntity(true);
        var health = entity.Health;
        health.SetOverrideIsAlive(null);
        health.InitHealth();
        health.SetBlockDamage(true).SetImmortal(true);
        Assert.AreEqual(DamageResult.Blocked, health.Kill().Result);
        Assert.AreEqual(DamageResult.Killed, health.Kill(force: true).Result);

        health.InitHealth();
        health.SetImmortal(false);
        Assert.AreEqual(DamageResult.Killed, health.Kill(null).Result);
    }

    [Test]
    public void Metadata_MatchesBaseAndOverrideByReference()
    {
        var first = NewMetadata();
        var second = NewMetadata();
        var unrelated = NewMetadata();
        var entity = Add(new TestEntity { Description = new EntityDescription(first, second) });
        CollectionAssert.AreEqual(new[] { entity }, entities.GetEntities(first));
        CollectionAssert.AreEqual(new[] { entity }, entities.GetEntities(second));
        Assert.IsEmpty(entities.GetEntities(unrelated));
        Assert.AreEqual(1, entities.GetEntitiesCount(first));
    }

    [Test]
    public void HideFilter_FollowsDestroyAndRestoration()
    {
        var gameObject = new GameObject("EntityTracker hide test");
        var entity = gameObject.AddComponent<Entity>();
        try
        {
            var dispose = new EnumerationReference<EntityDisposeEnumerations>();
            dispose.Set(EntityDisposeEnumerations.Hide);
            typeof(EntityBase).GetField("EntityDisposeAction",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(entity, dispose);
            entities.Register(entity);
            Assert.AreEqual(0, entities.GetEntitiesCount(EntitySearchFlags.Hidden));
            entity.DestroyEntity();
            Assert.AreEqual(1, entities.GetEntitiesCount(EntitySearchFlags.Hide));
            Assert.AreEqual(1, entities.GetEntitiesCount(EntitySearchFlags.Hidden));
            Assert.AreEqual(1, entities.GetEntitiesCount(EntitySearchFlags.Hide | EntitySearchFlags.HideWire));
            Assert.AreEqual(0, entities.GetEntitiesCount(EntitySearchFlags.HideWire | EntitySearchFlags.HideWirePolygons));
            entity.RestoreHideEvent(null);
            Assert.AreEqual(1, entities.GetEntitiesCount(EntitySearchFlags.Visible));
            Assert.AreEqual(0, entities.GetEntitiesCount(EntitySearchFlags.Hidden));
        }
        finally
        {
            entities.Unregister(entity);
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    [TestCase(EntitySearchFlags.Hide)]
    [TestCase(EntitySearchFlags.HideWire)]
    [TestCase(EntitySearchFlags.HideWirePolygons)]
    public void HideFilter_UsesCurrentDisposeActionAndActivity(EntitySearchFlags flag)
    {
        var gameObject = new GameObject("EntityTracker dispose filter test");
        var entity = gameObject.AddComponent<Entity>();
        try
        {
            var dispose = new EnumerationReference<EntityDisposeEnumerations>();
            dispose.Set(flag == EntitySearchFlags.Hide ? EntityDisposeEnumerations.Hide
                : flag == EntitySearchFlags.HideWire ? EntityDisposeEnumerations.HideWire
                : EntityDisposeEnumerations.HideWirePolygons);
            typeof(EntityBase).GetField("EntityDisposeAction",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(entity, dispose);
            entities.Register(entity);
            Assert.IsEmpty(entities.GetEntities(EntitySearchFlags.Hidden));

            gameObject.SetActive(false);
            CollectionAssert.AreEqual(new[] { entity }, entities.GetEntities(flag));
            Assert.AreEqual(1, entities.GetEntitiesCount(EntitySearchFlags.Hidden));
            Assert.AreEqual(0, entities.GetEntitiesCount(EntitySearchFlags.Hidden & ~flag));
            Assert.AreEqual(1, entities.GetEntitiesCount(flag | EntitySearchFlags.Hide));
            Assert.AreEqual(0, entities.GetEntitiesCount(flag | EntitySearchFlags.Visible));

            dispose.Set(EntityDisposeEnumerations.Destroy);
            Assert.IsEmpty(entities.GetEntities(EntitySearchFlags.Hidden));
            CollectionAssert.AreEqual(new[] { entity }, entities.GetEntities(EntitySearchFlags.NotVisible));
        }
        finally
        {
            entities.Unregister(entity);
            UnityEngine.Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void DefinitionAndConfiguredMetadata_BothFindTheEntity()
    {
        var metadata = NewMetadata();
        var firstDefinition = NewMetadata();
        var secondDefinition = NewMetadata();
        var firstObject = new GameObject("First definition entity");
        var secondObject = new GameObject("Second definition entity");
        var first = firstObject.AddComponent<EntityDefinitionSearchTestEntity>();
        var second = secondObject.AddComponent<EntityDefinitionSearchTestEntity>();
        try
        {
            first.ConfigureForTest(metadata, firstDefinition);
            second.ConfigureForTest(metadata, secondDefinition);
            entities.Register(first);
            entities.Register(second);
            CollectionAssert.AreEqual(new[] { first }, entities.GetEntities(firstDefinition));
            CollectionAssert.AreEqual(new[] { second }, entities.GetEntities(secondDefinition));
            CollectionAssert.AreEqual(new[] { first, second }, entities.GetEntities(metadata));
            Assert.AreEqual(1, entities.GetEntitiesCount(firstDefinition, EntitySearchFlags.Visible));
            firstObject.SetActive(false);
            Assert.AreEqual(0, entities.GetEntitiesCount(firstDefinition, EntitySearchFlags.Visible));
            Assert.AreEqual(1, entities.GetEntitiesCount(firstDefinition, EntitySearchFlags.NotVisible));
        }
        finally
        {
            entities.Unregister(first);
            entities.Unregister(second);
            UnityEngine.Object.DestroyImmediate(firstObject);
            UnityEngine.Object.DestroyImmediate(secondObject);
        }
    }

    [Test]
    public void EntityTypeAndMetadata_CanBeCombined()
    {
        var metadata = NewMetadata();
        var expected = Add(new TestEntity { EntityType = EntityTypeEnumerations.Box,
            Description = new EntityDescription(metadata) });
        Add(new TestEntity { Description = new EntityDescription(metadata) });
        Add(new TestEntity { EntityType = EntityTypeEnumerations.Box });
        CollectionAssert.AreEqual(new[] { expected },
            entities.GetEntities(EntityTypeEnumerations.Box, metadata: metadata));
        Assert.AreEqual(1, entities.GetEntitiesCount(EntityTypeEnumerations.Box, metadata: metadata));
        Assert.AreEqual(2, entities.GetEntitiesCount(EntityTypeEnumerations.Box));
    }

    [Test]
    public void GenericSearch_IncludesDerivedTypes()
    {
        Add(new TestEntity());
        var derived = Add(NewHealthEntity(true));
        CollectionAssert.AreEqual(new[] { derived }, entities.GetEntities<TestHealthEntity>());
        Assert.AreEqual(2, entities.GetEntities<TestEntity>().Count);
        Assert.AreEqual(1, entities.GetEntitiesCount<TestHealthEntity>(EntitySearchFlags.Alive));
    }

    [Test]
    public void PlayerRegistration_UsesBothTrackersAndOneEntityId()
    {
        var player = new TestPlayer();
        Assert.IsTrue(players.Register(player));
        Assert.IsTrue(entities.Contains(player));
        Assert.IsTrue(players.Contains(player));
        Assert.AreEqual(1, player.EntityIdAssignments);
        Assert.IsFalse(players.Register(player));
        Assert.IsFalse(entities.Register(player));
        Assert.AreEqual(1, player.EntityIdAssignments);
        Assert.AreEqual(1, entities.GetEntitiesCount(EntityTypeEnumerations.Player));
        Assert.AreEqual(1, entities.GetRegisteredEntityCount(EntityTypeEnumerations.Player));
    }

    [Test]
    public void AlreadyRegisteredEntity_KeepsIdWhenJoiningPlayerTracker()
    {
        var player = Add(new TestPlayer());
        long id = player.Id;
        Assert.IsTrue(players.Register(player));
        Assert.AreEqual(id, player.Id);
        Assert.AreEqual(1, player.EntityIdAssignments);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RemovingPlayerFromEitherTracker_RemovesBothAndReusesPlayerId(bool throughEntities)
    {
        var first = new TestPlayer();
        players.Register(first);
        long playerId = first.PlayerId;
        if (throughEntities)
            entities.Unregister(first);
        else
            players.Unregister(first);
        Assert.IsFalse(entities.Contains(first));
        Assert.IsFalse(players.Contains(first));
        Assert.AreEqual(0, entities.GetRegisteredEntityCount(EntityTypeEnumerations.Player));
        Assert.IsFalse(players.Unregister(first));
        var second = new TestPlayer();
        players.Register(second);
        Assert.AreEqual(playerId, second.PlayerId);
        Assert.AreNotEqual(first.Id, second.Id);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ClearingBothTrackers_DestroysEachObjectOnce(bool entitiesFirst)
    {
        var entity = Add(new TestEntity());
        var player = new TestPlayer();
        players.Register(player);
        if (entitiesFirst)
        {
            entities.Clear();
            players.Clear();
        }
        else
        {
            players.Clear();
            entities.Clear();
        }
        Assert.AreEqual(1, entity.DestroyCalls);
        Assert.AreEqual(1, player.DestroyCalls);
        Assert.AreEqual(0, entities.GetEntitiesCount());
        Assert.AreEqual(0, players.PlayersCount);
        Assert.IsEmpty(entities.RegisteredEntity);
    }

    [Test]
    public void NullIdentityFilter_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() => entities.GetEntities((Enumeration)null));
        Assert.Throws<ArgumentNullException>(() => entities.GetEntities((IEntityMetadata)null));
        Assert.IsFalse(entities.Register(null));
    }

    private T Add<T>(T entity) where T : TestEntity
    {
        Assert.IsTrue(entities.Register(entity));
        return entity;
    }

    private TestHealthEntity NewHealthEntity(bool alive, bool inPool = false)
    {
        var gameObject = new GameObject("EntityTracker health test");
        gameObjects.Add(gameObject);
        gameObject.AddComponent<Entity>();
        var health = gameObject.AddComponent<HealthComponent>();
        var entity = new TestHealthEntity { Health = health, Alive = alive, InPool = inPool };
        health.SetOverrideIsAlive(() => entity.Alive);
        return entity;
    }

    private EntityMetadata NewMetadata()
    {
        var metadata = ScriptableObject.CreateInstance<EntityMetadata>();
        metadataAssets.Add(metadata);
        return metadata;
    }

    private class TestEntity : IEntity
    {
        public long Id { get; private set; }
        public int EntityIdAssignments { get; private set; }
        public int DestroyCalls { get; private set; }
        public EntityDescription Description { get; set; }
        public Enumeration EntityType { get; set; } = EntityTypeEnumerations.Common;
        public bool OnScene { get; set; } = true;
        public bool InPool { get; set; }
        public GameObject gameObject => null;
        public void GenerateId(Func<long> register) { Id = register(); EntityIdAssignments++; }
        public void DestroyEntity() => DestroyEntity(new EntityDestroyOptions());
        public void DestroyEntity(EntityDestroyOptions options) { DestroyCalls++; }
        public Enumeration GetTimeScaleLayer() => PRTimeScaleEnumerations.Global;
    }

    private class TestHealthEntity : TestEntity, IHealthProvider
    {
        public HealthComponent Health { get; set; }
        public bool Alive { get; set; }
    }

    private class TestPlayer : TestEntity, IPlayer
    {
        public TestPlayer() { EntityType = EntityTypeEnumerations.Player; }
        public long PlayerId { get; private set; }
        public long Points => 0;
        public int Deaths => 0;
        public int Kills => 0;
        public PlayerType PlayerType => PlayerType.AI;
        public IPlayerTeam PlayerTeam => null;
        public IPlayerStats PlayerStats => null;
        public int HumanId => -1;
        public void GeneratePlayerId(Func<long> register) { PlayerId = register(); }
        public void SetNick(string name) { }
        public void SetTeam(IPlayerTeam team) { }
        public bool AddPlayerItem(IPlayerItem item) => false;
    }
}
#endif
