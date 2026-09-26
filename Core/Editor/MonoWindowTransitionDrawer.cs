using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Рисует свои значения перехода окна только тогда, когда они работают.
/// </summary>
/// <remarks>
/// Значения лежат рядом с выбором пресета (<see cref="MonoWindowTransitionSettings"/>) и
/// нужны только при <see cref="MonoWindowTransitionPreset.Custom"/>. Показанные всегда, они
/// выглядели бы настройкой, которая ничего не меняет: правишь длительность — а окно
/// открывается по пресету. Поле пресета ищется соседом по пути; не нашлось — значения
/// рисуются всегда, как у отдельного перехода.
/// </remarks>
[CustomPropertyDrawer(typeof(MonoWindowTransition))]
public class MonoWindowTransitionDrawer : PropertyDrawer
{
    private const string PresetField = "<" + nameof(MonoWindowTransitionSettings.Preset) + ">k__BackingField";

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!IsUsed(property))
            return 0f;

        float height = EditorGUIUtility.singleLineHeight;

        foreach (SerializedProperty child in GetChildren(property))
            height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(child, true);

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!IsUsed(property))
            return;

        EditorGUI.BeginProperty(position, label, property);

        var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(line, label, EditorStyles.boldLabel);

        float y = line.yMax;

        using (new EditorGUI.IndentLevelScope())
        {
            foreach (SerializedProperty child in GetChildren(property))
            {
                y += EditorGUIUtility.standardVerticalSpacing;

                float height = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), child, true);
                y += height;
            }
        }

        EditorGUI.EndProperty();
    }

    /// <summary>
    /// Работают ли свои значения: пресет рядом — <c>Custom</c> или его нет вовсе.
    /// </summary>
    private static bool IsUsed(SerializedProperty property)
    {
        string path = property.propertyPath;
        int separator = path.LastIndexOf('.');
        string presetPath = separator >= 0 ? path.Substring(0, separator + 1) + PresetField : PresetField;

        SerializedProperty preset = property.serializedObject.FindProperty(presetPath);

        return preset == null
               || preset.hasMultipleDifferentValues
               || preset.intValue == (int)MonoWindowTransitionPreset.Custom;
    }

    private static IEnumerable<SerializedProperty> GetChildren(SerializedProperty property)
    {
        SerializedProperty child = property.Copy();
        SerializedProperty end = property.GetEndProperty();

        if (!child.NextVisible(true))
            yield break;

        while (!SerializedProperty.EqualContents(child, end))
        {
            yield return child.Copy();

            if (!child.NextVisible(false))
                yield break;
        }
    }
}
