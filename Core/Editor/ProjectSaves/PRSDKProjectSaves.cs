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
/// Переключение — одна операция (<see cref="PRSDKSaveSwitch"/>: резервная копия, журнал, откат): владелец меняется
/// только после успеха всех площадок, и только после этого меняется активный проект (<see cref="TrySwitchTo"/>).
/// Если проект сменили в обход окна (инспектором, Undo) и переключить сохранения не вышло, указатель возвращается
/// к владельцу. Запуск игры при несовпадении владельца и активного проекта сначала пробует переключить, а не
/// вышло — останавливается: иначе игра прочитала бы и перезаписала чужой прогресс. Прерванное аварией
/// переключение доводится при следующем запуске редактора. Чьё сохранение сейчас у площадки,
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

    private static PRSDKSaveSwitch Switch => new(Root, Stores);

    private static string Owner
    {
        get => EditorPrefs.GetString(OwnerKey, string.Empty);
        set => EditorPrefs.SetString(OwnerKey, value);
    }

    private static PRSDKProject Active => PRSDKActiveProject.Instance != null ? PRSDKActiveProject.Instance.Project : null;

    /// <summary>
    /// Переключает сохранения площадок на <paramref name="target"/>. Активный проект меняет вызывающий — и только
    /// при успехе.
    /// </summary>
    /// <returns><c>false</c> — сохранения остались от прежнего проекта, причина в консоли.</returns>
    public static bool TrySwitchTo(PRSDKProject target)
    {
        if (target == null)
            return false;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[PRUnitySDK] Проект SDK меняется вне игры: площадка держит сохранение открытым.");
            return false;
        }

        if (!Recover())
        {
            Debug.LogError($"[PRUnitySDK] Сохранения площадок не переключены на {target.Title}: прошлое переключение не доведено, " +
                           $"файлы площадок могут быть от разных проектов — копия и журнал в {Root}.");
            return false;
        }

        string to = GuidOf(target);
        string from = Owner;
        if (string.IsNullOrEmpty(from))
        {
            Owner = to;
            return true;
        }

        if (from == to)
            return true;

        bool switched = Switch.TrySwitch(from, to, () => Owner = to, out string error);
        AssetDatabase.Refresh();
        if (!switched)
        {
            Debug.LogError($"[PRUnitySDK] Сохранения площадок не переключены на {target.Title}: {error}");
            return false;
        }

        Debug.Log($"[PRUnitySDK] Сохранения площадок переключены: {TitleOf(from)} → {target.Title}.");
        return true;
    }

    /// <summary>
    /// Переключает сохранения площадок на активный проект, если они ещё от другого; не вышло — возвращает
    /// активным проект-владелец.
    /// </summary>
    /// <remarks>
    /// Для смены проекта в обход окна проектов — инспектором указателя или Undo.
    /// </remarks>
    public static void Sync()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            pending = true;
            return;
        }

        pending = false;
        PRSDKProject active = Active;
        if (active == null || TrySwitchTo(active))
            return;

        PRSDKProject owner = ProjectOf(Owner);
        if (owner == null)
        {
            Debug.LogError($"[PRUnitySDK] Проект-владелец сохранений ({Owner}) не найден: игра с {active.Title} не запустится, " +
                           "пока сохранения не переключатся. Сделайте проект активным в окне проектов ещё раз.");
            return;
        }

        PRSDKActiveProject.Instance.SetProject(owner);
        Debug.LogError($"[PRUnitySDK] Активным оставлен {owner.Title}: сохранения площадок от него.");
    }

    private static PRSDKProject ProjectOf(string guid) =>
        string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadAssetAtPath<PRSDKProject>(AssetDatabase.GUIDToAssetPath(guid));

    private static string GuidOf(PRSDKProject project) =>
        project != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(project)) : string.Empty;

    private static string TitleOf(string guid)
    {
        var project = AssetDatabase.LoadAssetAtPath<PRSDKProject>(AssetDatabase.GUIDToAssetPath(guid));
        return project != null ? project.Title : $"удалённый проект {guid}";
    }

    private static void Initialize()
    {
        MigrateFromAssets();
        if (Recover() && (string.IsNullOrEmpty(Owner) || Owner != GuidOf(Active)))
            Sync();
    }

    /// <summary>
    /// Доводит прерванное переключение, если оно было.
    /// </summary>
    /// <returns><c>false</c> — откат не вернул всех файлов, и переключать дальше нельзя.</returns>
    private static bool Recover()
    {
        PRSDKSaveSwitch saves = Switch;
        if (!saves.HasUnfinished)
            return true;

        string recovered = saves.RecoverInterrupted(Owner, out PRSDKSaveSwitch.Recovery result);
        switch (result)
        {
            case PRSDKSaveSwitch.Recovery.RolledBack:
                Owner = recovered;
                AssetDatabase.Refresh();
                Debug.LogWarning($"[PRUnitySDK] Прошлое переключение сохранений прервалось: файлы площадок возвращены, владелец — {TitleOf(recovered)}.");
                return true;
            case PRSDKSaveSwitch.Recovery.Incomplete:
                Debug.LogError($"[PRUnitySDK] Прошлое переключение сохранений не откатилось целиком: файлы площадок могут быть от разных проектов. " +
                               $"Закройте то, что держит файлы сохранения, и перезапустите редактор; копия и журнал — в {Root}.");
                return false;
            default:
                return true;
        }
    }

    /// <summary>
    /// Переносит сохранения из прежнего места — <c>&lt;папка данных проекта&gt;/Editor/Saves</c> внутри Assets — в
    /// папку по GUID. Там они зависели от имени проекта и импортировались как ассеты.
    /// </summary>
    /// <remarks>
    /// Прежняя папка удаляется, только когда перенесены все её файлы. Если в новой папке уже лежит другой файл
    /// с тем же именем (перенос прерывали), на месте остаётся более свежий, а второй ложится рядом
    /// (<see cref="KeepBoth"/>): какой из двух нужен, по содержимому не понять.
    /// </remarks>
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

            try
            {
                foreach (string platformFolder in Directory.GetDirectories(old))
                {
                    string target = GetFolder(guid, Path.GetFileName(platformFolder));
                    Directory.CreateDirectory(target);
                    foreach (string file in Directory.GetFiles(platformFolder).Where(file => !file.EndsWith(".meta")))
                    {
                        string copy = Path.Combine(target, Path.GetFileName(file));
                        if (!File.Exists(copy))
                            File.Copy(file, copy);
                        else if (!SameContent(file, copy))
                            KeepBoth(file, copy);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[PRUnitySDK] Сохранения проекта {Path.GetFileNameWithoutExtension(path)} не перенесены, {old} оставлена на месте: {exception.Message}");
                continue;
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

    private static bool SameContent(string a, string b) =>
        new FileInfo(a).Length == new FileInfo(b).Length && File.ReadAllBytes(a).SequenceEqual(File.ReadAllBytes(b));

    /// <summary>
    /// Два разных сохранения под одним именем: свежее остаётся в <paramref name="copy"/>, второе ложится рядом
    /// с пометкой «older» и датой — площадка его не читает, но оно не потеряно.
    /// </summary>
    private static void KeepBoth(string file, string copy)
    {
        bool sourceNewer = File.GetLastWriteTimeUtc(file) > File.GetLastWriteTimeUtc(copy);
        string older = sourceNewer ? copy : file;
        string aside = Path.Combine(Path.GetDirectoryName(copy) ?? string.Empty,
            $"{Path.GetFileNameWithoutExtension(copy)}.older-{File.GetLastWriteTime(older):yyyyMMdd-HHmmss}{Path.GetExtension(copy)}");
        File.Copy(older, aside, overwrite: true);
        if (sourceNewer)
            File.Copy(file, copy, overwrite: true);
        Debug.LogWarning($"[PRUnitySDK] При переносе {file} в {copy} сохранения разные: оставлено более свежее, второе — {aside}.");
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode && pending)
            Sync();
        else if (state == PlayModeStateChange.ExitingEditMode)
            GuardPlay();
    }

    /// <summary>
    /// Игра не запускается с сохранением чужого проекта: прочитала бы и перезаписала его прогресс.
    /// </summary>
    private static void GuardPlay()
    {
        if (!Recover())
        {
            EditorApplication.isPlaying = false;
            Debug.LogError("[PRUnitySDK] Запуск остановлен: прошлое переключение сохранений не доведено, файлы площадок могут быть от разных проектов.");
            return;
        }

        PRSDKProject active = Active;
        string owner = Owner;
        if (active == null || string.IsNullOrEmpty(owner) || owner == GuidOf(active))
            return;

        // В ExitingEditMode isPlayingOrWillChangePlaymode уже true — переключение идёт мимо проверки игры.
        bool switched = Switch.TrySwitch(owner, GuidOf(active), () => Owner = GuidOf(active), out string error);
        AssetDatabase.Refresh();
        if (switched)
            return;

        EditorApplication.isPlaying = false;
        Debug.LogError($"[PRUnitySDK] Запуск остановлен: сохранения площадок от {TitleOf(owner)}, активный проект — {active.Title}, " +
                       $"а переключить не вышло: {error}");
    }
}
