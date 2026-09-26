#if PRSDK_TESTS
using NUnit.Framework;

/// <summary>
/// Решение «свой-чужой»: урон, враг, команды и матрица.
/// </summary>
/// <remarks>
/// Проверяется чистое решение (<see cref="EntitySides.ResolveDamage"/>,
/// <see cref="EntitySides.ResolveEnemy"/>) со своими настройками, без сцены и ассета проекта.
/// </remarks>
public class EntitySidesTests
{
    private static readonly Enumeration Players = EntitySideEnumerations.Players;
    private static readonly Enumeration Bots = EntitySideEnumerations.Bots;
    private static readonly Enumeration Breakables = EntitySideEnumerations.Breakables;

    private EntitySidesSettings settings;

    [SetUp]
    public void SetUp()
    {
        settings = new EntitySidesSettings();
    }

    [Test]
    public void EmptyMatrix_EveryoneHitsEveryone()
    {
        Assert.AreEqual(EntitySideDamage.Hit, Damage(Players, Bots));
        Assert.AreEqual(EntitySideDamage.Hit, Damage(Bots, Breakables));
    }

    [Test]
    public void MissingOrDisabledSettings_AlwaysHit()
    {
        settings.Matrix.Set(Players, Bots, EntitySideDamage.Block);
        settings.SetEnabled(false);

        Assert.AreEqual(EntitySideDamage.Hit, Damage(Players, Bots));
        Assert.AreEqual(EntitySideDamage.Hit,
            EntitySides.ResolveDamage(null, Players, Bots, EntityTeamRelation.None));
    }

    [Test]
    public void MatrixCell_IsSymmetric()
    {
        settings.Matrix.Set(Bots, Breakables, EntitySideDamage.NoDamage);

        Assert.AreEqual(EntitySideDamage.NoDamage, Damage(Bots, Breakables));
        Assert.AreEqual(EntitySideDamage.NoDamage, Damage(Breakables, Bots));
    }

    [Test]
    public void MatrixCell_SetToHit_RemovesCell()
    {
        settings.Matrix.Set(Players, Bots, EntitySideDamage.Block);
        settings.Matrix.Set(Bots, Players, EntitySideDamage.Hit);

        Assert.AreEqual(EntitySideDamage.Hit, Damage(Players, Bots));
    }

    [Test]
    public void SameTeam_BlockedWithoutFriendlyFire_HitWithIt()
    {
        Assert.AreEqual(EntitySideDamage.Block, Damage(Players, Players, EntityTeamRelation.SameTeam));

        settings.SetFriendlyFire(true);

        Assert.AreEqual(EntitySideDamage.Hit, Damage(Players, Players, EntityTeamRelation.SameTeam));
    }

    [Test]
    public void Teams_OverrideMatrix()
    {
        settings.Matrix.Set(Players, Bots, EntitySideDamage.Block);

        Assert.AreEqual(EntitySideDamage.Hit, Damage(Players, Bots, EntityTeamRelation.OtherTeam));
    }

    [Test]
    public void Teammate_IsNotEnemy_EvenWithFriendlyFire()
    {
        settings.SetFriendlyFire(true);

        Assert.IsFalse(Enemy(Players, Players, EntityTeamRelation.SameTeam));
    }

    [Test]
    public void OtherTeam_IsEnemy_EvenIfMatrixBlocks()
    {
        settings.Matrix.Set(Players, Bots, EntitySideDamage.Block);

        Assert.IsTrue(Enemy(Players, Bots, EntityTeamRelation.OtherTeam));
    }

    [Test]
    public void WithoutTeams_EnemyOnlyWhenHitDealsDamage()
    {
        settings.Matrix.Set(Players, Bots, EntitySideDamage.NoDamage);
        settings.Matrix.Set(Players, Breakables, EntitySideDamage.Block);

        Assert.IsFalse(Enemy(Players, Bots));
        Assert.IsFalse(Enemy(Players, Breakables));
        Assert.IsTrue(Enemy(Bots, Breakables));
    }

    [Test]
    public void EntityTypeSide_MappedOrDefault()
    {
        settings.SetEntityTypeSide(EntityTypeEnumerations.Box, Breakables);

        Assert.AreEqual(Breakables, settings.GetEntityTypeSide(EntityTypeEnumerations.Box));
        Assert.AreEqual(EntitySideEnumerations.Neutral, settings.GetEntityTypeSide(EntityTypeEnumerations.Common));
    }

    [Test]
    public void EntityTypeSide_SetAgain_Replaces()
    {
        settings.SetEntityTypeSide(EntityTypeEnumerations.Box, Breakables);
        settings.SetEntityTypeSide(EntityTypeEnumerations.Box, EntitySideEnumerations.Monsters);

        Assert.AreEqual(EntitySideEnumerations.Monsters, settings.GetEntityTypeSide(EntityTypeEnumerations.Box));
    }

    [Test]
    public void PlayerSide_ByPlayerType()
    {
        Assert.AreEqual(Players, settings.GetPlayerSide(PlayerType.Human));
        Assert.AreEqual(Bots, settings.GetPlayerSide(PlayerType.AI));
        Assert.AreEqual(EntitySideEnumerations.Npc, settings.GetPlayerSide(PlayerType.NPC));
    }

    private EntitySideDamage Damage(Enumeration attacker, Enumeration victim,
        EntityTeamRelation team = EntityTeamRelation.None)
    {
        return EntitySides.ResolveDamage(settings, attacker, victim, team);
    }

    private bool Enemy(Enumeration first, Enumeration second, EntityTeamRelation team = EntityTeamRelation.None)
    {
        return EntitySides.ResolveEnemy(settings, first, second, team);
    }
}
#endif
