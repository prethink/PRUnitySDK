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

        project ??= FindProject() ?? CreateProject();

        if (project == null)
            return;

        PRSDKDatabase database = project.Database ?? Find<PRSDKDatabase>() ?? Create<PRSDKDatabase>();
        PRSDKSettings settings = project.Settings ?? Find<PRSDKSettings>() ?? Create<PRSDKSettings>();
        PrefabContainer prefabs = project.Prefabs ?? Find<PrefabContainer>() ?? Create<PrefabContainer>();

        project.SetContent(database, settings, prefabs);

        if (pointer.Project != project)
            pointer.SetProject(project);

        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[PRUnitySDK] Проект «{project.Title}» подключён: " +
            $"база {Describe(database)}, настройки {Describe(settings)}, префабы {Describe(prefabs)}.");
    }

    /// <summary>
    /// Существующий проект: единственный либо ни одного.
    /// </summary>
    /// <remarks>
    /// Когда проектов несколько, выбирать за человека нельзя — он сам укажет активный
    /// в окне; молча взять первый попавшийся значит собрать игру не из тех данных.
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
    /// Создаёт недостающий ассет данных.
    /// </summary>
    /// <remarks>
    /// Вне <c>Resources</c>: до него дотягивается ссылка из проекта, а всё, что лежит
    /// в ресурсах, попадает в сборку независимо от того, нужно оно этой игре или нет.
    /// </remarks>
    private static T Create<T>() where T : ScriptableObject
    {
        EnsureFolder(DataFolder);

        var asset = ScriptableObject.CreateInstance<T>();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{DataFolder}/{typeof(T).Name}.asset");

        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"[PRUnitySDK] Создан пустой {typeof(T).Name}: {path}");

        return asset;
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
