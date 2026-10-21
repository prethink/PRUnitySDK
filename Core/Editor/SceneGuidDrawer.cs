using UnityEditor;
using UnityEngine;

/// <summary>
/// Поле выбора сцены для строки с GUID (<see cref="SceneGuidAttribute"/>).
/// </summary>
[CustomPropertyDrawer(typeof(SceneGuidAttribute))]
public sealed class SceneGuidDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "SceneGuid — только для строки");
            return;
        }

        string path = string.IsNullOrEmpty(property.stringValue) ? null : AssetDatabase.GUIDToAssetPath(property.stringValue);
        var current = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();
        var picked = (SceneAsset)EditorGUI.ObjectField(position, label, current, typeof(SceneAsset), false);
        if (EditorGUI.EndChangeCheck())
            property.stringValue = picked != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(picked)) : string.Empty;
        EditorGUI.EndProperty();
    }
}
