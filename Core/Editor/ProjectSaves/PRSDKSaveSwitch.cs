using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Переключение сохранений площадок между проектами SDK: резервная копия, журнал, подмена, запись владельца, откат.
/// </summary>
/// <remarks>
/// Отделено от <see cref="PRSDKProjectSaves"/>, чтобы его проверяли тесты на своей папке, своих площадках и своей
/// записи владельца.
/// <para>
/// Порядок, который держит файлы и запись владельца согласованными при исключении и при аварийном завершении:
/// </para>
/// <list type="number">
/// <item>Резервная копия собирается в <c>Backup.new</c> и подменяет <c>Backup</c> только целиком (прежняя
/// уходит в <c>Backup.old</c> и удаляется после подмены). Не собралась — файлы площадок не трогаются.</item>
/// <item>Журнал <c>Switch.txt</c> («откуда», «куда») пишется после готовой копии: по нему видно, что операция
/// начата и откатывать есть из чего. Пишется в <c>Switch.txt.new</c> и переносится — на месте он только целый;
/// нечитаемый журнал или журнал без двух GUID проектов восстановление не трогает (<see cref="Recovery.Unreadable"/>).</item>
/// <item>Файлы площадок уходят в папку прежнего проекта, на их место ложатся файлы нового. Исключение —
/// откат из копии.</item>
/// <item>Записывается новый владелец и сверяется чтением. Только потом журнал удаляется.</item>
/// </list>
/// Запись владельца могла упасть на полпути: если прочиталось «куда» — переключение состоялось; иначе файлы
/// откатываются и владельцем снова пишется «откуда». Перед откатом в журнал третьей строкой пишется
/// <c>rollback</c>: с этой минуты владелец «куда» ничего не доказывает (файлы уже могут быть от «откуда»), и
/// <see cref="RecoverInterrupted"/> по такому журналу всегда доводит откат. Отметку записать не вышло — откат не
/// начинается: файлы не тронуты, и восстановление решит по владельцу. Журнал удаляется, только когда и файлы,
/// и владелец сошлись с «откуда». Не сошлись — журнал остаётся (<see cref="HasUnfinished"/>: запуск игры и новое
/// переключение отказывают), и <see cref="RecoverInterrupted"/> доводит откат так же: файлы, потом владелец, потом
/// журнал.
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
    private readonly Func<string> readOwner;
    private readonly Action<string> writeOwner;

    /// <param name="readOwner">Чьё сохранение сейчас у площадок (GUID проекта); пусто — не записано.</param>
    /// <param name="writeOwner">Запоминает владельца. Может упасть, в том числе успев записать часть.</param>
    public PRSDKSaveSwitch(string root, IReadOnlyList<IPRSDKEditorSaveStore> stores, Func<string> readOwner, Action<string> writeOwner)
    {
        this.root = root;
        this.stores = stores;
        this.readOwner = readOwner;
        this.writeOwner = writeOwner;
    }

    private string BackupRoot => Path.Combine(root, "Backup");
    private string NewBackupRoot => Path.Combine(root, "Backup.new");
    private string OldBackupRoot => Path.Combine(root, "Backup.old");

    /// <summary>
    /// Журнал незаконченного переключения.
    /// </summary>
    public string JournalPath => Path.Combine(root, "Switch.txt");

    /// <summary>
    /// Переключение не закончено или откат не довёл файлы и владельца до прежних: они могут быть от разных проектов.
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
        /// <summary>Файлы возвращены из копии целиком, владелец — прежний.</summary>
        RolledBack,
        /// <summary>Часть файлов не вернулась или владелец не записался: журнал и копия остаются.</summary>
        Incomplete,
        /// <summary>Журнал не прочитать или в нём нет двух GUID проектов: ничего не тронуто, журнал остаётся.</summary>
        Unreadable
    }

    /// <summary>
    /// Папка сохранений проекта для площадки.
    /// </summary>
    public string GetFolder(string projectGuid, string platform) => Path.Combine(root, "Projects", projectGuid, platform);

    /// <summary>
    /// Переключает файлы площадок с проекта <paramref name="from"/> на <paramref name="to"/> и записывает
    /// владельцем <paramref name="to"/>.
    /// </summary>
    /// <param name="error">Почему не вышло.</param>
    /// <returns><c>false</c> — переключения не было: файлы и владелец прежние либо, если журнал остался
    /// (<see cref="HasUnfinished"/>), откат не доведён.</returns>
    public bool TrySwitch(string from, string to, out string error)
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
            // Через временный файл и перенос: оборванная запись остаётся в .new, а журнал появляется только целым.
            string temp = JournalPath + ".new";
            File.WriteAllLines(temp, new[] { from, to });
            File.Move(temp, JournalPath);
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
            // Владелец ещё не тронут, но сверяется так же: откат законченный, только когда сошлось всё.
            error = Undo(from, false, "прежние возвращены на место: " + exception.Message, exception.Message);
            return false;
        }

        if (TryWriteOwner(to, out string ownerError))
        {
            DeleteJournal();
            return true;
        }

        error = Undo(from, true, "не записан новый владелец, прежние файлы и владелец возвращены: " + ownerError,
            "не записан новый владелец: " + ownerError);
        return false;
    }

    /// <summary>
    /// Доводит до конца переключение, прерванное аварией или незаконченным откатом: журнал есть — его не закончили.
    /// </summary>
    public Recovery RecoverInterrupted()
    {
        if (!File.Exists(JournalPath))
            return Recovery.None;

        string[] lines;
        try
        {
            lines = File.ReadAllLines(JournalPath);
        }
        catch (Exception)
        {
            return Recovery.Unreadable;
        }

        string from = lines.Length > 0 ? lines[0].Trim() : null;
        string to = lines.Length > 1 ? lines[1].Trim() : null;
        // Журнал появляется только целым, но и чужая правка не должна стать владельцем: не GUID проекта — не трогаем.
        if (!IsProjectGuid(from) || !IsProjectGuid(to))
            return Recovery.Unreadable;
        // Любая третья строка, даже недописанная, — откат начат: владельцу «куда» больше не верим.
        bool rollingBack = lines.Length > 2;

        // Владелец уже новый: файлы подменили и запомнили, не успели только убрать журнал.
        if (!rollingBack && ReadOwner() == to)
        {
            DeleteJournal();
            return Recovery.Completed;
        }

        // Журнал пишется после готовой копии: по ней файлы возвращаются к виду до переключения, владелец — «откуда».
        if (RollBack().Count > 0)
            return Recovery.Incomplete;
        if (!TryWriteOwner(from, out _))
            return Recovery.Incomplete;

        DeleteJournal();
        return Recovery.RolledBack;
    }

    /// <summary>
    /// Откат после сбоя: файлы из копии, владелец — <paramref name="from"/>. Журнал удаляется, только если сошлось и то,
    /// и другое.
    /// </summary>
    /// <param name="ownerTouched">Владельца уже пытались переписать: без отметки отката в журнале восстановление
    /// поверило бы ему, поэтому без неё откат не начинается.</param>
    /// <returns>Текст ошибки: <paramref name="done"/>, если откат полный, иначе — с тем, что не вернулось.</returns>
    private string Undo(string from, bool ownerTouched, string done, string reason)
    {
        if (!TryMarkRollback(out string markError))
        {
            // Владелец не тронут — восстановление и без отметки откатит (он не «куда»). Тронут — файлы оставляем:
            // от «куда» и они, и, возможно, владелец; восстановление решит по владельцу.
            if (ownerTouched)
                return reason + ". Не записана отметка отката (" + markError + "), файлы не возвращены" +
                       $" — журнал {JournalPath} оставлен";
        }

        List<string> failed = RollBack();
        bool ownerBack = TryWriteOwner(from, out string ownerError);
        if (failed.Count == 0 && ownerBack)
        {
            DeleteJournal();
            return done;
        }

        // Файлы или владелец от разных проектов: журнал остаётся, игра не запустится, пока откат не доделан.
        string notBack = failed.Count > 0 ? ". Не вернулись: " + string.Join(", ", failed) : string.Empty;
        string owner = ownerBack ? string.Empty : ". Владелец не возвращён: " + ownerError;
        return reason + notBack + owner + $" — копия в {BackupRoot}, журнал {JournalPath} оставлен";
    }

    /// <summary>
    /// GUID ассета Unity: 32 шестнадцатеричных знака.
    /// </summary>
    private static bool IsProjectGuid(string value)
    {
        if (value == null || value.Length != 32)
            return false;
        foreach (char c in value)
            if (!Uri.IsHexDigit(c))
                return false;
        return true;
    }

    /// <summary>
    /// Отмечает в журнале, что начат откат. Дозаписью: оборвётся — первые две строки целы.
    /// </summary>
    private bool TryMarkRollback(out string error)
    {
        error = null;
        try
        {
            File.AppendAllText(JournalPath, "rollback" + Environment.NewLine);
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }

    /// <summary>
    /// Пишет владельца и сверяет чтением: запись могла упасть, успев изменить его, или «пройти», не записав.
    /// </summary>
    private bool TryWriteOwner(string owner, out string error)
    {
        error = null;
        try
        {
            writeOwner(owner);
        }
        catch (Exception exception)
        {
            error = exception.Message;
        }

        string written = ReadOwner();
        if (written == owner)
            return true;

        error ??= $"записано «{written}» вместо «{owner}»";
        return false;
    }

    private string ReadOwner()
    {
        try
        {
            return readOwner();
        }
        catch (Exception)
        {
            return null;
        }
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
