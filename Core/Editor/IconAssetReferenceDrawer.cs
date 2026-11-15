using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Поле ссылки на ассет с нашей иконкой: награду, предмет, предложение магазина, эмоцию.
/// </summary>
/// <remarks>
/// Стандартное поле показывает у любого ScriptableObject один и тот же значок, и награды
/// в списке различаются только именем файла. Здесь рядом с полем стоит сама иконка — та же,
/// что видит игрок.
/// <para>
/// Привязан к <see cref="ScriptableObject"/>, а не к конкретным типам: иконку отдают через
/// <see cref="IIconProvider"/> десяток несвязанных типов, часть — в модулях, которые
/// отключаются, и ядро не может ссылаться на них по имени. Поле со ссылкой на ассет
/// без иконки рисуется как обычно. Свой рисовальщик типа или атрибут поля
/// (<see cref="IconPreviewAttribute"/>, <see cref="InlineAssetAttribute"/>) главнее этого.
/// </para>
/// </remarks>
[CustomPropertyDrawer(typeof(ScriptableObject), true)]
public sealed class IconAssetReferenceDrawer : PropertyDrawer
{
    private const float IconSize = 36f;
    private const float Gap = 4f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return HasIconSlot(property, GetReferenceType())
            ? IconSize
            : EditorGUIUtility.singleLineHeight;
    }

    /// <summary>
    /// Нужно ли полю место под иконку.
    /// </summary>
    /// <remarks>
    /// Решает тип поля, а не то, нашлась ли иконка: в списке наград строка без иконки
    /// была бы ниже соседних, а её поле уезжало бы влево, и колонка рвалась. Пустое
    /// место заодно показывает, у какой награды иконки нет.
    /// </remarks>
    private static bool HasIconSlot(SerializedProperty property, Type type)
    {
        if (property.propertyType != SerializedPropertyType.ObjectReference)
            return false;

        return typeof(IIconProvider).IsAssignableFrom(type)
            || property.objectReferenceValue is IIconProvider;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Type type = GetReferenceType();

        if (property.propertyType != SerializedPropertyType.ObjectReference)
        {
            EditorGUI.LabelField(position, label.text, "Только для ссылок на ассеты");
            return;
        }

        if (!HasIconSlot(property, type))
        {
            EditorGUI.ObjectField(position, property, type, label);
            return;
        }

        Sprite icon = IconPreviewDrawer.ResolveIcon(property);

        label = EditorGUI.BeginProperty(position, label, property);

        // Подпись и поле стоят по середине строки, чтобы читаться на одной линии с иконкой.
        float lineY = position.y + (IconSize - EditorGUIUtility.singleLineHeight) * 0.5f;
        var line = new Rect(position.x, lineY, position.width, EditorGUIUtility.singleLineHeight);
        Rect content = label == GUIContent.none || string.IsNullOrEmpty(label.text)
            ? EditorGUI.IndentedRect(line)
            : EditorGUI.PrefixLabel(line, label);

        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        var iconRect = new Rect(content.x, position.y, IconSize, IconSize);
        var fieldRect = new Rect(
            iconRect.xMax + Gap,
            lineY,
            Mathf.Max(40f, content.xMax - iconRect.xMax - Gap),
            EditorGUIUtility.singleLineHeight);

        // Иконка тоже часть поля: на неё щёлкают, чтобы найти ассет в проекте.
        if (Event.current.type == EventType.MouseDown
            && Event.current.button == 0
            && iconRect.Contains(Event.current.mousePosition))
        {
            EditorGUIUtility.PingObject(property.objectReferenceValue);
            Event.current.Use();
        }

        if (icon != null)
            IconPreviewDrawer.DrawSprite(iconRect, icon);
        else
            DrawEmptySlot(iconRect, property.objectReferenceValue != null);
        EditorGUI.ObjectField(fieldRect, property, type, GUIContent.none);

        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }

    /// <summary>
    /// Рисует место под иконку, которой нет.
    /// </summary>
    /// <param name="assigned">Ассет выбран, но иконку не отдал.</param>
    private static void DrawEmptySlot(Rect rect, bool assigned)
    {
        if (Event.current.type != EventType.Repaint)
            return;

        Rect inner = new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f);
        EditorGUI.DrawRect(inner, new Color(0f, 0f, 0f, 0.18f));

        if (!assigned)
            return;

        GUI.Label(
            inner,
            // Не «нет»: иконка бывает вычисляемой и появляется только в игре — у награды-яйца
            // StealEgg она берётся по текущему яйцу игрока.
            new GUIContent("?", "Иконки нет в редакторе: не задана в ассете или вычисляется в игре."),
            EditorStyles.centeredGreyMiniLabel);
    }

    /// <summary>
    /// Тип, которым поле ограничивает выбор ассета: элемента, если поле — список.
    /// </summary>
    private Type GetReferenceType()
    {
        Type type = fieldInfo?.FieldType;

        if (type == null)
            return typeof(ScriptableObject);

        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            return type.GetGenericArguments()[0];

        return type;
    }
}
