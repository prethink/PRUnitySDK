using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Переключение настроек площадок одной операцией: резервная копия, журнал, откат.
/// </summary>
/// <remarks>
/// Только файлы, без AssetDatabase: операцию проверяют тесты (<c>PRSDKPlatformSettingsSwitchTests</c>).
/// <para>
/// Порядок: живые файлы запоминаются у прежнего проекта, копируются в резерв, пишется журнал «откуда → куда», на место
/// встают копии нового. Сбой на любом файле возвращает все из резерва. Удачное переключение оставляет журнал до
/// <see cref="Commit"/>: его зовут, когда переключились и сохранения и владелец стал «куда». Прервался процесс —
/// <see cref="Recover"/> по владельцу решает: стал «куда» — закончено, иначе — откат из резерва.
/// </para>
/// </remarks>
public sealed class PRSDKPlatformSettingsSwitch
{
    private const string JournalFile = "PlatformSettings.journal";
    private const string BackupFolder = "PlatformSettingsBackup";
    private const string AbsentSuffix = ".absent";

    private readonly string root;

    /// <param name="root">Служебная папка: журнал и резервная копия.</param>
    public PRSDKPlatformSettingsSwitch(string root) => this.root = root;

    /// <summary>
    /// Файл настроек в переключении.
    /// </summary>
    public readonly struct Entry
    {
        /// <param name="live">Файл в плагине.</param>
        /// <param name="fromCopy">Копия прежнего проекта; <c>null</c> — проекта нет, запоминать некуда.</param>
        /// <param name="toCopy">Копия нового проекта.</param>
        public Entry(string live, string fromCopy, string toCopy)
        {
            Live = live;
            FromCopy = fromCopy;
            ToCopy = toCopy;
        }

        public string Live { get; }
        public string FromCopy { get; }
        public string ToCopy { get; }
    }

    /// <summary>
    /// Чем кончилось восстановление.
    /// </summary>
    public enum Recovery
    {
        /// <summary>Журнала не было.</summary>
        None,
        /// <summary>Владелец уже «куда»: переключение закончено, журнал убран.</summary>
        Completed,
        /// <summary>Файлы возвращены из резерва.</summary>
        RolledBack,
        /// <summary>Не все файлы вернулись или журнал не убрать: журнал и резерв остаются.</summary>
        Incomplete,
        /// <summary>Журнал не прочитать: ничего не тронуто.</summary>
        Unreadable
    }

    public string JournalPath => Path.Combine(root, JournalFile);

    /// <summary>
    /// Файлы, которых у нового проекта не было: он получил нынешние, от прежнего проекта.
    /// </summary>
    public List<string> Seeded { get; } = new();

    /// <summary>
    /// Прошлое переключение не закончено.
    /// </summary>
    public bool HasUnfinished => File.Exists(JournalPath);

    /// <summary>
    /// Папка, где сейчас лежит плагин: на месте, спрятанная в «Имя~» или нигде.
    /// </summary>
    /// <returns><c>null</c> — плагина нет.</returns>
    public static string LiveFolder(string pluginFolder, string hiddenSuffix)
    {
        if (string.IsNullOrEmpty(pluginFolder))
            return null;
        if (Directory.Exists(pluginFolder))
            return pluginFolder;
        return Directory.Exists(pluginFolder + hiddenSuffix) ? pluginFolder + hiddenSuffix : null;
    }

    /// <summary>
    /// Ставит настройки проекта <paramref name="to"/>.
    /// </summary>
    /// <param name="loaded">Живые файлы, содержимое которых сменилось.</param>
    /// <returns><c>false</c> — файлы в прежнем виде (или, если журнал остался, откат не доведён).</returns>
    public bool TrySwitch(string from, string to, IReadOnlyList<Entry> entries, out List<string> loaded, out string error)
    {
        loaded = new List<string>();
        error = null;
        Seeded.Clear();

        if (HasUnfinished)
        {
            error = $"прошлое переключение настроек не доведено (журнал {JournalPath})";
            return false;
        }

        // Прежний проект получает свежие настройки: дальше они в плагине сменятся.
        foreach (Entry entry in entries.Where(entry => entry.FromCopy != null))
        {
            PRSDKPlatformSettingsFile.Save(entry.Live, entry.FromCopy, out string saveError);
            if (saveError != null)
            {
                error = $"настройки {entry.Live} не запомнены у прежнего проекта, ничего не тронуто: {saveError}";
                return false;
            }
        }

        try
        {
            Backup(entries);
            string temp = JournalPath + ".new";
            File.WriteAllLines(temp, new[] { from ?? string.Empty, to }.Concat(entries.Select(entry => entry.Live)));
            if (File.Exists(JournalPath)) File.Delete(JournalPath);
            File.Move(temp, JournalPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            error = "не записаны резервная копия или журнал настроек, ничего не тронуто: " + exception.Message;
            return false;
        }

        foreach (Entry entry in entries)
        {
            PRSDKPlatformSettingsFile.LoadResult result =
                PRSDKPlatformSettingsFile.Load(entry.Live, entry.ToCopy, preferNewer: false, out string loadError);
            if (result == PRSDKPlatformSettingsFile.LoadResult.Loaded)
            {
                loaded.Add(entry.Live);
                continue;
            }

            if (result == PRSDKPlatformSettingsFile.LoadResult.Seeded)
                Seeded.Add(entry.Live);

            if (result != PRSDKPlatformSettingsFile.LoadResult.Failed)
                continue;

            List<string> failed = RollBack(out List<string> restored);
            loaded = restored;
            if (failed.Count == 0 && TryDeleteJournal())
            {
                error = $"не поставлены настройки {entry.Live} ({loadError}), прежние возвращены";
                return false;
            }

            error = $"не поставлены настройки {entry.Live} ({loadError}), и откат не доведён" +
                    (failed.Count > 0 ? ": " + string.Join(", ", failed) : string.Empty) + $" — журнал {JournalPath} оставлен";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Переключение закончено целиком (и сохранения, и владелец): журнал больше не нужен.
    /// </summary>
    public bool Commit() => TryDeleteJournal();

    /// <summary>
    /// Возвращает настройки из резерва: дальше переключение не пошло (не переключились сохранения).
    /// </summary>
    /// <param name="restored">Живые файлы, которые вернулись.</param>
    /// <returns><c>false</c> — вернулось не всё, журнал оставлен.</returns>
    public bool Revert(out List<string> restored, out string error)
    {
        error = null;
        restored = new List<string>();
        if (!HasUnfinished)
            return true;

        List<string> failed = RollBack(out restored);
        if (failed.Count == 0 && TryDeleteJournal())
            return true;

        error = failed.Count > 0 ? "не вернулись: " + string.Join(", ", failed) : $"журнал {JournalPath} не удалён";
        return false;
    }

    /// <summary>
    /// Доводит прерванное переключение по владельцу: он «куда» — закончено, иначе — откат.
    /// </summary>
    public Recovery Recover(string owner, out List<string> restored)
    {
        restored = new List<string>();
        if (!HasUnfinished)
            return Recovery.None;

        string[] lines;
        try
        {
            lines = File.ReadAllLines(JournalPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Recovery.Unreadable;
        }

        if (lines.Length < 2 || string.IsNullOrWhiteSpace(lines[1]))
            return Recovery.Unreadable;

        if (owner == lines[1].Trim())
            return TryDeleteJournal() ? Recovery.Completed : Recovery.Incomplete;

        List<string> failed = RollBack(out restored);
        return failed.Count == 0 && TryDeleteJournal() ? Recovery.RolledBack : Recovery.Incomplete;
    }

    private string BackupPath(int index) => Path.Combine(root, BackupFolder, index.ToString(System.Globalization.CultureInfo.InvariantCulture));

    private void Backup(IReadOnlyList<Entry> entries)
    {
        string folder = Path.Combine(root, BackupFolder);
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
        Directory.CreateDirectory(folder);

        for (int i = 0; i < entries.Count; i++)
        {
            if (File.Exists(entries[i].Live))
                File.Copy(entries[i].Live, BackupPath(i));
            else
                File.WriteAllText(BackupPath(i) + AbsentSuffix, string.Empty);
        }
    }

    /// <returns>Файлы, которые не вернулись.</returns>
    private List<string> RollBack(out List<string> restored)
    {
        var failed = new List<string>();
        restored = new List<string>();

        string[] lines;
        try
        {
            lines = File.ReadAllLines(JournalPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            failed.Add($"{JournalPath} ({exception.Message})");
            return failed;
        }

        for (int i = 0; i + 2 < lines.Length; i++)
        {
            string live = lines[i + 2];
            string copy = BackupPath(i);
            try
            {
                // Файла не было до переключения — переключение его и не создавало: трогать нечего.
                if (!File.Exists(copy))
                {
                    if (!File.Exists(copy + AbsentSuffix))
                        failed.Add(live + " (нет в резервной копии)");
                    continue;
                }

                if (File.Exists(live) && File.ReadAllBytes(live).AsSpan().SequenceEqual(File.ReadAllBytes(copy)))
                    continue;

                File.Copy(copy, live, overwrite: true);
                restored.Add(live);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                failed.Add($"{live} ({exception.Message})");
            }
        }

        return failed;
    }

    private bool TryDeleteJournal()
    {
        try
        {
            if (File.Exists(JournalPath))
                File.Delete(JournalPath);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
