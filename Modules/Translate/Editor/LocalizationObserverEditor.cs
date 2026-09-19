using UnityEditor;
using UnityEngine;

/// <summary>
/// Добавляет инструменты проверки языка в стандартный Inspector наблюдателей перевода.
/// </summary>
/// <remarks>
/// Редактор один на всех: кнопки переключают язык, а какой источник у наблюдателя —
/// провайдер или ключ — для проверки неважно.
/// </remarks>
[CustomEditor(typeof(LocalizationObserverBase), true)]
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
