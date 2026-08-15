using UnityEditor;
using UnityEngine;

/// <summary>
/// Добавляет инструменты проверки языка в стандартный Inspector <see cref="LocalizationObserver"/>.
/// </summary>
[CustomEditor(typeof(LocalizationObserver), true)]
public sealed class LocalizationObserverEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Проверка локализации", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawLanguageButton("RU", "ru");
            DrawLanguageButton("EN", "en");
            DrawLanguageButton("TR", "tr");
        }
    }

    private static void DrawLanguageButton(string label, string languageKey)
    {
        if (GUILayout.Button(label))
            PRUnitySDK.LanguageManager.SwitchLang(languageKey);
    }
}
