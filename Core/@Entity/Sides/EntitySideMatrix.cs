using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Матрица сторон: что происходит с ударом одной стороны по другой.
/// </summary>
/// <remarks>
/// Симметричная, как матрица столкновений слоёв в Unity: «игроки → боты» и «боты → игроки»
/// — одна клетка. Хранятся только клетки, отличные от <see cref="EntitySideDamage.Hit"/>:
/// пустая матрица — «все бьют всех», и новая сторона ничего не ломает, пока её не настроят.
/// </remarks>
[Serializable]
public class EntitySideMatrix
{
    /// <summary>
    /// Клетка матрицы. Пара сторон хранится значениями перечисления, порядок в паре не важен.
    /// </summary>
    [Serializable]
    public class Cell
    {
        public string First;
        public string Second;
        public EntitySideDamage Damage;

        public bool Is(string first, string second)
        {
            return (First == first && Second == second) || (First == second && Second == first);
        }
    }

    [SerializeField] private List<Cell> cells = new();

    /// <summary>
    /// Что происходит с ударом стороны <paramref name="first"/> по стороне <paramref name="second"/>.
    /// </summary>
    public EntitySideDamage Get(Enumeration first, Enumeration second)
    {
        if (first == null || second == null)
            return EntitySideDamage.Hit;

        foreach (Cell cell in cells)
        {
            if (cell != null && cell.Is(first.Value, second.Value))
                return cell.Damage;
        }

        return EntitySideDamage.Hit;
    }

    /// <summary>
    /// Задаёт клетку. <see cref="EntitySideDamage.Hit"/> клетку убирает: это значение по умолчанию.
    /// </summary>
    public void Set(Enumeration first, Enumeration second, EntitySideDamage damage)
    {
        if (first == null || second == null)
            return;

        cells.RemoveAll(cell => cell == null || cell.Is(first.Value, second.Value));

        if (damage != EntitySideDamage.Hit)
            cells.Add(new Cell { First = first.Value, Second = second.Value, Damage = damage });
    }
}
