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
/// При смене проекта файлы площадок (<see cref="IPRSDKEditorSaveStore"/>) копируются в папку прежнего проекта
/// (<c>&lt;папка данных проекта&gt;/Editor/Saves/&lt;площадка&gt;</c>), а на их место ложатся файлы нового. Если у
/// нового проекта сохранения ещё нет, файлы площадки удаляются — игра начнётся с чистого сохранения. Папка
/// <c>Editor</c> в сборку не попадает.
/// </para>
/// <para>
/// Чьё сохранение сейчас лежит у площадки, помнит <c>EditorPrefs</c> (ключ свой у каждого Unity-проекта). В первый
/// запуск владельцем считается активный проект. Во время игры переключение откладывается до выхода из неё:
/// площадка держит сохранение открытым и записала бы его поверх.
/// </para>
/// </remarks>
[InitializeOnLoad]
public static class PRSDKProjectSaves
{
    // Ключ с путём проекта: EditorPrefs общие на все Unity-проекты машины.
    private static readonly string OwnerKey = "PRSDK.SaveOwner." + Application.dataPath;

    private static bool pending;

    static PRSDKProjectSaves()
    {
        PRSDKActiveProject.EditedInEditor += Sync;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += InitializeOwner;
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
    public static string GetFolder(PRSDKProject project, string platform)
    {
        string path = AssetDatabase.GetAssetPath(project);
        if (string.IsNullOrEmpty(path))
            return null;

        string directory = Path.GetDirectoryName(path)?.Replace('\\', '/');
        return $"{directory}/{Path.GetFileNameWithoutExtension(path)}/Editor/Saves/{platform}";
    }

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

        var owner = AssetDatabase.LoadAssetAtPath<PRSDKProject>(AssetDatabase.GUIDToAssetPath(ownerGuid));

        foreach (IPRSDKEditorSaveStore store in Stores)
        {
            // Прежний проект удалён — его сохранение некуда убрать, оно просто уступает место новому.
            if (owner != null)
                Stash(store, GetFolder(owner, store.Platform));

            Restore(store, GetFolder(active, store.Platform));
        }

        EditorPrefs.SetString(OwnerKey, activeGuid);
        AssetDatabase.Refresh();
        Debug.Log($"[PRUnitySDK] Сохранения площадок переключены: {(owner != null ? owner.Title : "удалённый проект")} → {active.Title}.");
    }

    /// <summary>
    /// Копирует файлы площадки в папку проекта; файла нет — убирает и старую копию: сохранение было пустым.
    /// </summary>
    private static void Stash(IPRSDKEditorSaveStore store, string folder)
    {
        if (string.IsNullOrEmpty(folder))
            return;

        Directory.CreateDirectory(folder);
        foreach (string file in store.Files)
        {
            string copy = Path.Combine(folder, Path.GetFileName(file));
            if (File.Exists(file))
                File.Copy(file, copy, overwrite: true);
            else
                DeleteWithMeta(copy);
        }
    }

    /// <summary>
    /// Кладёт файлы проекта на место файлов площадки; копии нет — удаляет файл, и площадка создаст новое сохранение.
    /// </summary>
    private static void Restore(IPRSDKEditorSaveStore store, string folder)
    {
        foreach (string file in store.Files)
        {
            string copy = string.IsNullOrEmpty(folder) ? null : Path.Combine(folder, Path.GetFileName(file));
            if (copy != null && File.Exists(copy))
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

    private static void DeleteWithMeta(string file)
    {
        if (File.Exists(file))
            File.Delete(file);
        if (File.Exists(file + ".meta"))
            File.Delete(file + ".meta");
    }

    private static string GuidOf(PRSDKProject project) => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(project));

    private static void InitializeOwner()
    {
        if (string.IsNullOrEmpty(EditorPrefs.GetString(OwnerKey, string.Empty)))
            Sync();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode && pending)
            Sync();
    }
}
