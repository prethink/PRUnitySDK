using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(PrefabPreviewAttribute))]
public class PrefabPreviewDrawer : PropertyDrawer
{
    private Editor previewEditor;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (PrefabPreviewAttribute)attribute;

        Rect fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        Rect previewRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 4, position.width, attr.Height);

        EditorGUI.PropertyField(fieldRect, property, label);

        if (property.objectReferenceValue == null)
            return;

        // создаём editor для превью
        if (previewEditor == null || previewEditor.target != property.objectReferenceValue)
        {
            previewEditor = Editor.CreateEditor(property.objectReferenceValue);
        }

        if (previewEditor != null && previewEditor.HasPreviewGUI())
        {
            previewEditor.OnPreviewGUI(previewRect, EditorStyles.helpBox);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var attr = (PrefabPreviewAttribute)attribute;

        return EditorGUIUtility.singleLineHeight + 4 + attr.Height;
    }
}