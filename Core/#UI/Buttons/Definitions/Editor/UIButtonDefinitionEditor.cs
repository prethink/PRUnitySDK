using UnityEditor;
using UnityEngine;

/// <summary>
/// Проверяет ссылки и обязательные настройки кнопки до запуска игры.
/// </summary>
[CustomEditor(typeof(UIButtonDefinition), true)]
public sealed class UIButtonDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        serializedObject.Update();
        if (!serializedObject.FindProperty("openWindow").boolValue &&
            serializedObject.FindProperty("action").objectReferenceValue == null && target.GetType() == typeof(UIButtonDefinition))
            EditorGUILayout.HelpBox("Назначьте Action или включите Open Window: сейчас кнопка ничего не выполняет.", MessageType.Warning);
        var definition = (UIButtonDefinition)target;
        if (definition.ShowDescription && string.IsNullOrWhiteSpace(definition.LocalizationKey) &&
            (definition.LocalizationValues == null || definition.LocalizationValues.Count == 0))
            EditorGUILayout.HelpBox("Заполните Description для трёх языков или задайте ключ базы локализации. До этого показывается запасная подпись.", MessageType.Warning);
        if (!definition.ShowButton && definition.Hotkey == KeyCode.None)
            EditorGUILayout.HelpBox("Кнопка скрыта и клавиша не назначена: вызвать её из интерфейса нельзя.", MessageType.Warning);
    }
}
