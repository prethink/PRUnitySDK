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
/// Проект — это база и настройки одной игры. Переключение меняет их разом: раньше состав
/// приходилось переносить наборами поверх общих ассетов, и перепутать игры было легко.
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

    private const string DefaultFolder = "Assets/PRUnityData/Projects";

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
                    "Проект не выбран: база и настройки берутся из Resources, как раньше. " +
                    "Так работает игра, которая на проекты ещё не переходила.",
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

                // Последний проект удалить нельзя: без него игра вернётся к загрузке
                // из Resources, где данных уже нет — они переехали в папку проекта.
                using (new EditorGUI.DisabledScope(projects.Length <= 1))
                {
                    if (GUILayout.Button("Удалить", GUILayout.Width(80f)))
                    {
                        Delete(project);
                        GUIUtility.ExitGUI();
                    }
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

        Debug.Log($"[PRUnitySDK] Активный проект: {project.Title}.");
        Reload();
    }

    /// <summary>
    /// Удаляет проект и, по выбору, его данные.
    /// </summary>
    /// <remarks>
    /// Спрашивает всегда: удаление ассетов необратимо, а база игры — это недели работы.
    /// Данные удаляются только свои: если на базу или настройки ссылается другой проект,
    /// они остаются, иначе одна игра унесла бы за собой данные другой.
    /// </remarks>
    private void Delete(PRSDKProject project)
    {
        if (project == null || projects.Length <= 1)
            return;

        // Всё, что понадобится после удаления, снимается заранее: удалённый ассет —
        // уничтоженный объект, и обращение к его имени бросает MissingReferenceException.
        string title = project.Title;
        string projectPath = AssetDatabase.GetAssetPath(project);
        string databasePath = GetOwnAssetPath(project, project.Database);
        string settingsPath = GetOwnAssetPath(project, project.Settings);

        var details = new List<string> { $"Проект: {projectPath}" };

        if (!string.IsNullOrEmpty(databasePath))
            details.Add($"База: {databasePath}");

        if (!string.IsNullOrEmpty(settingsPath))
            details.Add($"Настройки: {settingsPath}");

        int choice = EditorUtility.DisplayDialogComplex(
            $"Удалить «{title}»?",
            string.Join("\n", details) + "\n\nОтменить удаление нельзя.",
            "Удалить с данными",
            "Отмена",
            "Только проект");

        if (choice == 1)
            return;

        bool wasActive = IsActive(project);

        if (choice == 0)
        {
            DeleteAsset(databasePath);
            DeleteAsset(settingsPath);
        }

        AssetDatabase.DeleteAsset(projectPath);

        if (choice == 0)
            RemoveEmptyFolder(databasePath, settingsPath);

        AssetDatabase.SaveAssets();

        Reload();

        // Активный проект нельзя оставить пустым: указатель повис бы, а данных
        // в Resources больше нет.
        if (wasActive && projects.Length > 0)
            Activate(projects[0]);

        Debug.Log($"[PRUnitySDK] Проект «{title}» удалён.");
    }

    /// <summary>
    /// Убирает опустевшую папку данных проекта.
    /// </summary>
    /// <remarks>
    /// Unity удаляет только ассеты, а папка остаётся: список папок с именами удалённых
    /// игр выглядит так, будто они ещё существуют. Папка убирается лишь пустой — если
    /// туда положили что-то своё, это не мусор.
    /// </remarks>
    private static void RemoveEmptyFolder(params string[] assetPaths)
    {
        foreach (string assetPath in assetPaths)
        {
            if (string.IsNullOrEmpty(assetPath))
                continue;

            string folder = Path.GetDirectoryName(assetPath);

            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
                continue;

            bool isEmpty = AssetDatabase.FindAssets(string.Empty, new[] { folder }).Length == 0;

            if (isEmpty)
                AssetDatabase.DeleteAsset(folder);
        }
    }

    /// <summary>
    /// Путь ассета, если он принадлежит только этому проекту.
    /// </summary>
    private string GetOwnAssetPath(PRSDKProject owner, ScriptableObject asset)
    {
        if (asset == null)
            return null;

        bool sharedWithOthers = projects.Any(other =>
            other != owner && (other.Database == asset || (ScriptableObject)other.Settings == asset));

        return sharedWithOthers ? null : AssetDatabase.GetAssetPath(asset);
    }

    private bool IsActive(PRSDKProject project)
    {
        PRSDKActiveProject pointer = PRSDKActiveProject.Instance;
        return pointer != null && pointer.Project == project;
    }

    private static void DeleteAsset(string path)
    {
        if (!string.IsNullOrEmpty(path))
            AssetDatabase.DeleteAsset(path);
    }

    /// <summary>
    /// Создаёт ассет проекта вместе с его данными.
    /// </summary>
    /// <param name="fromCurrent">
    /// Взять то, что игра использует сейчас, вместо создания пустых. Так делается первый
    /// проект: состав уже собран, и переносить его руками незачем.
    /// </param>
    /// <remarks>
    /// Новый проект получает **свои** базу и настройки, а не ссылки на чужие: иначе две
    /// игры правили бы одни и те же ассеты, и смысл разделения терялся бы на первом же
    /// изменении.
    /// </remarks>
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
        AssetDatabase.CreateAsset(project, path);

        string projectName = Path.GetFileNameWithoutExtension(path);

        if (fromCurrent)
        {
            project.SetContent(PRSDKDatabase.Instance, PRSDKSettings.Instance);
        }
        else
        {
            project.SetContent(
                PRSDKProjectBootstrap.Create<PRSDKDatabase>(projectName),
                PRSDKProjectBootstrap.Create<PRSDKSettings>(projectName));
        }

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
