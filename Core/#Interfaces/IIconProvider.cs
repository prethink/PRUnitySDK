using UnityEngine;

/// <summary>
/// Определяет интерфейс для предметов, которые имеют иконку.
/// Используется для отображения предмета в UI, инвентаре, магазинах и т.п.
/// </summary>
public interface IIconProvider
{
    /// <summary>
    /// Иконка, визуально представляющая предмет.
    /// </summary>
    Sprite Icon { get; }
}
