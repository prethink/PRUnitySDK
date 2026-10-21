using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Файловая часть переключения сохранений площадок между проектами SDK: резервная копия, журнал, подмена, откат.
/// </summary>
/// <remarks>
/// Отделена от <see cref="PRSDKProjectSaves"/>, чтобы её проверяли тесты на своей папке и своих площадках.
/// <para>
/// Порядок, который держит данные целыми при исключении и при аварийном завершении редактора:
/// </para>
/// <list type="number">
/// <item>Резервная копия собирается в <c>Backup.new</c> и подменяет <c>Backup</c> только целиком (прежняя
/// уходит в <c>Backup.old</c> и удаляется после подмены). Не собралась — файлы площадок не трогаются.</item>
/// <item>Журнал <c>Switch.txt</c> («откуда», «куда») пишется после готовой копии: по нему видно, что операция
/// начата и откатывать есть из чего.</item>
/// <item>Файлы площадок уходят в папку прежнего проекта, на их место ложатся файлы нового. Исключение —
/// откат из копии.</item>
/// <item>Вызывающий запоминает нового владельца, и только потом журнал удаляется. Запомнить не вышло — откат из
/// копии, как при сбое подмены.</item>
/// </list>
/// После аварии журнал остаётся: <see cref="RecoverInterrupted"/> при следующем запуске либо признаёт
/// переключение законченным (владелец уже новый), либо возвращает файлы из копии. Журнал удаляется, только когда
/// откат прошёл целиком: иначе файлы площадок от разных проектов, и об этом должно остаться напоминание
/// (<see cref="HasUnfinished"/> — запуск игры и новое переключение отказывают, пока оно есть).
/// <para>
/// <c>.meta</c> файлов площадок ходят вместе с ними — в копии, в папке проекта и обратно: на файл сохранения
/// внутри Assets могут ссылаться, и новый GUID после возврата эти ссылки потерял бы. Нет своего <c>.meta</c> у
/// восстанавливаемого сохранения — чужой удаляется, и Unity заводит новый.
/// </para>
/// </remarks>
public sealed class PRSDKSaveSwitch
{
    private const string MetaExtension = ".meta";

    private readonly string root;
    private readonly IReadOnlyList<IPRSDKEditorSaveStore> stores;

    public PRSDKSaveSwitch(string root, IReadOnlyList<IPRSDKEditorSaveStore> stores)
    {
        this.root = root;
        this.stores = stores;
    }

    private string BackupRoot => Path.Combine(root, "Backup");
    private string NewBackupRoot => Path.Combine(root, "Backup.new");
    private string OldBackupRoot => Path.Combine(root, "Backup.old");

    /// <summary>
    /// Журнал незаконченного переключения.
    /// </summary>
    public string JournalPath => Path.Combine(root, "Switch.txt");

    /// <summary>
    /// Переключение не закончено или откат не вернул всех файлов: файлы площадок могут быть от разных проектов.
    /// </summary>
    public bool HasUnfinished => File.Exists(JournalPath);

    /// <summary>
    /// Чем кончилось восстановление после прерванного переключения.
    /// </summary>
    public enum Recovery
    {
        /// <summary>Журнала не было.</summary>
        None,
        /// <summary>Переключение успели закончить: владелец уже новый.</summary>
        Completed,
        /// <summary>Файлы возвращены из копии целиком.</summary>
        RolledBack,
        /// <summary>Часть файлов не вернулась: журнал и копия остаются.</summary>
        Incomplete
    }

    /// <summary>
    /// Папка сохранений проекта для площадки.
    /// </summary>
    public string GetFolder(string projectGuid, string platform) => Path.Combine(root, "Projects", projectGuid, platform);

    /// <summary>
    /// Переключает файлы площадок с проекта <paramref name="from"/> на <paramref name="to"/>.
    /// </summary>
    /// <param name="commit">Запоминает нового владельца; зовётся после подмены файлов, до удаления журнала.</param>
    /// <param name="error">Почему не вышло; файлы площадок при этом прежние.</param>
    /// <returns><c>false</c> — переключения не было.</returns>
    public bool TrySwitch(string from, string to, Action commit, out string error)
    {
        error = null;
        try
        {
            Backup();
        }
        catch (Exception exception)
        {
            error = "не удалась резервная копия, файлы площадок не тронуты: " + exception.Message;
            return false;
        }

        try
        {
            File.WriteAllLines(JournalPath, new[] { from, to });
        }
        catch (Exception exception)
        {
            error = "не записан журнал переключения, файлы площадок не тронуты: " + exception.Message;
            return false;
        }

        try
        {
            foreach (IPRSDKEditorSaveStore store in stores)
            {
                Stash(store, GetFolder(from, store.Platform));
                Restore(store, GetFolder(to, store.Platform));
            }
        }
        catch (Exception exception)
        {
            List<string> failed = RollBack();
            if (failed.Count == 0)
            {
                DeleteJournal();
                error = "прежние возвращены на место: " + exception.Message;
            }
            else
            {
                // Файлы от разных проектов: журнал остаётся, игра не запустится, пока откат не доделан.
                error = exception.Message + ". Не вернулись: " + string.Join(", ", failed)
                        + $" — копия в {BackupRoot}, журнал {JournalPath} оставлен";
            }
            return false;
        }

        try
        {
            commit?.Invoke();
        }
        catch (Exception exception)
        {
            // Владелец не записан — остался прежний, и файлы должны быть его: иначе запись и файлы разойдутся.
            List<string> failed = RollBack();
            if (failed.Count == 0)
            {
                DeleteJournal();
                error = "не записан новый владелец, прежние файлы возвращены на место: " + exception.Message;
            }
            else
            {
                error = "не записан новый владелец: " + exception.Message + ". Не вернулись: " + string.Join(", ", failed)
                        + $" — копия в {BackupRoot}, журнал {JournalPath} оставлен";
            }
            return false;
        }

        DeleteJournal();
        return true;
    }

    /// <summary>
    /// Доводит до конца переключение, прерванное аварией: журнал есть — его не закончили.
    /// </summary>
    /// <param name="owner">Чьё сохранение по записи владельца.</param>
    /// <returns>Владелец после восстановления: <paramref name="owner"/>, если журнала нет, переключение успели
    /// закончить или откат не удался; проект «откуда», если файлы возвращены из копии.</returns>
    public string RecoverInterrupted(string owner, out Recovery result)
    {
        result = Recovery.None;
        if (!File.Exists(JournalPath))
            return owner;

        string[] lines = File.ReadAllLines(JournalPath);
        string from = lines.Length > 0 ? lines[0] : null;
        string to = lines.Length > 1 ? lines[1] : null;

        // Владелец уже новый: файлы подменили и запомнили, не успели только убрать журнал.
        if (!string.IsNullOrEmpty(to) && owner == to)
        {
            DeleteJournal();
            result = Recovery.Completed;
            return owner;
        }

        // Журнал пишется после готовой копии: по ней файлы возвращаются к виду до переключения.
        if (RollBack().Count > 0)
        {
            result = Recovery.Incomplete;
            return owner;
        }

        result = Recovery.RolledBack;
        DeleteJournal();
        return string.IsNullOrEmpty(from) ? owner : from;
    }

    /// <summary>
    /// Копирует файлы площадки с их <c>.meta</c> в папку проекта; файла нет — убирает и старую копию: сохранение было
    /// пустым.
    /// </summary>
    private static void Stash(IPRSDKEditorSaveStore store, string folder)
    {
        Directory.CreateDirectory(folder);
        foreach (string file in store.Files)
        {
            string copy = Path.Combine(folder, Path.GetFileName(file));
            if (File.Exists(file))
                File.Copy(file, copy, overwrite: true);
            else
                DeleteWithMeta(copy);

            if (File.Exists(file + MetaExtension))
                File.Copy(file + MetaExtension, copy + MetaExtension, overwrite: true);
            else if (File.Exists(copy + MetaExtension))
                File.Delete(copy + MetaExtension);
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
                // Свой .meta проекта — его GUID; своего нет — чужой убирается, и Unity заведёт новый:
                // иначе следующее переключение сохранило бы чужой GUID как свой.
                PlaceMeta(copy, file);
            }
            else
            {
                DeleteWithMeta(file);
            }
        }
    }

    private string BackupFolder(string backupRoot, IPRSDKEditorSaveStore store) => Path.Combine(backupRoot, store.Platform);

    /// <summary>
    /// Копия нынешних файлов площадок с их <c>.meta</c>; файла нет — копии тоже нет.
    /// </summary>
    private void Backup()
    {
        DeleteFolder(NewBackupRoot);
        DeleteFolder(OldBackupRoot);
        Directory.CreateDirectory(NewBackupRoot);
        foreach (IPRSDKEditorSaveStore store in stores)
        {
            string folder = BackupFolder(NewBackupRoot, store);
            Directory.CreateDirectory(folder);
            foreach (string file in store.Files)
            {
                string copy = Path.Combine(folder, Path.GetFileName(file));
                if (File.Exists(file))
                    File.Copy(file, copy, overwrite: true);
                if (File.Exists(file + MetaExtension))
                    File.Copy(file + MetaExtension, copy + MetaExtension, overwrite: true);
            }
        }

        // Прежняя копия уходит в сторону, а не удаляется до подмены: в любой момент целая копия есть на диске.
        if (Directory.Exists(BackupRoot))
            Directory.Move(BackupRoot, OldBackupRoot);
        Directory.Move(NewBackupRoot, BackupRoot);
        DeleteFolder(OldBackupRoot);
    }

    /// <summary>
    /// Возвращает файлы площадок и их <c>.meta</c>, какими они были до переключения. Нет файла в копии — его не было
    /// и на месте: файл удаляется вместе с <c>.meta</c>.
    /// </summary>
    /// <returns>Файлы, которые вернуть не удалось.</returns>
    private List<string> RollBack()
    {
        var failed = new List<string>();
        foreach (IPRSDKEditorSaveStore store in stores)
        {
            foreach (string file in store.Files)
            {
                try
                {
                    string copy = Path.Combine(BackupFolder(BackupRoot, store), Path.GetFileName(file));
                    if (File.Exists(copy))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(file) ?? string.Empty);
                        // Файл, который не успели тронуть, не перезаписывается: его могут держать открытым,
                        // и откат зря счёл бы себя неполным.
                        CopyIfDiffers(copy, file);
                        if (File.Exists(copy + MetaExtension))
                            CopyIfDiffers(copy + MetaExtension, file + MetaExtension);
                        else if (File.Exists(file + MetaExtension))
                            File.Delete(file + MetaExtension);
                    }
                    else
                    {
                        DeleteWithMeta(file);
                    }
                }
                catch (Exception)
                {
                    failed.Add(file);
                }
            }
        }
        return failed;
    }

    /// <summary>
    /// <c>.meta</c> к файлу на месте: из копии, а без него в копии — никакого.
    /// </summary>
    private static void PlaceMeta(string copy, string file)
    {
        if (File.Exists(copy + MetaExtension))
            File.Copy(copy + MetaExtension, file + MetaExtension, overwrite: true);
        else if (File.Exists(file + MetaExtension))
            File.Delete(file + MetaExtension);
    }

    private static void CopyIfDiffers(string source, string target)
    {
        if (File.Exists(target) && SameContent(source, target))
            return;
        File.Copy(source, target, overwrite: true);
    }

    private static bool SameContent(string a, string b)
    {
        var first = new FileInfo(a);
        var second = new FileInfo(b);
        if (first.Length != second.Length)
            return false;
        // Чтение с общим доступом: файл, открытый площадкой на чтение, сравнить можно.
        using var left = new FileStream(a, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var right = new FileStream(b, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        int x, y;
        do
        {
            x = left.ReadByte();
            y = right.ReadByte();
            if (x != y) return false;
        } while (x != -1);
        return true;
    }

    private void DeleteJournal()
    {
        if (File.Exists(JournalPath))
            File.Delete(JournalPath);
    }

    private static void DeleteWithMeta(string file)
    {
        if (File.Exists(file))
            File.Delete(file);
        if (File.Exists(file + MetaExtension))
            File.Delete(file + MetaExtension);
    }

    private static void DeleteFolder(string folder)
    {
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
    }
}
