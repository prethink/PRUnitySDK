using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Рисует матрицу сторон треугольником, как матрицу столкновений слоёв в Unity.
/// </summary>
/// <remarks>
/// Клетка симметрична, поэтому показана одна половина. Нажатие по клетке переключает
/// «бьёт» → «без урона» → «не бьёт». «Бьёт» не хранится: это значение по умолчанию.
/// </remarks>
[CustomPropertyDrawer(typeof(EntitySideMatrix))]
public class EntitySideMatrixDrawer : PropertyDrawer
{
    private const float LabelWidth = 110f;
    private const float CellWidth = 76f;
    private const float Spacing = 2f;

    private static readonly Color HitColor = new(0.45f, 0.85f, 0.45f);
    private static readonly Color NoDamageColor = new(1f, 0.85f, 0.3f);
    private static readonly Color BlockColor = new(1f, 0.45f, 0.4f);

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        return (line + Spacing) * (GetSides().Count + 2);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        List<Enumeration> sides = GetSides();
        SerializedProperty cells = property.FindPropertyRelative("cells");
        float line = EditorGUIUtility.singleLineHeight;

        Rect area = EditorGUI.IndentedRect(position);
        float y = area.y;

        EditorGUI.LabelField(new Rect(area.x, y, area.width, line), label, EditorStyles.boldLabel);
        y += line + Spacing;

        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        for (int column = 0; column < sides.Count; column++)
            GUI.Label(CellRect(area.x, y, column, line), sides[column].Value, EditorStyles.centeredGreyMiniLabel);

        y += line + Spacing;

        for (int row = 0; row < sides.Count; row++)
        {
            GUI.Label(new Rect(area.x, y, LabelWidth, line), sides[row].Value, EditorStyles.miniLabel);

            for (int column = 0; column <= row; column++)
                DrawCell(CellRect(area.x, y, column, line), cells, sides[row], sides[column]);

            y += line + Spacing;
        }

        EditorGUI.indentLevel = indent;
    }

    private static Rect CellRect(float x, float y, int column, float height)
    {
        return new Rect(x + LabelWidth + column * (CellWidth + Spacing), y, CellWidth, height);
    }

    private static void DrawCell(Rect rect, SerializedProperty cells, Enumeration first, Enumeration second)
    {
        EntitySideDamage damage = Get(cells, first.Value, second.Value);

        (string text, Color color) = damage switch
        {
            EntitySideDamage.NoDamage => ("Без урона", NoDamageColor),
            EntitySideDamage.Block => ("Не бьёт", BlockColor),
            _ => ("Бьёт", HitColor)
        };

        var content = new GUIContent(text, $"{first.Value} ↔ {second.Value}: {text}. Нажмите, чтобы сменить.");
        Color previous = GUI.backgroundColor;
        GUI.backgroundColor = color;

        if (GUI.Button(rect, content, EditorStyles.miniButton))
            Set(cells, first.Value, second.Value, Next(damage));

        GUI.backgroundColor = previous;
    }

    private static EntitySideDamage Next(EntitySideDamage damage)
    {
        return damage switch
        {
            EntitySideDamage.Hit => EntitySideDamage.NoDamage,
            EntitySideDamage.NoDamage => EntitySideDamage.Block,
            _ => EntitySideDamage.Hit
        };
    }

    private static EntitySideDamage Get(SerializedProperty cells, string first, string second)
    {
        for (int i = 0; i < cells.arraySize; i++)
        {
            SerializedProperty cell = cells.GetArrayElementAtIndex(i);

            if (Matches(cell, first, second))
                return (EntitySideDamage)cell.FindPropertyRelative(nameof(EntitySideMatrix.Cell.Damage)).intValue;
        }

        return EntitySideDamage.Hit;
    }

    private static void Set(SerializedProperty cells, string first, string second, EntitySideDamage damage)
    {
        for (int i = cells.arraySize - 1; i >= 0; i--)
        {
            if (Matches(cells.GetArrayElementAtIndex(i), first, second))
                cells.DeleteArrayElementAtIndex(i);
        }

        if (damage == EntitySideDamage.Hit)
            return;

        cells.arraySize++;
        SerializedProperty added = cells.GetArrayElementAtIndex(cells.arraySize - 1);
        added.FindPropertyRelative(nameof(EntitySideMatrix.Cell.First)).stringValue = first;
        added.FindPropertyRelative(nameof(EntitySideMatrix.Cell.Second)).stringValue = second;
        added.FindPropertyRelative(nameof(EntitySideMatrix.Cell.Damage)).intValue = (int)damage;
    }

    private static bool Matches(SerializedProperty cell, string first, string second)
    {
        string a = cell.FindPropertyRelative(nameof(EntitySideMatrix.Cell.First)).stringValue;
        string b = cell.FindPropertyRelative(nameof(EntitySideMatrix.Cell.Second)).stringValue;

        return (a == first && b == second) || (a == second && b == first);
    }

    private static List<Enumeration> GetSides()
    {
        return EnumerationReference<EntitySideEnumerations>.GetOptions().Where(side => side != null).ToList();
    }
}
