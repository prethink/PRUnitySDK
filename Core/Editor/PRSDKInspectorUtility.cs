using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Общие элементы интерфейса инспекторов assets PRUnitySDK.
/// </summary>
internal static class PRSDKInspectorUtility
{
    private const BindingFlags FieldFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    /// Возвращает сериализованные секции верхнего уровня без поля MonoScript.
    /// </summary>
    public static IReadOnlyList<SerializedProperty> GetRootProperties(SerializedObject serializedObject)
    {
        var properties = new List<SerializedProperty>();
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (iterator.propertyPath == "m_Script")
                continue;

            properties.Add(iterator.Copy());
        }

        return properties;
    }

    /// <summary>
    /// Преобразует имя backing field в читаемое название секции.
    /// </summary>
    public static string GetSectionName(SerializedProperty property)
    {
        string name = property.name;
        const string backingFieldSuffix = ">k__BackingField";

        if (name.StartsWith("<", StringComparison.Ordinal) &&
            name.EndsWith(backingFieldSuffix, StringComparison.Ordinal))
        {
            name = name.Substring(1, name.Length - backingFieldSuffix.Length - 1);
        }

        return ObjectNames.NicifyVariableName(name);
    }

    /// <summary>
    /// Возвращает тип поля, соответствующего сериализованной секции.
    /// </summary>
    public static Type GetFieldType(Type targetType, SerializedProperty property)
    {
        for (Type current = targetType; current != null; current = current.BaseType)
        {
            FieldInfo field = current.GetField(property.name, FieldFlags);
            if (field != null)
                return field.FieldType;
        }

        return null;
    }

    /// <summary>
    /// Возвращает тип definition для наследника <see cref="Database{T}"/>.
    /// </summary>
    public static Type GetDatabaseElementType(Type databaseType)
    {
        for (Type current = databaseType; current != null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Database<>))
                return current.GetGenericArguments()[0];
        }

        return null;
    }

    /// <summary>
    /// Проверяет, соответствует ли секция текущему поисковому запросу.
    /// </summary>
    public static bool MatchesSearch(string sectionName, string search)
    {
        return string.IsNullOrWhiteSpace(search) ||
               sectionName.IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// Рисует общую шапку инспектора singleton-asset.
    /// </summary>
    public static void DrawHeader(string title, UnityEngine.Object asset)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField(title, EditorStyles.largeLabel);
        string path = AssetDatabase.GetAssetPath(asset);
        if (!string.IsNullOrWhiteSpace(path))
            EditorGUILayout.LabelField(path, EditorStyles.miniLabel);
        EditorGUILayout.Space(4f);
    }
}
