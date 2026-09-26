using System;

/// <summary>
/// Свой-чужой: на чьей стороне сущность и что будет с ударом одной по другой.
/// </summary>
/// <remarks>
/// Одно место для вопроса «можно ли бить»: урон спрашивает его через правило
/// (<see cref="EntitySideDamageRule"/>), выбор целей — напрямую.
/// <para>
/// Порядок ответа на удар:
/// <list type="number">
/// <item>нет атакующего или бьёт сам себя — обычный удар: это не вопрос сторон;</item>
/// <item>оба игроки в командах (не <c>Default</c>) — решает команда: свои не бьют друг
/// друга (или бьют при <see cref="EntitySidesSettings.FriendlyFire"/>), чужие бьют;</item>
/// <item>иначе — матрица сторон.</item>
/// </list>
/// </para>
/// </remarks>
public static class EntitySides
{
    private static readonly Guid DefaultTeamGuid = Guid.Parse(TeamGuids.DefaultTeamGuid);

    private static EntitySidesSettings Settings => PRUnitySDK.Settings != null ? PRUnitySDK.Settings.Sides : null;

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
        EntitySidesSettings settings = Settings;

        if (settings == null || !settings.Enabled || attacker.IsNull() || victim.IsNull() || ReferenceEquals(attacker, victim))
            return EntitySideDamage.Hit;

        if (TryGetTeamRelation(attacker, victim, out bool sameTeam))
            return !sameTeam || settings.FriendlyFire ? EntitySideDamage.Hit : EntitySideDamage.Block;

        return settings.Matrix.Get(GetSide(attacker), GetSide(victim));
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
    /// Враг ли <paramref name="other"/> для <paramref name="entity"/>: удар по нему наносит урон.
    /// </summary>
    /// <remarks>
    /// По нему выбирают цели: бот атакует врагов, а не всё, во что можно попасть.
    /// </remarks>
    public static bool IsEnemy(IEntity entity, IEntity other)
    {
        return !ReferenceEquals(entity, other) && GetDamage(entity, other) == EntitySideDamage.Hit;
    }

    /// <summary>
    /// Игроки в одной команде. Команда <c>Default</c> — «без команды», и такие игроки
    /// союзниками по команде не считаются.
    /// </summary>
    public static bool IsSameTeam(IEntity first, IEntity second)
    {
        return TryGetTeamRelation(first, second, out bool sameTeam) && sameTeam;
    }

    /// <summary>
    /// Решается ли пара командами: оба — игроки в командах, не <c>Default</c>.
    /// </summary>
    private static bool TryGetTeamRelation(IEntity first, IEntity second, out bool sameTeam)
    {
        sameTeam = false;

        if (first is not IPlayer firstPlayer || second is not IPlayer secondPlayer ||
            !HasTeam(firstPlayer) || !HasTeam(secondPlayer))
            return false;

        sameTeam = firstPlayer.PlayerTeam.Guid == secondPlayer.PlayerTeam.Guid;
        return true;
    }

    private static bool HasTeam(IPlayer player)
    {
        return player.PlayerTeam != null && player.PlayerTeam.Guid != DefaultTeamGuid;
    }
}
