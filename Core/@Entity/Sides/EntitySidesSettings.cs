using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Настройки «свой-чужой»: стороны сущностей, матрица ударов и команды.
/// </summary>
[Serializable]
[SettingsDescription("Свой-чужой: кто кого бьёт. Сущность получает сторону по типу игрока или виду " +
                     "(переопределяет EntitySideOverride), матрица решает, что с ударом стороны по стороне. " +
                     "Игроки в командах (не Default) решаются командой: свои не бьют друг друга, чужие бьют.")]
public class EntitySidesSettings
{
    /// <summary>
    /// Вид сущности и его сторона.
    /// </summary>
    [Serializable]
    public class EntityTypeSide
    {
        public EnumerationReference<EntityTypeEnumerations> EntityType = new();
        public EnumerationReference<EntitySideEnumerations> Side = new();
    }

    [field: SerializeField]
    [field: Tooltip("Правило сторон работает: удары проверяются матрицей и командами.")]
    public bool Enabled { get; private set; } = true;

    [field: SerializeField]
    [field: Tooltip("Игроки одной команды бьют друг друга. Команда Default — «без команды», её решает матрица.")]
    public bool FriendlyFire { get; private set; }

    [Header("Стороны игроков")]
    [SerializeField] private EnumerationReference<EntitySideEnumerations> humanSide = Side(EntitySideEnumerations.Players);
    [SerializeField] private EnumerationReference<EntitySideEnumerations> aiSide = Side(EntitySideEnumerations.Bots);
    [SerializeField] private EnumerationReference<EntitySideEnumerations> npcSide = Side(EntitySideEnumerations.Npc);

    [Header("Стороны остальных сущностей")]
    [SerializeField]
    [Tooltip("Сторона по виду сущности. Не нашлось — Default Side.")]
    private List<EntityTypeSide> entityTypeSides = new();

    [SerializeField]
    [Tooltip("Сторона всего, что не сопоставлено.")]
    private EnumerationReference<EntitySideEnumerations> defaultSide = Side(EntitySideEnumerations.Neutral);

    [field: SerializeField]
    [field: Tooltip("Что происходит с ударом стороны по стороне. Клетка симметрична; по умолчанию — «бьёт».")]
    public EntitySideMatrix Matrix { get; private set; } = new();

    /// <summary>
    /// Сторона игрока по его типу.
    /// </summary>
    public Enumeration GetPlayerSide(PlayerType type)
    {
        EnumerationReference<EntitySideEnumerations> side = type switch
        {
            PlayerType.Human => humanSide,
            PlayerType.AI => aiSide,
            _ => npcSide
        };

        return side?.ToEnumeration() ?? DefaultSide;
    }

    /// <summary>
    /// Сторона по виду сущности или <see cref="DefaultSide"/>.
    /// </summary>
    public Enumeration GetEntityTypeSide(Enumeration entityType)
    {
        if (entityType != null)
        {
            foreach (EntityTypeSide entry in entityTypeSides)
            {
                if (entry?.EntityType != null && entry.EntityType.Value == entityType.Value)
                    return entry.Side?.ToEnumeration() ?? DefaultSide;
            }
        }

        return DefaultSide;
    }

    /// <summary>
    /// Сторона всего, что не сопоставлено.
    /// </summary>
    public Enumeration DefaultSide => defaultSide?.ToEnumeration() ?? EntitySideEnumerations.Neutral;

    /// <summary>
    /// Включает или выключает стороны — например, на время режима без правил.
    /// </summary>
    public EntitySidesSettings SetEnabled(bool value)
    {
        Enabled = value;
        return this;
    }

    /// <summary>
    /// Разрешает или запрещает игрокам одной команды бить друг друга.
    /// </summary>
    public EntitySidesSettings SetFriendlyFire(bool value)
    {
        FriendlyFire = value;
        return this;
    }

    /// <summary>
    /// Сопоставляет виду сущности сторону, заменяя прежнее сопоставление.
    /// </summary>
    public EntitySidesSettings SetEntityTypeSide(Enumeration entityType, Enumeration side)
    {
        if (entityType == null)
            return this;

        entityTypeSides.RemoveAll(entry => entry?.EntityType != null && entry.EntityType.Value == entityType.Value);

        var added = new EntityTypeSide();
        added.EntityType.Set(entityType);
        added.Side.Set(side);
        entityTypeSides.Add(added);

        return this;
    }

    private static EnumerationReference<EntitySideEnumerations> Side(Enumeration value)
    {
        var reference = new EnumerationReference<EntitySideEnumerations>();
        reference.Set(value);
        return reference;
    }
}
