using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(LocalizationRow))]
public class LocalizationRowDrawer : LocalizationLangDrawerBase
{
    protected override SerializedProperty GetValues(SerializedProperty property)
    {
        return property.FindPropertyRelative("langData");
    }

    protected override float GetBeforeLanguagesHeight(SerializedProperty property)
    {
        return EditorGUIUtility.singleLineHeight + 3f * 2;
    }

    protected override void DrawBeforeLanguages(Rect position, SerializedProperty property, ref Rect r)
    {
        var key = property.FindPropertyRelative(nameof(LocalizationRow.Key).GetBackingField());

        float h = EditorGUIUtility.singleLineHeight;
        float sp = 3f;

        key.stringValue = EditorGUI.TextField(r, "Key", key.stringValue);

        r.y += h + sp * 2;
    }
}