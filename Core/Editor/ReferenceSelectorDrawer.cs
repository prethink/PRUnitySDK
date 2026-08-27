using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Рисует поле <c>[SerializeReference]</c> с выбором конкретной реализации.
/// </summary>
[CustomPropertyDrawer(typeof(ReferenceSelectorAttribute))]
public class ReferenceSelectorDrawer : PropertyDrawer
{
    private const string NoneLabel = "None";

    /// <summary>
    /// Подходящие реализации по базовому типу.
    /// Набор типов неизменен до перезагрузки домена, поэтому ищется один раз.
    /// </summary>
    private static readonly Dictionary<Type, Type[]> implementationsCache = new();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.HelpBox(position,
                $"{nameof(ReferenceSelectorAttribute)} требует поле с [SerializeReference].",
                MessageType.Error);
            return;
        }

        EditorGUI.BeginProperty(position, label, property);

        Rect header = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        DrawHeader(header, property, label);

        if (property.managedReferenceValue != null)
        {
            EditorGUI.indentLevel++;
            DrawChildren(position, property);
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;

        if (property.propertyType != SerializedPropertyType.ManagedReference ||
            property.managedReferenceValue == null)
        {
            return height;
        }

        foreach (SerializedProperty child in EnumerateChildren(property))
            height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;

        return height;
    }

    /// <summary>
    /// Рисует строку с названием поля и кнопкой выбора типа.
    /// </summary>
    private void DrawHeader(Rect rect, SerializedProperty property, GUIContent label)
    {
        Rect labelRect = new(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
        Rect buttonRect = new(rect.x + EditorGUIUtility.labelWidth, rect.y,
            rect.width - EditorGUIUtility.labelWidth, rect.height);

        EditorGUI.LabelField(labelRect, label);

        var selector = (ReferenceSelectorAttribute)attribute;
        string current = GetTypeLabel(property.managedReferenceValue?.GetType(), selector.ShowFullName);

        if (!EditorGUI.DropdownButton(buttonRect, new GUIContent(current), FocusType.Keyboard))
            return;

        ShowTypeMenu(property, selector);
    }

    private void ShowTypeMenu(SerializedProperty property, ReferenceSelectorAttribute selector)
    {
        Type baseType = GetManagedReferenceFieldType(property);
        if (baseType == null)
            return;

        // Копия пути: меню отрабатывает отложенно, а исходный SerializedProperty
        // к тому моменту уже может указывать на другой элемент списка.
        string propertyPath = property.propertyPath;
        SerializedObject serializedObject = property.serializedObject;

        var menu = new GenericMenu();
        Type currentType = property.managedReferenceValue?.GetType();

        menu.AddItem(new GUIContent(NoneLabel), currentType == null,
            () => Assign(serializedObject, propertyPath, null));

        menu.AddSeparator(string.Empty);

        foreach (Type type in GetImplementations(baseType))
        {
            Type captured = type;
            menu.AddItem(new GUIContent(GetTypeLabel(type, selector.ShowFullName)), currentType == type,
                () => Assign(serializedObject, propertyPath, captured));
        }

        menu.ShowAsContext();
    }

    /// <summary>
    /// Создаёт экземпляр выбранного типа и записывает его в поле.
    /// </summary>
    private static void Assign(SerializedObject serializedObject, string propertyPath, Type type)
    {
        serializedObject.Update();

        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        if (property == null)
            return;

        try
        {
            property.managedReferenceValue = type == null ? null : Activator.CreateInstance(type);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Не удалось создать {type?.Name}: {exception.Message}");
            return;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawChildren(Rect position, SerializedProperty property)
    {
        float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        foreach (SerializedProperty child in EnumerateChildren(property))
        {
            float height = EditorGUI.GetPropertyHeight(child, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), child, true);
            y += height + EditorGUIUtility.standardVerticalSpacing;
        }
    }

    /// <summary>
    /// Перебирает непосредственные поля выбранной реализации.
    /// </summary>
    private static IEnumerable<SerializedProperty> EnumerateChildren(SerializedProperty property)
    {
        SerializedProperty iterator = property.Copy();
        SerializedProperty end = iterator.GetEndProperty();

        if (!iterator.NextVisible(true))
            yield break;

        int depth = iterator.depth;

        do
        {
            if (SerializedProperty.EqualContents(iterator, end))
                yield break;

            if (iterator.depth == depth)
                yield return iterator.Copy();
        }
        while (iterator.NextVisible(false));
    }

    /// <summary>
    /// Возвращает тип поля, объявленный в коде: <c>IAction</c> для <c>[SerializeReference] IAction</c>.
    /// </summary>
    /// <remarks>
    /// Unity хранит его строкой вида «сборка полное.имя.Типа», собственного API для
    /// получения <see cref="Type"/> у <see cref="SerializedProperty"/> нет.
    /// </remarks>
    private static Type GetManagedReferenceFieldType(SerializedProperty property)
    {
        string typename = property.managedReferenceFieldTypename;
        if (string.IsNullOrEmpty(typename))
            return null;

        string[] parts = typename.Split(' ');
        if (parts.Length != 2)
            return null;

        return Type.GetType($"{parts[1]}, {parts[0]}");
    }

    /// <summary>
    /// Собирает типы, которые Unity сможет положить в такое поле.
    /// </summary>
    private static Type[] GetImplementations(Type baseType)
    {
        if (implementationsCache.TryGetValue(baseType, out Type[] cached))
            return cached;

        Type[] result = TypeCache.GetTypesDerivedFrom(baseType)
            .Where(IsSelectable)
            .OrderBy(type => type.Name)
            .ToArray();

        implementationsCache[baseType] = result;
        return result;
    }

    private static bool IsSelectable(Type type)
    {
        if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters)
            return false;

        // SerializeReference хранит обычные объекты: наследники UnityEngine.Object
        // сериализуются ссылкой на ассет и в такое поле не попадают.
        if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            return false;

        if (!Attribute.IsDefined(type, typeof(SerializableAttribute)))
            return false;

        return type.GetConstructor(Type.EmptyTypes) != null;
    }

    private static string GetTypeLabel(Type type, bool showFullName)
    {
        if (type == null)
            return NoneLabel;

        return showFullName ? type.FullName : ObjectNames.NicifyVariableName(type.Name);
    }
}
