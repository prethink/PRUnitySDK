using System;

/// <summary>
/// Условия поиска сущностей. Hide-режимы выбираются как альтернативы, остальные флаги пересекаются.
/// </summary>
[Flags]
public enum EntitySearchFlags
{
    /// <summary>
    /// Все существующие зарегистрированные сущности без ограничения состояния.
    /// </summary>
    None = 0,

    /// <summary>
    /// IEntity.OnScene возвращает true.
    /// </summary>
    OnScene = 1 << 0,

    /// <summary>
    /// Сущность находится в пуле.
    /// </summary>
    InPool = 1 << 1,

    /// <summary>
    /// Сущность находится вне пула.
    /// </summary>
    NotInPool = 1 << 2,

    /// <summary>
    /// Сущность активна в иерархии и находится вне пула.
    /// </summary>
    Visible = 1 << 3,

    /// <summary>
    /// Неактивная сущность вне пула с настроенным действием Hide.
    /// </summary>
    Hide = 1 << 4,

    /// <summary>
    /// Неактивная сущность вне пула с настроенным действием HideWire.
    /// </summary>
    HideWire = 1 << 5,

    /// <summary>
    /// Неактивная сущность вне пула с настроенным действием HideWirePolygons.
    /// </summary>
    HideWirePolygons = 1 << 6,

    /// <summary>
    /// Неактивная сущность вне пула с любым из трёх настроенных Hide-действий.
    /// </summary>
    Hidden = Hide | HideWire | HideWirePolygons,

    /// <summary>
    /// Сущность со здоровьем, для которой IsAlive() возвращает true.
    /// </summary>
    Alive = 1 << 7,

    /// <summary>
    /// Сущность со здоровьем, для которой IsAlive() возвращает false.
    /// </summary>
    Dead = 1 << 8,

    /// <summary>
    /// Сущность не удовлетворяет Visible, включая отключённые объекты и пул.
    /// </summary>
    NotVisible = 1 << 9
}
