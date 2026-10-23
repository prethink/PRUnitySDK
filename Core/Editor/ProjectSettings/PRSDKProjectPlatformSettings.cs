using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Свои настройки площадок у каждого проекта SDK: при смене проекта в плагин встают настройки новой игры.
/// </summary>
/// <remarks>
/// Плагин держит настройки в одном месте (у Яндекса — <c>Assets/PluginYourGames/Resources/SettingsYG2.asset</c>:
/// ID игры, покупки, таблицы лидеров), и без переключения все игры в одном Unity-проекте делили бы их. Какие файлы
/// чьи, задаёт манифест модуля (<see cref="PRModuleManifest.ProjectSettingsFiles"/>), пути — от папки плагина.
/// <para>
/// Копия лежит у проекта: <c>&lt;папка ассета проекта&gt;/PlatformSettings~/&lt;Id модуля&gt;/&lt;путь в плагине&gt;</c>. «~»
/// прячет её от Unity — второго ассета с тем же GUID не появится, — а в git она идёт вместе с данными игры.
/// </para>
/// <para>
/// Правка в плагине сразу уходит в копию активного проекта (<see cref="Postprocessor"/>). При смене проекта
/// изменённые в памяти ассеты сначала записываются и запоминаются у прежнего, затем на место встают копии нового;
/// копии у нового нет — он получает нынешние настройки как стартовые. При запуске редактора копия активного
/// проекта сверяется с плагином: новее копия (пришла из git) — встаёт она, новее плагин — запоминается он.
/// </para>
/// <para>
/// Отключённая площадка прячет плагин в «Имя~», но её настройки переключаются и там: файлы ищутся в обеих папках.
/// Поэтому этот класс в ядре и не зависит от кода плагинов.
/// </para>
/// </remarks>
[InitializeOnLoad]
public static class PRSDKProjectPlatformSettings
{
    /// <summary>
    /// Папка копий у проекта.
    /// </summary>
    public const string FolderName = "PlatformSettings~";

    private static List<PRModuleManifest> manifests;
    private static bool writing;

    static PRSDKProjectPlatformSettings()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                Load(Active, preferNewer: true);
        };
    }

    private static PRSDKProject Active => PRSDKActiveProject.Instance != null ? PRSDKActiveProject.Instance.Project : null;

    /// <summary>
    /// Переводит настройки площадок с проекта <paramref name="from"/> на <paramref name="to"/>.
    /// </summary>
    public static void Switch(PRSDKProject from, PRSDKProject to)
    {
        if (to == null || from == to)
            return;

        if (from != null)
        {
            foreach (FileEntry entry in Entries())
            {
                // Правка в окне плагина может ещё не лечь на диск: без записи она ушла бы в настройки новой игры.
                if (entry.Visible && entry.Live.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                    AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(entry.Live));

                PRSDKPlatformSettingsFile.Save(entry.Live, StoredPath(from, entry), out string error);
                if (error != null)
                    Debug.LogError($"[PRUnitySDK] Настройки {entry.Live} не запомнены у {from.Title}: {error}. Они перейдут к {to.Title}.");
            }
        }

        Load(to, preferNewer: false);
    }

    private static void Load(PRSDKProject project, bool preferNewer)
    {
        if (project == null)
            return;

        var loaded = new List<string>();
        var seeded = new List<string>();

        writing = true;
        try
        {
            foreach (FileEntry entry in Entries())
            {
                PRSDKPlatformSettingsFile.LoadResult result =
                    PRSDKPlatformSettingsFile.Load(entry.Live, StoredPath(project, entry), preferNewer, out string error);

                if (result == PRSDKPlatformSettingsFile.LoadResult.Failed)
                    Debug.LogError($"[PRUnitySDK] Настройки площадки {entry.Live} не переключены на {project.Title}: {error}.");
                else if (result == PRSDKPlatformSettingsFile.LoadResult.Seeded)
                    seeded.Add(entry.Live);
                else if (result == PRSDKPlatformSettingsFile.LoadResult.Loaded)
                {
                    loaded.Add(entry.Live);
                    // Ассет в памяти перечитывается с диска; у спрятанного плагина Unity файла не видит.
                    if (entry.Visible)
                        AssetDatabase.ImportAsset(entry.Live, ImportAssetOptions.ForceUpdate);
                }
            }
        }
        finally
        {
            writing = false;
        }

        if (loaded.Count > 0)
            Debug.Log($"[PRUnitySDK] Настройки площадок проекта {project.Title}: {string.Join(", ", loaded)}.");
        if (seeded.Count > 0)
            Debug.Log($"[PRUnitySDK] У проекта {project.Title} своих настроек площадок не было — взяты нынешние: " +
                      $"{string.Join(", ", seeded)}. Поправьте их под игру.");
    }

    private static string StoredPath(PRSDKProject project, FileEntry entry)
    {
        string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(project))!.Replace('\\', '/');
        return $"{folder}/{FolderName}/{entry.ModuleId}/{entry.Relative}";
    }

    private static IEnumerable<FileEntry> Entries()
    {
        manifests ??= AssetDatabase.FindAssets("t:" + nameof(PRModuleManifest))
            .Select(guid => AssetDatabase.LoadAssetAtPath<PRModuleManifest>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(manifest => manifest != null)
            .ToList();

        foreach (PRModuleManifest manifest in manifests)
        {
            if (manifest == null || manifest.ProjectSettingsFiles.Count == 0)
                continue;

            PRPluginState state = PRModulePlugins.GetState(manifest);
            if (state != PRPluginState.Visible && state != PRPluginState.Hidden)
                continue;

            string folder = manifest.PluginFolder + (state == PRPluginState.Hidden ? PRModulePlugins.HiddenSuffix : string.Empty);
            foreach (string relative in manifest.ProjectSettingsFiles)
                yield return new FileEntry(manifest.Id, $"{folder}/{relative}", relative, state == PRPluginState.Visible);
        }
    }

    private readonly struct FileEntry
    {
        public FileEntry(string moduleId, string live, string relative, bool visible)
        {
            ModuleId = moduleId;
            Live = live;
            Relative = relative;
            Visible = visible;
        }

        public string ModuleId { get; }
        public string Live { get; }
        public string Relative { get; }
        public bool Visible { get; }
    }

    /// <summary>
    /// Правка настроек в плагине сразу уходит в копию активного проекта.
    /// </summary>
    private sealed class Postprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            // Сменился манифест — список файлов перечитается.
            if (imported.Concat(moved).Any(path => AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(PRModuleManifest)))
                manifests = null;

            PRSDKProject project = Active;
            if (writing || project == null || imported.Length == 0)
                return;

            var changed = new HashSet<string>(imported, StringComparer.OrdinalIgnoreCase);
            foreach (FileEntry entry in Entries())
            {
                if (!entry.Visible || !changed.Contains(entry.Live))
                    continue;

                PRSDKPlatformSettingsFile.Save(entry.Live, StoredPath(project, entry), out string error);
                if (error != null)
                    Debug.LogError($"[PRUnitySDK] Правка {entry.Live} не запомнена у проекта {project.Title}: {error}.");
            }
        }
    }
}
