using AYellowpaper.SerializedCollections.Editor;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Стандартный встроенный Editor для правой панели PRSDKDatabase без зависимости от private SDK.
/// </summary>
public sealed class PRSDKEmbeddedAssetEditor : UnityEditor.Editor
{
    private const string SerializedDictionaryListName = "_serializedList";
    private readonly Dictionary<string, SerializedDictionaryInstanceDrawer> dictionaryDrawers = new();

    public override void OnInspectorGUI()
    {
        serializedObject.UpdateIfRequiredOrScript();
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            bool readOnly =
                iterator.propertyPath == "m_Script" ||
                IsStableIdProperty(iterator.name);

            using (new EditorGUI.DisabledScope(readOnly))
                DrawEmbeddedProperty(iterator, target.GetType());
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void OnDisable()
    {
        dictionaryDrawers.Clear();
    }

    private void DrawEmbeddedProperty(SerializedProperty property, Type parentType)
    {
        if (TryDrawSerializedDictionary(property, parentType))
            return;

        if (!ContainsSerializedDictionary(property))
        {
            EditorGUILayout.PropertyField(property, includeChildren: true);
            return;
        }

        property.isExpanded = EditorGUILayout.Foldout(
            property.isExpanded,
            new GUIContent(property.displayName, property.tooltip),
            true);
        if (!property.isExpanded)
            return;

        using (new EditorGUI.IndentLevelScope())
        {
            FieldInfo fieldInfo = PRSDKInspectorUtility.GetFieldInfo(parentType, property);
            Type childParentType = fieldInfo?.FieldType;
            foreach (SerializedProperty child in PRSDKInspectorUtility.GetDirectChildren(property))
                DrawEmbeddedProperty(child, childParentType);
        }
    }

    private bool TryDrawSerializedDictionary(SerializedProperty property, Type parentType)
    {
        if (!property.hasVisibleChildren)
            return false;

        SerializedProperty serializedList =
            property.FindPropertyRelative(SerializedDictionaryListName);
        if (serializedList is not { isArray: true })
            return false;

        FieldInfo fieldInfo = PRSDKInspectorUtility.GetFieldInfo(parentType, property);
        if (fieldInfo == null)
        {
            DrawSerializedDictionaryFallback(property, serializedList);
            return true;
        }

        if (!dictionaryDrawers.TryGetValue(
                property.propertyPath,
                out SerializedDictionaryInstanceDrawer drawer))
        {
            drawer = new SerializedDictionaryInstanceDrawer(property, fieldInfo);
            dictionaryDrawers[property.propertyPath] = drawer;
        }

        var label = new GUIContent(property.displayName, property.tooltip);
        float height = drawer.GetPropertyHeight(label);
        Rect position = EditorGUILayout.GetControlRect(
            true,
            height,
            GUILayout.ExpandWidth(true));
        drawer.OnGUI(position, label);
        return true;
    }

    private static void DrawSerializedDictionaryFallback(
        SerializedProperty property,
        SerializedProperty serializedList)
    {
        serializedList.isExpanded = property.isExpanded;
        EditorGUILayout.PropertyField(
            serializedList,
            new GUIContent(property.displayName, property.tooltip),
            includeChildren: true);
        property.isExpanded = serializedList.isExpanded;
    }

    private static bool ContainsSerializedDictionary(SerializedProperty property)
    {
        if (!property.hasVisibleChildren)
            return false;

        foreach (SerializedProperty child in PRSDKInspectorUtility.GetDirectChildren(property))
        {
            if (child.hasVisibleChildren &&
                child.FindPropertyRelative(SerializedDictionaryListName) != null)
            {
                return true;
            }

            if (ContainsSerializedDictionary(child))
                return true;
        }

        return false;
    }

    private static bool IsStableIdProperty(string propertyName)
    {
        return string.Equals(propertyName, "id", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(propertyName, "<id>k__BackingField", StringComparison.OrdinalIgnoreCase);
    }
}
