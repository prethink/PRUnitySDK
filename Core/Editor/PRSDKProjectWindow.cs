using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Окно проекта SDK: какой игрой мы сейчас занимаемся.
/// </summary>
/// <remarks>
/// Проект — это база, настройки и префабы одной игры. Переключение меняет их все разом:
/// раньше состав приходилось переносить наборами поверх общих ассетов, и перепутать игры
/// было легко.
/// <para>
/// В сборку уходит только активный проект: сами ассеты лежат вне <c>Resources</c>,
/// а ссылка на них одна — из указателя <see cref="PRSDKActiveProject"/>.
/// </para>
/// </remarks>
public sealed class PRSDKProjectWindow : EditorWindow
{
    /// <summary>
    /// Путь пункта меню.
    /// </summary>
    public const string MenuPath = "PRUnitySDK/Windows/Project";

    private const string DefaultFolder = "Assets/PRUnitySDK.Projects";

    private PRSDKProject[] projects = Array.Empty<PRSDKProject>();
    private UnityEditor.Editor activeEditor;
    private Vector2 scroll;
    private bool loaded;

    [MenuItem(MenuPath, false, 0)]
    private static void Open()
    {
        var window = GetWindow<PRSDKProjectWindow>();
        window.titleContent = new GUIContent("Проект SDK");
        window.minSize = new Vector2(560f, 420f);
        window.Show();
    }

    private void OnGUI()
    {
        if (!loaded)
            Reload();

        DrawToolbar();

        using (var scope = new EditorGUILayout.ScrollViewScope(scroll))
        {
            scroll = scope.scrollPosition;

            DrawActive();
            EditorGUILayout.Space(6f);
            DrawList();
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Обновить", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                Reload();

            if (GUILayout.Button("Создать проект", EditorStyles.toolbarButton, GUILayout.Width(120f)))
                CreateProject(fromCurrent: false);

            if (GUILayout.Button("Собрать из текущих", EditorStyles.toolbarButton, GUILayout.Width(140f)))
                CreateProject(fromCurrent: true);

            GUILayout.FlexibleSpace();
        }
    }

    /// <summary>
    /// Показывает активный проект и его состав.
    /// </summary>
    private void DrawActive()
    {
        PRSDKActiveProject pointer = PRSDKActiveProject.Instance;
        PRSDKProject active = pointer != null ? pointer.Project : null;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Текущий проект", EditorStyles.boldLabel);

            if (active == null)
            {
                EditorGUILayout.HelpBox(
                    "Проект не выбран: база, настройки и префабы берутся из Resources, как раньше. " +
                    "Так работает и проект, который на профили ещё не переходил.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(active.Title, EditorStyles.largeLabel);

            if (!string.IsNullOrWhiteSpace(active.Description))
                EditorGUILayout.LabelField(active.Description, EditorStyles.wordWrappedMiniLabel);

            if (!active.IsComplete)
            {
                EditorGUILayout.HelpBox(
                    "Проект заполнен не полностью — недостающее берётся из Resources.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(2f);

            if (activeEditor == null || activeEditor.target != active)
            {
                DestroyEditor();
                activeEditor = UnityEditor.Editor.CreateEditor(active);
            }

            activeEditor.OnInspectorGUI();
        }
    }

    /// <summary>
    /// Список проектов, найденных в проекте Unity.
    /// </summary>
    private void DrawList()
    {
        EditorGUILayout.LabelField($"Проекты ({projects.Length})", EditorStyles.boldLabel);

        if (projects.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "Ассетов проекта нет. «Собрать из текущих» сделает первый из того, что уже лежит в Resources.",
                MessageType.Info);
            return;
        }

        PRSDKActiveProject pointer = PRSDKActiveProject.Instance;
        PRSDKProject active = pointer != null ? pointer.Project : null;

        foreach (PRSDKProject project in projects)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                bool isActive = project == active;

                EditorGUILayout.LabelField(
                    isActive ? $"● {project.Title}" : project.Title,
                    isActive ? EditorStyles.boldLabel : EditorStyles.label);

                if (GUILayout.Button("В проекте", GUILayout.Width(90f)))
                    EditorGUIUtility.PingObject(project);

                using (new EditorGUI.DisabledScope(isActive))
                {
                    if (GUILayout.Button("Сделать активным", GUILayout.Width(140f)))
                        Activate(project);
                }
            }
        }
    }

    /// <summary>
    /// Переключает активный проект.
    /// </summary>
    /// <remarks>
    /// Кеш синглтонов сбрасывается здесь же: они держат найденный ассет до перезапуска,
    /// и без сброса редактор продолжил бы показывать данные прежней игры.
    /// </remarks>
    private void Activate(PRSDKProject project)
    {
        PRSDKActiveProject pointer = PRSDKActiveProject.Instance;

        if (pointer == null)
            return;

        Undo.RecordObject(pointer, "Сменить проект SDK");
        pointer.SetProject(project);

        ScriptableObjectSingleton<PRSDKDatabase>.ResetInstance();
        ScriptableObjectSingleton<PRSDKSettings>.ResetInstance();
        ScriptableObjectSingleton<PrefabContainer>.ResetInstance();

        Debug.Log($"[PRUnitySDK] Активный проект: {project.Title}.");
        Reload();
    }

    /// <summary>
    /// Создаёт ассет проекта.
    /// </summary>
    /// <param name="fromCurrent">
    /// Заполнить тем, что игра использует сейчас. Так делается первый проект: состав уже
    /// собран, и переносить его руками незачем.
    /// </param>
    private void CreateProject(bool fromCurrent)
    {
        if (!Directory.Exists(DefaultFolder))
            Directory.CreateDirectory(DefaultFolder);

        string path = EditorUtility.SaveFilePanelInProject(
            "Новый проект SDK",
            fromCurrent ? "Current Project" : "New Project",
            "asset",
            "Где хранить проект",
            DefaultFolder);

        if (string.IsNullOrEmpty(path))
            return;

        var project = CreateInstance<PRSDKProject>();

        if (fromCurrent)
            project.SetContent(PRSDKDatabase.Instance, PRSDKSettings.Instance, PrefabContainer.Instance);

        AssetDatabase.CreateAsset(project, path);
        AssetDatabase.SaveAssets();

        Reload();
        EditorGUIUtility.PingObject(project);
    }

    private void Reload()
    {
        loaded = true;

        projects = AssetDatabase.FindAssets($"t:{nameof(PRSDKProject)}")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<PRSDKProject>)
            .Where(project => project != null)
            .OrderBy(project => project.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        DestroyEditor();
        Repaint();
    }

    private void DestroyEditor()
    {
        if (activeEditor == null)
            return;

        DestroyImmediate(activeEditor);
        activeEditor = null;
    }

    private void OnDisable()
    {
        DestroyEditor();
    }
}
