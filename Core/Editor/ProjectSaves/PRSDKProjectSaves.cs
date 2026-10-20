using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Своё сохранение у каждого проекта SDK: при смене проекта сохранение площадок переключается вместе с ним.
/// </summary>
/// <remarks>
/// Площадка пишет сохранение в одно место (у Яндекса — <c>Assets/PluginYourGames/Editor/SavesEditorYG2.json</c>),
/// и без переключения игры в одном Unity-проекте делили бы прогресс: StealEgg читал бы сохранение PickBox.
/// <para>
/// При смене проекта файлы площадок (<see cref="IPRSDKEditorSaveStore"/>) копируются в папку прежнего проекта,
/// а на их место ложатся файлы нового. Если у нового проекта сохранения ещё нет, файлы площадки удаляются — игра
/// начнётся с чистого сохранения.
/// </para>
/// <para>
/// Сохранения проектов лежат в <c>UserSettings/PRSDKProjectSaves/Projects/&lt;guid проекта&gt;/&lt;площадка&gt;</c>:
/// по GUID, а не по имени, — переименование ассета проекта их не теряет, и удалённый проект оставляет своё
/// сохранение на месте. <c>UserSettings</c> не импортируется как ассеты, не попадает в сборку и переживает
/// удаление <c>Library</c>.
/// </para>
/// <para>
/// Переключение — одна операция: перед ней файлы площадок копируются в <c>…/Backup</c>, при любой ошибке они
/// возвращаются оттуда, а владелец меняется только после успеха всех площадок. Чьё сохранение сейчас у площадки,
/// помнит <c>EditorPrefs</c> (ключ свой у каждого Unity-проекта); в первый запуск владелец — активный проект.
/// Во время игры переключение ждёт выхода из неё: площадка держит сохранение открытым и записала бы его поверх.
/// </para>
/// </remarks>
[InitializeOnLoad]
public static class PRSDKProjectSaves
{
    /// <summary>
    /// Где лежат сохранения проектов и служебные копии.
    /// </summary>
    public const string Root = "UserSettings/PRSDKProjectSaves";

    // Ключ с путём проекта: EditorPrefs общие на все Unity-проекты машины.
    private static readonly string OwnerKey = "PRSDK.SaveOwner." + Application.dataPath;

    private static bool pending;

    static PRSDKProjectSaves()
    {
        PRSDKActiveProject.EditedInEditor += Sync;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += Initialize;
    }

    /// <summary>
    /// Площадки, чьи сохранения переключаются.
    /// </summary>
    public static IReadOnlyList<IPRSDKEditorSaveStore> Stores =>
        TypeCache.GetTypesDerivedFrom<IPRSDKEditorSaveStore>()
            .Where(type => !type.IsAbstract && !type.IsInterface && type.GetConstructor(Type.EmptyTypes) != null)
            .Select(type => (IPRSDKEditorSaveStore)Activator.CreateInstance(type))
            .ToList();

    /// <summary>
    /// Папка сохранений проекта для площадки.
    /// </summary>
    public static string GetFolder(string projectGuid, string platform) => $"{Root}/Projects/{projectGuid}/{platform}";

    /// <summary>
    /// Переключает сохранения площадок на активный проект, если они ещё от другого.
    /// </summary>
    public static void Sync()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            pending = true;
            return;
        }

        pending = false;
        PRSDKProject active = PRSDKActiveProject.Instance != null ? PRSDKActiveProject.Instance.Project : null;
        if (active == null)
            return;

        string activeGuid = GuidOf(active);
        string ownerGuid = EditorPrefs.GetString(OwnerKey, string.Empty);

        if (string.IsNullOrEmpty(ownerGuid))
        {
            EditorPrefs.SetString(OwnerKey, activeGuid);
            return;
        }

        if (ownerGuid == activeGuid)
            return;

        IReadOnlyList<IPRSDKEditorSaveStore> stores = Stores;
        try
        {
            Backup(stores);
            foreach (IPRSDKEditorSaveStore store in stores)
            {
                Stash(store, GetFolder(ownerGuid, store.Platform));
                Restore(store, GetFolder(activeGuid, store.Platform));
            }
        }
        catch (Exception exception)
        {
            RollBack(stores);
            Debug.LogError($"[PRUnitySDK] Сохранения площадок не переключены, прежние возвращены на место: {exception.Message}");
            AssetDatabase.Refresh();
            return;
        }

        EditorPrefs.SetString(OwnerKey, activeGuid);
        AssetDatabase.Refresh();
        Debug.Log($"[PRUnitySDK] Сохранения площадок переключены: {TitleOf(ownerGuid)} → {active.Title}.");
    }

    /// <summary>
    /// Копирует файлы площадки в папку проекта; файла нет — убирает и старую копию: сохранение было пустым.
    /// </summary>
    private static void Stash(IPRSDKEditorSaveStore store, string folder)
    {
        Directory.CreateDirectory(folder);
        foreach (string file in store.Files)
        {
            string copy = Path.Combine(folder, Path.GetFileName(file));
            if (File.Exists(file))
                File.Copy(file, copy, overwrite: true);
            else if (File.Exists(copy))
                File.Delete(copy);
        }
    }

    /// <summary>
    /// Кладёт файлы проекта на место файлов площадки; копии нет — удаляет файл, и площадка создаст новое сохранение.
    /// </summary>
    private static void Restore(IPRSDKEditorSaveStore store, string folder)
    {
        foreach (string file in store.Files)
        {
            string copy = Path.Combine(folder, Path.GetFileName(file));
            if (File.Exists(copy))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file) ?? string.Empty);
                File.Copy(copy, file, overwrite: true);
            }
            else
            {
                DeleteWithMeta(file);
            }
        }
    }

    private static string BackupFolder(IPRSDKEditorSaveStore store) => $"{Root}/Backup/{store.Platform}";

    /// <summary>
    /// Копия нынешних файлов площадок до переключения; файла нет — копии тоже нет.
    /// </summary>
    private static void Backup(IEnumerable<IPRSDKEditorSaveStore> stores)
    {
        foreach (IPRSDKEditorSaveStore store in stores)
        {
            string folder = BackupFolder(store);
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
            Directory.CreateDirectory(folder);
            foreach (string file in store.Files)
                if (File.Exists(file))
                    File.Copy(file, Path.Combine(folder, Path.GetFileName(file)), overwrite: true);
        }
    }

    /// <summary>
    /// Возвращает файлы площадок, какими они были до переключения.
    /// </summary>
    private static void RollBack(IEnumerable<IPRSDKEditorSaveStore> stores)
    {
        foreach (IPRSDKEditorSaveStore store in stores)
        {
            foreach (string file in store.Files)
            {
                try
                {
                    string copy = Path.Combine(BackupFolder(store), Path.GetFileName(file));
                    if (File.Exists(copy)) File.Copy(copy, file, overwrite: true);
                    else DeleteWithMeta(file);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[PRUnitySDK] Не вернул {file}, копия лежит в {BackupFolder(store)}: {exception.Message}");
                }
            }
        }
    }

    private static void DeleteWithMeta(string file)
    {
        if (File.Exists(file))
            File.Delete(file);
        if (File.Exists(file + ".meta"))
            File.Delete(file + ".meta");
    }

    private static string GuidOf(PRSDKProject project) => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(project));

    private static string TitleOf(string guid)
    {
        var project = AssetDatabase.LoadAssetAtPath<PRSDKProject>(AssetDatabase.GUIDToAssetPath(guid));
        return project != null ? project.Title : $"удалённый проект {guid}";
    }

    private static void Initialize()
    {
        MigrateFromAssets();
        if (string.IsNullOrEmpty(EditorPrefs.GetString(OwnerKey, string.Empty)))
            Sync();
    }

    /// <summary>
    /// Переносит сохранения из прежнего места — <c>&lt;папка данных проекта&gt;/Editor/Saves</c> внутри Assets — в
    /// папку по GUID. Там они зависели от имени проекта и импортировались как ассеты.
    /// </summary>
    private static void MigrateFromAssets()
    {
        bool moved = false;
        foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(PRSDKProject)))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string editor = $"{Path.GetDirectoryName(path)?.Replace('\\', '/')}/{Path.GetFileNameWithoutExtension(path)}/Editor";
            string old = editor + "/Saves";
            if (!Directory.Exists(old))
                continue;

            foreach (string platformFolder in Directory.GetDirectories(old))
            {
                string target = GetFolder(guid, Path.GetFileName(platformFolder));
                Directory.CreateDirectory(target);
                foreach (string file in Directory.GetFiles(platformFolder).Where(file => !file.EndsWith(".meta")))
                {
                    string copy = Path.Combine(target, Path.GetFileName(file));
                    if (!File.Exists(copy))
                        File.Copy(file, copy);
                }
            }

            AssetDatabase.DeleteAsset(old);
            if (AssetDatabase.IsValidFolder(editor) && !Directory.EnumerateFileSystemEntries(editor).Any())
                AssetDatabase.DeleteAsset(editor);
            moved = true;
            Debug.Log($"[PRUnitySDK] Сохранения проекта {Path.GetFileNameWithoutExtension(path)} перенесены из {old} в {Root}/Projects/{guid}.");
        }

        if (moved)
            AssetDatabase.Refresh();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode && pending)
            Sync();
    }
}
