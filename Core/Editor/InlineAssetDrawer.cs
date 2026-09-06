using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Рисует поле-ссылку вместе с полями самого ассета.
/// </summary>
/// <remarks>
/// Поля берутся у присвоенного экземпляра через отдельный <see cref="SerializedObject"/>,
/// а не у типа поля — поэтому наследник показывает свои поля, а не урезанные базовые.
/// Правки сразу применяются к ассету и попадают в Undo как обычные изменения инспектора.
/// </remarks>
[CustomPropertyDrawer(typeof(InlineAssetAttribute))]
public class InlineAssetDrawer : PropertyDrawer
{
    private const float Spacing = 2f;
    private const float BoxPadding = 4f;

    /// <summary>
    /// Ассет может ссылаться на другой ассет с тем же атрибутом. Ограничение глубины
    /// не даёт инспектору уйти в бесконечную вложенность при взаимных ссылках.
    /// </summary>
    private const int MaxDepth = 3;

    private static int depth;

    private readonly Dictionary<int, SerializedObject> nestedObjects = new();

    /// <summary>
    /// Пути, для которых уже применено состояние раскрытия из атрибута.
    /// </summary>
    private readonly HashSet<string> defaultsApplied = new();

    /// <inheritdoc />
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;

        if (!CanDrawInline(property))
            return height;

        ApplyDefaultExpanded(property);

        // Свёрнутое поле занимает одну строку. Без этой проверки высота считалась по
        // всем полям ассета всегда, и под свёрнутой ссылкой зияла пустота, а поля
        // компонента, идущие ниже, уезжали за пределы видимой области инспектора.
        if (!property.isExpanded)
            return height;

        SerializedObject nested = GetNested(property.objectReferenceValue);
        if (nested == null)
            return height;

        nested.UpdateIfRequiredOrScript();

        height += Spacing + BoxPadding;

        SerializedProperty iterator = nested.GetIterator();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (IsHidden(iterator))
                continue;

            height += EditorGUI.GetPropertyHeight(iterator, true) + Spacing;
        }

        if (((InlineAssetAttribute)attribute).ShowAssetPath)
            height += EditorGUIUtility.singleLineHeight + Spacing;

        return height + BoxPadding;
    }

    /// <inheritdoc />
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var settings = (InlineAssetAttribute)attribute;

        var fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        if (!CanDrawInline(property))
        {
            EditorGUI.PropertyField(fieldRect, property, label);
            return;
        }

        DrawFoldoutField(fieldRect, property, label);

        if (!property.isExpanded)
            return;

        SerializedObject nested = GetNested(property.objectReferenceValue);
        if (nested == null)
            return;

        float contentHeight = position.height - EditorGUIUtility.singleLineHeight - Spacing;
        var boxRect = new Rect(position.x, fieldRect.yMax + Spacing, position.width, contentHeight);
        GUI.Box(boxRect, GUIContent.none, EditorStyles.helpBox);

        depth++;

        try
        {
            DrawNested(boxRect, nested, settings, property.objectReferenceValue);
        }
        finally
        {
            depth--;
        }
    }

    /// <summary>
    /// Рисует ссылку так, чтобы подпись поля работала треугольником раскрытия.
    /// </summary>
    private static void DrawFoldoutField(Rect rect, SerializedProperty property, GUIContent label)
    {
        float labelWidth = EditorGUIUtility.labelWidth;

        var labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
        var valueRect = new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, rect.height);

        property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);
        EditorGUI.PropertyField(valueRect, property, GUIContent.none);
    }

    /// <summary>
    /// Рисует поля ассета и, если нужно, путь к нему.
    /// </summary>
    private void DrawNested(Rect boxRect, SerializedObject nested, InlineAssetAttribute settings, Object asset)
    {
        nested.UpdateIfRequiredOrScript();

        float y = boxRect.y + BoxPadding;
        float width = boxRect.width - BoxPadding * 2f;
        float x = boxRect.x + BoxPadding;

        SerializedProperty iterator = nested.GetIterator();
        bool enterChildren = true;

        EditorGUI.BeginChangeCheck();

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (IsHidden(iterator))
                continue;

            float height = EditorGUI.GetPropertyHeight(iterator, true);
            EditorGUI.PropertyField(new Rect(x, y, width, height), iterator, true);
            y += height + Spacing;
        }

        if (EditorGUI.EndChangeCheck())
            nested.ApplyModifiedProperties();

        if (!settings.ShowAssetPath)
            return;

        string path = AssetDatabase.GetAssetPath(asset);
        string note = string.IsNullOrEmpty(path)
            ? "Значения общие для всех, кто ссылается на этот объект."
            : $"Общий ассет: {path}";

        EditorGUI.LabelField(
            new Rect(x, y, width, EditorGUIUtility.singleLineHeight),
            note,
            EditorStyles.miniLabel);
    }

    /// <summary>
    /// Поля ассета показываются, когда есть что показывать и это никого не запутает.
    /// </summary>
    /// <remarks>
    /// При множественном выделении ассеты у объектов разные, и один общий блок полей
    /// показывал бы значения только первого — поэтому там остаётся обычное поле.
    /// </remarks>
    private static bool CanDrawInline(SerializedProperty property)
    {
        return property.propertyType == SerializedPropertyType.ObjectReference
            && property.objectReferenceValue != null
            && !property.serializedObject.isEditingMultipleObjects
            && depth < MaxDepth;
    }

    /// <summary>
    /// Строку скрипта не показываем: у ассета он не меняется, а место занимает.
    /// </summary>
    private static bool IsHidden(SerializedProperty property)
    {
        return property.propertyPath == "m_Script";
    }

    /// <summary>
    /// Раскрывает поле при первой отрисовке, если атрибут просит.
    /// </summary>
    /// <remarks>
    /// Дальше состоянием управляет пользователь: свернуть можно как обычно, и до
    /// перестроения инспектора оно останется свёрнутым.
    /// </remarks>
    private void ApplyDefaultExpanded(SerializedProperty property)
    {
        if (!((InlineAssetAttribute)attribute).Expanded)
            return;

        if (!defaultsApplied.Add(property.propertyPath))
            return;

        property.isExpanded = true;
    }

    private SerializedObject GetNested(Object asset)
    {
        if (asset == null)
            return null;

        int key = asset.GetInstanceID();

        if (nestedObjects.TryGetValue(key, out SerializedObject cached) && cached.targetObject != null)
            return cached;

        var created = new SerializedObject(asset);
        nestedObjects[key] = created;
        return created;
    }
}
