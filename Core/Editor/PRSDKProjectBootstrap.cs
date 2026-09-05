using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Подхватывает данные игры при первом запуске.
/// </summary>
/// <remarks>
/// SDK не должен требовать ручной настройки, чтобы просто открыться: при загрузке
/// редактора он смотрит, есть ли активный проект, и если нет — собирает его сам.
/// Уже существующие база, настройки и префабы не пересоздаются, а записываются в проект
/// как есть: импорт SDK в живую игру не должен ничего обнулять.
/// <para>
/// Чего не хватает — создаётся пустым в папке данных. Пустая база лучше, чем ошибка
/// на старте: игру можно запустить и заполнять по ходу.
/// </para>
/// </remarks>
[InitializeOnLoad]
public static class PRSDKProjectBootstrap
{
    private const string ProjectsFolder = "Assets/PRUnityData/Projects";
    private const string DataFolder = "Assets/PRUnityData";
    private const string DefaultProjectName = "Current Project";

    static PRSDKProjectBootstrap()
    {
        // Отложенно: при загрузке домена база ассетов может быть ещё не готова,
        // а импорт из конструктора статического класса Unity не приветствует.
        EditorApplication.delayCall += Ensure;
    }

    /// <summary>
    /// Проверяет данные игры и при необходимости собирает проект.
    /// </summary>
    private static void Ensure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            return;

        PRSDKActiveProject pointer = PRSDKActiveProject.Instance;

        if (pointer == null)
            return;

        PRSDKProject project = pointer.Project;

        if (project != null && project.IsComplete)
            return;

        // Первая настройка — когда активного проекта ещё нет. Только тогда можно взять
        // данные, лежащие в проекте Unity сами по себе: это данные той же игры, просто
        // созданные до появления проектов. У проекта, уже выбранного человеком, чужого
        // брать нельзя — так одна игра начала бы собираться из данных другой.
        bool isFirstSetup = project == null;

        project ??= FindProject() ?? CreateProject();

        if (project == null)
            return;

        PRSDKDatabase database = project.Database
                                 ?? (isFirstSetup ? Find<PRSDKDatabase>() : null)
                                 ?? Create<PRSDKDatabase>(project.name);

        PRSDKSettings settings = project.Settings
                                 ?? (isFirstSetup ? Find<PRSDKSettings>() : null)
                                 ?? Create<PRSDKSettings>(project.name);

        project.SetContent(database, settings);

        if (pointer.Project != project)
            pointer.SetProject(project);

        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[PRUnitySDK] Проект «{project.Title}» подключён: " +
            $"база {Describe(database)}, настройки {Describe(settings)}.");
    }

    /// <summary>
    /// Существующий проект: единственный либо ни одного.
    /// </summary>
    /// <remarks>
    /// Когда проектов несколько, выбирать за человека нельзя: он сам укажет активный
    /// в окне. Взятый наугад первый проект означает сборку не из тех данных.
    /// </remarks>
    private static PRSDKProject FindProject()
    {
        PRSDKProject[] projects = FindAll<PRSDKProject>();

        if (projects.Length == 1)
            return projects[0];

        if (projects.Length > 1)
        {
            Debug.LogWarning(
                $"[PRUnitySDK] Проектов найдено {projects.Length}, активный не выбран — " +
                "укажите его в окне PRUnitySDK/Windows/Project.");
        }

        return null;
    }

    private static PRSDKProject CreateProject()
    {
        EnsureFolder(ProjectsFolder);

        string path = AssetDatabase.GenerateUniqueAssetPath($"{ProjectsFolder}/{DefaultProjectName}.asset");
        var project = ScriptableObject.CreateInstance<PRSDKProject>();

        AssetDatabase.CreateAsset(project, path);
        return project;
    }

    /// <summary>
    /// Первый ассет нужного вида в проекте Unity.
    /// </summary>
    private static T Find<T>() where T : ScriptableObject
    {
        T[] assets = FindAll<T>();

        if (assets.Length > 1)
        {
            Debug.LogWarning(
                $"[PRUnitySDK] {typeof(T).Name}: найдено {assets.Length} ассетов, взят первый — " +
                "лишние стоит удалить или разложить по проектам.");
        }

        return assets.FirstOrDefault();
    }

    private static T[] FindAll<T>() where T : ScriptableObject
    {
        return AssetDatabase.FindAssets($"t:{typeof(T).Name}")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<T>)
            .Where(asset => asset != null)
            .ToArray();
    }

    /// <summary>
    /// Создаёт недостающий ассет данных в папке своего проекта.
    /// </summary>
    /// <remarks>
    /// Вне <c>Resources</c>: до него дотягивается ссылка из проекта, а всё, что лежит
    /// в ресурсах, попадает в сборку независимо от того, нужно оно этой игре или нет.
    /// Папка по имени проекта нужна, чтобы данные двух игр не смешались в одном списке
    /// файлов.
    /// </remarks>
    public static T Create<T>(string projectName) where T : ScriptableObject
    {
        string folder = GetProjectFolder(projectName);
        EnsureFolder(folder);

        var asset = ScriptableObject.CreateInstance<T>();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{typeof(T).Name}.asset");

        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"[PRUnitySDK] Создан пустой {typeof(T).Name}: {path}");

        return asset;
    }

    /// <summary>
    /// Папка данных проекта.
    /// </summary>
    public static string GetProjectFolder(string projectName)
    {
        string safe = string.IsNullOrWhiteSpace(projectName) ? "Project" : projectName.Trim();

        foreach (char invalid in Path.GetInvalidFileNameChars())
            safe = safe.Replace(invalid, '_');

        return $"{DataFolder}/{safe}";
    }

    private static void EnsureFolder(string folder)
    {
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }
    }

    private static string Describe(Object asset)
    {
        return asset != null ? AssetDatabase.GetAssetPath(asset) : "не найдены";
    }
}
