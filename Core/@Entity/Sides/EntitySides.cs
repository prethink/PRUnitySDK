using System;

/// <summary>
/// Свой-чужой: на чьей стороне сущность, что будет с ударом одной по другой и враги ли они.
/// </summary>
/// <remarks>
/// Одно место для вопросов «можно ли бить» и «враг ли»: урон спрашивает первое через
/// правило (<see cref="EntitySideDamageRule"/>), выбор целей — второе.
/// <para>
/// Это разные вопросы. «Можно ударить» — про урон: при <see cref="EntitySidesSettings.FriendlyFire"/>
/// союзника ударить можно, но врагом он от этого не становится, и бот, выбирающий цели,
/// не должен идти на своих.
/// </para>
/// <para>
/// Порядок ответа:
/// <list type="number">
/// <item>нет второй сущности или это она сама — обычный удар, не враг: это не вопрос сторон;</item>
/// <item>оба игроки в командах (не <c>Default</c>) — решает команда;</item>
/// <item>иначе — матрица сторон.</item>
/// </list>
/// Само решение — <see cref="ResolveDamage"/> и <see cref="ResolveEnemy"/>: они не трогают
/// ни сущностей, ни настроек проекта, и их можно спрашивать с любыми настройками.
/// </para>
/// </remarks>
public static class EntitySides
{
    private static readonly Guid DefaultTeamGuid = Guid.Parse(TeamGuids.DefaultTeamGuid);

    private static EntitySidesSettings Settings => PRUnitySDK.Settings != null ? PRUnitySDK.Settings.Sides : null;

    #region Сущности

    /// <summary>
    /// Сторона сущности.
    /// </summary>
    /// <remarks>
    /// <see cref="EntitySideOverride"/> на объекте сильнее всего; у игрока — сторона его типа
    /// (человек, бот, NPC); у остальных — сторона вида сущности.
    /// </remarks>
    /// <returns><c>null</c>, если сущности нет.</returns>
    public static Enumeration GetSide(IEntity entity)
    {
        if (entity.IsNull())
            return null;

        if (entity.gameObject.TryGetComponent(out EntitySideOverride sideOverride) && sideOverride.Side != null)
            return sideOverride.Side;

        EntitySidesSettings settings = Settings;

        if (settings == null)
            return EntitySideEnumerations.Neutral;

        return entity is IPlayer player
            ? settings.GetPlayerSide(player.PlayerType)
            : settings.GetEntityTypeSide(entity.EntityType);
    }

    /// <summary>
    /// Что будет с ударом <paramref name="attacker"/> по <paramref name="victim"/>.
    /// </summary>
    public static EntitySideDamage GetDamage(IEntity attacker, IEntity victim)
    {
        if (attacker.IsNull() || victim.IsNull() || ReferenceEquals(attacker, victim))
            return EntitySideDamage.Hit;

        return ResolveDamage(Settings, GetSide(attacker), GetSide(victim), GetTeamRelation(attacker, victim));
    }

    /// <summary>
    /// Может ли удар <paramref name="attacker"/> по <paramref name="victim"/> хотя бы засчитаться.
    /// </summary>
    /// <remarks>
    /// Удар без урона тоже засчитан: по цели можно бить, она просто цела.
    /// </remarks>
    public static bool CanHit(IEntity attacker, IEntity victim)
    {
        return GetDamage(attacker, victim) != EntitySideDamage.Block;
    }

    /// <summary>
    /// Враг ли <paramref name="other"/> для <paramref name="entity"/>.
    /// </summary>
    /// <remarks>
    /// По нему выбирают цели. Союзник по команде врагом не бывает, даже когда friendly fire
    /// позволяет его ударить.
    /// </remarks>
    public static bool IsEnemy(IEntity entity, IEntity other)
    {
        if (entity.IsNull() || other.IsNull() || ReferenceEquals(entity, other))
            return false;

        return ResolveEnemy(Settings, GetSide(entity), GetSide(other), GetTeamRelation(entity, other));
    }

    /// <summary>
    /// Игроки в одной команде. Команда <c>Default</c> — «без команды», и такие игроки
    /// союзниками по команде не считаются.
    /// </summary>
    public static bool IsSameTeam(IEntity first, IEntity second)
    {
        return GetTeamRelation(first, second) == EntityTeamRelation.SameTeam;
    }

    /// <summary>
    /// Как пара связана командами.
    /// </summary>
    public static EntityTeamRelation GetTeamRelation(IEntity first, IEntity second)
    {
        if (first is not IPlayer firstPlayer || second is not IPlayer secondPlayer ||
            !HasTeam(firstPlayer) || !HasTeam(secondPlayer))
            return EntityTeamRelation.None;

        return firstPlayer.PlayerTeam.Guid == secondPlayer.PlayerTeam.Guid
            ? EntityTeamRelation.SameTeam
            : EntityTeamRelation.OtherTeam;
    }

    private static bool HasTeam(IPlayer player)
    {
        return player.PlayerTeam != null && player.PlayerTeam.Guid != DefaultTeamGuid;
    }

    #endregion

    #region Решение

    /// <summary>
    /// Что будет с ударом стороны по стороне при заданных настройках и командах.
    /// </summary>
    /// <remarks>
    /// Выключенные или отсутствующие настройки — обычный удар: стороны ничего не решают.
    /// </remarks>
    public static EntitySideDamage ResolveDamage(EntitySidesSettings settings,
        Enumeration attackerSide, Enumeration victimSide, EntityTeamRelation team)
    {
        if (settings == null || !settings.Enabled)
            return EntitySideDamage.Hit;

        return team switch
        {
            EntityTeamRelation.SameTeam => settings.FriendlyFire ? EntitySideDamage.Hit : EntitySideDamage.Block,
            EntityTeamRelation.OtherTeam => EntitySideDamage.Hit,
            _ => settings.Matrix.Get(attackerSide, victimSide)
        };
    }

    /// <summary>
    /// Враги ли стороны при заданных настройках и командах.
    /// </summary>
    /// <remarks>
    /// Команды решают сами: своя — не враг (friendly fire не в счёт), чужая — враг.
    /// Без команд враг тот, по кому удар наносит урон.
    /// </remarks>
    public static bool ResolveEnemy(EntitySidesSettings settings,
        Enumeration firstSide, Enumeration secondSide, EntityTeamRelation team)
    {
        return team switch
        {
            EntityTeamRelation.SameTeam => false,
            EntityTeamRelation.OtherTeam => true,
            _ => ResolveDamage(settings, firstSide, secondSide, EntityTeamRelation.None) == EntitySideDamage.Hit
        };
    }

    #endregion
}
