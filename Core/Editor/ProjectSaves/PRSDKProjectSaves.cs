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
/// Площадка пишет сохранение в одно место (у Яндекса — <c>Assets/PluginYourGames/Editor/SavesEditorYG2.json</c>,
/// у VK, Пикабу и Steam — <c>UserSettings/PRSDKEditorSaves</c>), и без переключения игры в одном Unity-проекте
/// делили бы прогресс: StealEgg читал бы сохранение PickBox.
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
/// переключение доводится при следующем запуске редактора. Чьё сохранение сейчас у площадки, записано в
/// <c>Owner.txt</c> рядом с сохранениями проектов — переезжает вместе с ними, если Unity-проект перенесли в другую
/// папку или на другой компьютер. Записи нет — владельцем при запуске редактора становится активный проект: файл
/// площадки лежит в Assets и переезжает вместе с указателем проекта. Посреди сессии сменённый указатель владельцем
/// не назначается (<see cref="UnknownOwner"/>).
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

    // Владелец лежит рядом с сохранениями проектов: переезжает вместе с ними, а не теряется с путём проекта.
    private const string OwnerPath = Root + "/Owner.txt";

    // Прежнее место владельца: EditorPrefs общие на машину, ключ с путём проекта. Читается, пока нет файла.
    private static readonly string LegacyOwnerKey = "PRSDK.SaveOwner." + Application.dataPath;

    private static bool pending;

    // Активный проект, известный до смены указателя: при запуске редактора и после переключений.
    // null — запуск ещё не обработан, пусто — при запуске активного проекта не было.
    private static string lastActive;

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

    private static PRSDKSaveSwitch Switch => new(Root, Stores, () => Owner, value => Owner = value);

    private static string Owner
    {
        get => File.Exists(OwnerPath) ? File.ReadAllText(OwnerPath).Trim() : EditorPrefs.GetString(LegacyOwnerKey, string.Empty);
        set
        {
            // Простой записью, не File.Replace: тот падает, пока свежий файл держит антивирус или индексатор.
            // Оборванную запись при переключении ловит PRSDKSaveSwitch: сверяет записанное и откатывает файлы и владельца.
            Directory.CreateDirectory(Root);
            File.WriteAllText(OwnerPath, value);
            EditorPrefs.DeleteKey(LegacyOwnerKey);
        }
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
        if (!TryReadOwner(out string from))
            return false;
        if (string.IsNullOrEmpty(from))
        {
            from = UnknownOwner(target);
            if (string.IsNullOrEmpty(from))
            {
                Debug.LogError($"[PRUnitySDK] Сохранения площадок не переключены на {target.Title}: неизвестно, чьи они сейчас " +
                               $"({OwnerPath} нет), игра не запустится. {WriteOwnerHint}");
                return false;
            }

            if (!BindOwner(from))
                return false;
        }

        if (from == to)
        {
            lastActive = to;
            return true;
        }

        if (!SwitchBoth(from, to, out string error))
        {
            Debug.LogError($"[PRUnitySDK] Сохранения и настройки площадок не переключены на {target.Title}: {error}");
            return false;
        }

        lastActive = to;
        Debug.Log($"[PRUnitySDK] Сохранения площадок переключены: {TitleOf(from)} → {target.Title}.");
        if (error != null)
            Debug.LogWarning($"[PRUnitySDK] {error}.");
        return true;
    }

    /// <summary>
    /// Переключает настройки и сохранения площадок одной операцией: оба или ни одно.
    /// </summary>
    /// <remarks>
    /// Единственный путь переключения — и для окна проектов (<see cref="TrySwitchTo"/>), и для проверки перед игрой
    /// (<see cref="GuardPlay"/>). Сначала настройки (ID игры, покупки): их откатить проще, а владелец у них общий с
    /// сохранениями (<see cref="PRSDKProjectPlatformSettings"/>).
    /// </remarks>
    /// <param name="error">Почему не вышло; при успехе — предупреждение или <c>null</c>.</param>
    private static bool SwitchBoth(string from, string to, out string error)
    {
        if (!PRSDKProjectPlatformSettings.TrySwitch(from, to, out string settingsError))
        {
            error = "настройки площадок не переключены: " + settingsError;
            return false;
        }

        PRSDKSaveSwitch saves = Switch;
        bool switched = saves.TrySwitch(from, to, out error);
        AssetDatabase.Refresh();
        if (!switched)
        {
            // Журнал сохранений остался — их восстановление решит по владельцу, и настройки решат так же: не трогаем.
            if (!saves.HasUnfinished)
                PRSDKProjectPlatformSettings.Revert();
            return false;
        }

        PRSDKProjectPlatformSettings.Commit();
        return true;
    }

    /// <summary>
    /// Чьи сохранения у площадки, когда владелец не записан, — без догадок по уже сменённому указателю.
    /// </summary>
    /// <returns>Пусто — узнать нельзя.</returns>
    /// <remarks>
    /// Файл площадки лежит в Assets рядом с указателем проекта и переезжает вместе с ним, поэтому он от проекта,
    /// который был активным до смены: того, что запомнили при запуске редактора и после переключений
    /// (<see cref="lastActive"/>). При запуске активного не было — сохранение ничьё и достаётся
    /// <paramref name="target"/>. Запуск ещё не обработан — указателю верить нельзя: его могли сменить, и владелец
    /// не назначается, пока его не запишут руками.
    /// </remarks>
    private static string UnknownOwner(PRSDKProject target)
    {
        if (lastActive == null)
            return null;
        return lastActive != string.Empty ? lastActive : GuidOf(target);
    }

    private static string WriteOwnerHint =>
        $"Запишите в {OwnerPath} GUID проекта, чьё это сохранение (строка guid в .meta его ассета).";

    private static bool HasProjectSaves() =>
        Directory.Exists(Root + "/Projects") && Directory.EnumerateFileSystemEntries(Root + "/Projects").Any();

    /// <returns><c>false</c> — записать не вышло, причина в консоли.</returns>
    private static bool BindOwner(string guid)
    {
        try
        {
            Owner = guid;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[PRUnitySDK] Владелец сохранений ({TitleOf(guid)}) не записан в {OwnerPath}: {exception.Message}");
            return false;
        }

        Debug.Log($"[PRUnitySDK] Сохранения площадок записаны за проектом {TitleOf(guid)}: прежний владелец не был записан.");
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

        // Владелец неизвестен или не читается: вернуть некому, причину TrySwitchTo уже написал.
        if (!TryReadOwner(out string ownerGuid) || string.IsNullOrEmpty(ownerGuid))
            return;

        PRSDKProject owner = ProjectOf(ownerGuid);
        if (owner == null)
        {
            Debug.LogError($"[PRUnitySDK] Проект-владелец сохранений ({ownerGuid}) не найден: игра с {active.Title} не запустится, " +
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
        // Владелец из EditorPrefs — в файл сразу: ключ привязан к пути и пропадёт, если проект перенесут.
        // Не вышло — ключ остаётся и читается дальше, перенос повторится при следующем запуске.
        if (!File.Exists(OwnerPath) && !string.IsNullOrEmpty(Owner))
            BindOwner(Owner);

        lastActive = GuidOf(Active);
        if (!Recover() || !TryReadOwner(out string owner))
            return;

        if (string.IsNullOrEmpty(owner) && !string.IsNullOrEmpty(lastActive))
        {
            // Переключений не было — сохранение у площадки одно, и оно от активного проекта: файл и указатель лежат
            // в Assets и переезжают вместе. Были — запись владельца потеряна, а указатель мог разойтись с файлом.
            if (HasProjectSaves())
            {
                lastActive = null;
                Debug.LogError($"[PRUnitySDK] Нет {OwnerPath}, а сохранения проектов в {Root}/Projects есть: неизвестно, чьи сейчас " +
                               $"файлы площадок, и игра не запустится. {WriteOwnerHint}");
                return;
            }

            if (!BindOwner(lastActive))
                return;
            owner = lastActive;
        }

        if (owner != lastActive)
            Sync();
        else
            // Владелец — активный проект: плагин сверяется с его копией настроек (новее копия из git — встаёт она).
            PRSDKProjectPlatformSettings.Refresh(Active);
    }

    /// <summary>
    /// Читает владельца. Файл не читается (держит другая программа) — ошибка в консоли, а не исключение.
    /// </summary>
    /// <remarks>
    /// Пустым такого владельца считать нельзя: пустой назначается заново и перезаписал бы настоящего.
    /// </remarks>
    private static bool TryReadOwner(out string owner)
    {
        try
        {
            owner = Owner;
            return true;
        }
        catch (Exception exception)
        {
            owner = null;
            Debug.LogError($"[PRUnitySDK] Не прочитать {OwnerPath}: {exception.Message}. Сохранения площадок не переключаются, " +
                           "игра не запустится, пока файл не освободится.");
            return false;
        }
    }

    /// <summary>
    /// Доводит прерванное переключение, если оно было.
    /// </summary>
    /// <returns><c>false</c> — откат не вернул всех файлов, и переключать дальше нельзя.</returns>
    private static bool Recover()
    {
        // Сохранения первыми: их восстановление может вернуть владельца, а настройки решают по нему.
        if (!RecoverSaves())
            return false;
        return TryReadOwner(out string owner) && PRSDKProjectPlatformSettings.Recover(owner);
    }

    /// <summary>
    /// Активный проект, если он же владелец сохранений и настроек площадок; иначе <c>null</c>.
    /// </summary>
    /// <remarks>
    /// Без сообщений в консоль: спрашивают на каждом импорте.
    /// </remarks>
    internal static PRSDKProject ActiveOwner
    {
        get
        {
            PRSDKProject active = Active;
            if (active == null)
                return null;
            try
            {
                return Owner == GuidOf(active) ? active : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    private static bool RecoverSaves()
    {
        PRSDKSaveSwitch saves = Switch;
        if (!saves.HasUnfinished)
            return true;

        PRSDKSaveSwitch.Recovery result = saves.RecoverInterrupted();
        switch (result)
        {
            case PRSDKSaveSwitch.Recovery.RolledBack:
                AssetDatabase.Refresh();
                Debug.LogWarning("[PRUnitySDK] Прошлое переключение сохранений прервалось: файлы площадок возвращены, владелец — " +
                                 $"{(TryReadOwner(out string owner) ? TitleOf(owner) : OwnerPath)}.");
                return true;
            case PRSDKSaveSwitch.Recovery.Incomplete:
                AssetDatabase.Refresh();
                Debug.LogError($"[PRUnitySDK] Прошлое переключение сохранений не откатилось целиком: файлы площадок или запись владельца " +
                               $"({OwnerPath}) могут быть от разных проектов. Закройте то, что держит эти файлы, и перезапустите редактор; " +
                               $"копия и журнал — в {Root}.");
                return false;
            case PRSDKSaveSwitch.Recovery.Unreadable:
                Debug.LogError($"[PRUnitySDK] Журнал прошлого переключения сохранений ({saves.JournalPath}) не прочитать или он повреждён: " +
                               "файлы площадок и владелец не тронуты, но могут быть от разных проектов; игра не запустится. " +
                               $"Сверьте файлы площадок с копией в {Root}/Backup и {OwnerPath}, затем удалите журнал.");
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
        if (!TryReadOwner(out string owner))
        {
            if (active != null)
                EditorApplication.isPlaying = false;
            return;
        }

        if (active == null || owner == GuidOf(active))
            return;

        if (string.IsNullOrEmpty(owner))
        {
            EditorApplication.isPlaying = false;
            Debug.LogError($"[PRUnitySDK] Запуск остановлен: неизвестно, чьи сейчас сохранения площадок ({OwnerPath} нет), " +
                           $"а активный проект — {active.Title}. {WriteOwnerHint}");
            return;
        }

        // В ExitingEditMode isPlayingOrWillChangePlaymode уже true — переключение идёт мимо проверки игры.
        // Та же операция, что при смене проекта: иначе сохранения стали бы от активного, а настройки остались бы от прежнего.
        bool switched = SwitchBoth(owner, GuidOf(active), out string error);
        if (switched)
        {
            lastActive = GuidOf(active);
            return;
        }

        EditorApplication.isPlaying = false;
        Debug.LogError($"[PRUnitySDK] Запуск остановлен: сохранения и настройки площадок от {TitleOf(owner)}, активный проект — {active.Title}, " +
                       $"а переключить не вышло: {error}");
    }
}
