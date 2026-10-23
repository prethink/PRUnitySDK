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
/// Копия лежит рядом с настройками проекта: <c>&lt;папка его PRSDKSettings&gt;/PlatformSettings~/&lt;Id модуля&gt;/&lt;путь в плагине&gt;</c>.
/// Не у ассета проекта: «Копия» в окне проектов кладёт новый проект в ту же папку, и два проекта делили бы одни
/// настройки, а PRSDKSettings у каждого свои. «~» прячет папку от Unity — второго ассета с тем же GUID не появится, — а
/// в git она идёт вместе с данными игры.
/// </para>
/// <para>
/// Переключение — часть смены проекта в <see cref="PRSDKProjectSaves.TrySwitchTo"/>: сначала настройки
/// (<see cref="PRSDKPlatformSettingsSwitch"/>: резерв, журнал, откат), потом сохранения; не вышло что-то одно —
/// настройки возвращаются, и проект не меняется. Чьи настройки сейчас в плагине, — тот же владелец, что у сохранений
/// (<c>Owner.txt</c>): переключаются они вместе. Прерванное переключение доводится по владельцу при запуске редактора.
/// </para>
/// <para>
/// Правка в плагине уходит в копию владельца, только когда он и есть активный проект (<see cref="Postprocessor"/>).
/// При запуске редактора, если владелец совпал с активным, копия сверяется с плагином: новее копия (пришла из git) —
/// встаёт она, новее плагин — запоминается он. Время файла решает только это: чьи настройки, решает владелец.
/// </para>
/// <para>
/// Отключённая площадка прячет плагин в «Имя~», но её настройки переключаются и там: файлы ищутся в обеих папках.
/// Поэтому этот класс в ядре и не зависит от кода плагинов.
/// </para>
/// </remarks>
public static class PRSDKProjectPlatformSettings
{
    /// <summary>
    /// Папка копий у проекта.
    /// </summary>
    public const string FolderName = "PlatformSettings~";

    private static List<PRModuleManifest> manifests;
    private static bool writing;

    private static PRSDKPlatformSettingsSwitch Operation => new(PRSDKProjectSaves.Root);

    /// <summary>
    /// Ставит настройки проекта <paramref name="toGuid"/>; журнал остаётся до <see cref="Commit"/> или <see cref="Revert"/>.
    /// </summary>
    /// <returns><c>false</c> — настройки прежние, причина в <paramref name="error"/>.</returns>
    internal static bool TrySwitch(string fromGuid, string toGuid, out string error)
    {
        PRSDKProject from = ProjectOf(fromGuid);
        PRSDKProject to = ProjectOf(toGuid);
        error = null;
        if (to == null)
        {
            error = $"проект {toGuid} не найден";
            return false;
        }

        var entries = new List<PRSDKPlatformSettingsSwitch.Entry>();
        foreach (FileEntry entry in Entries())
        {
            // Правка в окне плагина может ещё не лечь на диск: без записи она ушла бы в настройки новой игры.
            if (entry.Visible && entry.Live.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(entry.Live);
                if (asset != null)
                    AssetDatabase.SaveAssetIfDirty(asset);
            }

            entries.Add(new PRSDKPlatformSettingsSwitch.Entry(entry.Live, from != null ? StoredPath(from, entry) : null, StoredPath(to, entry)));
        }

        if (entries.Count == 0)
            return true;

        bool switched;
        List<string> changed;
        PRSDKPlatformSettingsSwitch operation = Operation;
        writing = true;
        try
        {
            switched = operation.TrySwitch(fromGuid, toGuid, entries, out changed, out error);
        }
        finally
        {
            writing = false;
        }

        Reimport(changed);
        if (switched && changed.Count > 0)
            Debug.Log($"[PRUnitySDK] Настройки площадок проекта {to.Title}: {string.Join(", ", changed)}.");
        // Первый переход в игру без своих настроек — она получает нынешние; без предупреждения кажется, что настройки общие.
        if (switched && operation.Seeded.Count > 0)
            Debug.LogWarning($"[PRUnitySDK] У проекта {to.Title} своих настроек площадок не было — взяты от " +
                             $"{(from != null ? from.Title : "прежнего проекта")}: {string.Join(", ", operation.Seeded)}. " +
                             "Поправьте их под игру: дальше они у каждой игры свои.");
        return switched;
    }

    /// <summary>
    /// Смена проекта прошла целиком: журнал настроек больше не нужен.
    /// </summary>
    internal static void Commit()
    {
        if (!Operation.Commit())
            Debug.LogWarning($"[PRUnitySDK] Журнал настроек площадок ({Operation.JournalPath}) не удалён — уберётся при запуске редактора.");
    }

    /// <summary>
    /// Смена проекта не прошла: настройки возвращаются из резерва.
    /// </summary>
    internal static void Revert()
    {
        PRSDKPlatformSettingsSwitch operation = Operation;
        if (!operation.HasUnfinished)
            return;

        bool reverted;
        List<string> restored;
        string error;
        writing = true;
        try
        {
            reverted = operation.Revert(out restored, out error);
        }
        finally
        {
            writing = false;
        }

        Reimport(restored);
        if (!reverted)
            Debug.LogError($"[PRUnitySDK] Настройки площадок не вернулись к прежнему проекту: {error}. Копия — {PRSDKProjectSaves.Root}.");
    }

    /// <summary>
    /// Доводит прерванное переключение настроек по владельцу сохранений.
    /// </summary>
    /// <returns><c>false</c> — настройки могут быть от разных проектов, переключать и играть нельзя.</returns>
    internal static bool Recover(string owner)
    {
        PRSDKPlatformSettingsSwitch operation = Operation;
        if (!operation.HasUnfinished)
            return true;

        PRSDKPlatformSettingsSwitch.Recovery result;
        List<string> restored;
        writing = true;
        try
        {
            result = operation.Recover(owner, out restored);
        }
        finally
        {
            writing = false;
        }

        Reimport(restored);
        switch (result)
        {
            case PRSDKPlatformSettingsSwitch.Recovery.RolledBack:
                Debug.LogWarning("[PRUnitySDK] Прошлое переключение настроек площадок прервалось: возвращены настройки владельца сохранений.");
                return true;
            case PRSDKPlatformSettingsSwitch.Recovery.Incomplete:
                Debug.LogError($"[PRUnitySDK] Прошлое переключение настроек площадок не откатилось целиком: закройте то, что держит файлы " +
                               $"плагинов, и перезапустите редактор. Копия и журнал — в {PRSDKProjectSaves.Root}.");
                return false;
            case PRSDKPlatformSettingsSwitch.Recovery.Unreadable:
                Debug.LogError($"[PRUnitySDK] Журнал переключения настроек площадок ({operation.JournalPath}) не прочитать: сверьте файлы " +
                               $"плагинов с копией в {PRSDKProjectSaves.Root}/PlatformSettingsBackup, затем удалите журнал.");
                return false;
            default:
                return true;
        }
    }

    /// <summary>
    /// Сверяет плагин с копией владельца, когда тот — активный проект: выигрывает более новый файл.
    /// </summary>
    internal static void Refresh(PRSDKProject owner)
    {
        if (owner == null || Operation.HasUnfinished)
            return;

        var loaded = new List<string>();
        var seeded = new List<string>();

        writing = true;
        try
        {
            foreach (FileEntry entry in Entries())
            {
                PRSDKPlatformSettingsFile.LoadResult result =
                    PRSDKPlatformSettingsFile.Load(entry.Live, StoredPath(owner, entry), preferNewer: true, out string error);

                if (result == PRSDKPlatformSettingsFile.LoadResult.Failed)
                    Debug.LogError($"[PRUnitySDK] Настройки площадки {entry.Live} не сверены с проектом {owner.Title}: {error}.");
                else if (result == PRSDKPlatformSettingsFile.LoadResult.Seeded)
                    seeded.Add(entry.Live);
                else if (result == PRSDKPlatformSettingsFile.LoadResult.Loaded)
                    loaded.Add(entry.Live);
            }
        }
        finally
        {
            writing = false;
        }

        Reimport(loaded);
        if (loaded.Count > 0)
            Debug.Log($"[PRUnitySDK] Настройки площадок проекта {owner.Title} обновлены из его копии: {string.Join(", ", loaded)}.");
        if (seeded.Count > 0)
            Debug.Log($"[PRUnitySDK] У проекта {owner.Title} своих настроек площадок не было — взяты нынешние: " +
                      $"{string.Join(", ", seeded)}.");
    }

    /// <summary>
    /// Ассеты в памяти перечитываются с диска; у спрятанного плагина Unity файла не видит.
    /// </summary>
    private static void Reimport(List<string> changed)
    {
        if (changed == null)
            return;

        writing = true;
        try
        {
            foreach (string live in changed.Where(path => !path.Contains(PRModulePlugins.HiddenSuffix + "/")))
                AssetDatabase.ImportAsset(live, ImportAssetOptions.ForceUpdate);
        }
        finally
        {
            writing = false;
        }
    }

    private static PRSDKProject ProjectOf(string guid) =>
        string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadAssetAtPath<PRSDKProject>(AssetDatabase.GUIDToAssetPath(guid));

    private static string StoredPath(PRSDKProject project, FileEntry entry) => $"{DataFolder(project)}/{FolderName}/{entry.ModuleId}/{entry.Relative}";

    /// <summary>
    /// Папка данных проекта: где его PRSDKSettings; настроек нет — папка с именем проекта рядом с его ассетом.
    /// </summary>
    private static string DataFolder(PRSDKProject project)
    {
        string projectPath = AssetDatabase.GetAssetPath(project);
        string settingsPath = project.Settings != null ? AssetDatabase.GetAssetPath(project.Settings) : string.Empty;
        string folder = settingsPath.Length > 0
            ? Path.GetDirectoryName(settingsPath)
            : Path.Combine(Path.GetDirectoryName(projectPath) ?? string.Empty, Path.GetFileNameWithoutExtension(projectPath));
        return folder!.Replace('\\', '/');
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

            string folder = PRSDKPlatformSettingsSwitch.LiveFolder(manifest.PluginFolder, PRModulePlugins.HiddenSuffix);
            if (folder == null)
                continue;

            bool visible = folder == manifest.PluginFolder;
            foreach (string relative in manifest.ProjectSettingsFiles)
                yield return new FileEntry(manifest.Id, $"{folder}/{relative}", relative, visible);
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
    /// Правка настроек в плагине уходит в копию владельца, если он и есть активный проект.
    /// </summary>
    private sealed class Postprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            // Сменился манифест — список файлов перечитается.
            if (imported.Concat(moved).Any(path => AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(PRModuleManifest)))
                manifests = null;

            if (writing || imported.Length == 0 || Operation.HasUnfinished)
                return;

            // Чьи настройки — решает владелец: посреди сбоя или до переключения указатель мог уйти к другой игре.
            PRSDKProject owner = PRSDKProjectSaves.ActiveOwner;
            if (owner == null)
                return;

            var changed = new HashSet<string>(imported, StringComparer.OrdinalIgnoreCase);
            foreach (FileEntry entry in Entries())
            {
                if (!entry.Visible || !changed.Contains(entry.Live))
                    continue;

                PRSDKPlatformSettingsFile.Save(entry.Live, StoredPath(owner, entry), out string error);
                if (error != null)
                    Debug.LogError($"[PRUnitySDK] Правка {entry.Live} не запомнена у проекта {owner.Title}: {error}.");
            }
        }
    }
}
