#if PRSDK_TESTS
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

/// <summary>
/// Переключение сохранений площадок: подмена файлов, сбой копии, подмены и записи владельца, восстановление после аварии, <c>.meta</c>.
/// </summary>
/// <remarks>
/// Сбой — настоящая блокировка файла: открытый без общего доступа файл Windows не даёт ни прочитать, ни перезаписать.
/// </remarks>
public sealed class PRSDKSaveSwitchTests
{
    private const string A = "projectA";
    private const string B = "projectB";

    private string folder;
    private string root;
    private Store first;
    private Store second;
    private PRSDKSaveSwitch saves;
    private string owner;
    private System.Func<string> readOwner;
    private System.Action<string> writeOwner;

    /// <summary>
    /// Площадка теста. Конструктор с параметрами: <see cref="PRSDKProjectSaves.Stores"/> берёт только площадки
    /// без параметров, и эта в настоящее переключение не попадает.
    /// </summary>
    private sealed class Store : IPRSDKEditorSaveStore
    {
        public Store(string platform, string file)
        {
            Platform = platform;
            File = file;
        }

        public string Platform { get; }
        public string File { get; }

        /// <summary>
        /// Сколько раз перечисляли файлы: копия, в папку проекта, на место, откат — по разу.
        /// </summary>
        public int Passes { get; private set; }

        /// <summary>
        /// Зовётся на каждом перечислении с его номером: так тест вмешивается в нужную стадию.
        /// </summary>
        public System.Action<int> OnPass { get; set; }

        public IEnumerable<string> Files
        {
            get
            {
                OnPass?.Invoke(++Passes);
                yield return File;
            }
        }
    }

    [SetUp]
    public void SetUp()
    {
        folder = Path.Combine(Path.GetTempPath(), "PRSDKSaveSwitchTests_" + Path.GetRandomFileName());
        root = Path.Combine(folder, "Saves");
        string live = Path.Combine(folder, "Live");
        Directory.CreateDirectory(live);
        first = new Store("First", Path.Combine(live, "first.json"));
        second = new Store("Second", Path.Combine(live, "second.json"));
        owner = A;
        readOwner = () => owner;
        writeOwner = value => owner = value;
        saves = new PRSDKSaveSwitch(root, new IPRSDKEditorSaveStore[] { first, second }, () => readOwner(), value => writeOwner(value));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
    }

    [Test]
    public void Switch_MovesLiveFilesToOldProject_AndPutsNewProjectFiles()
    {
        File.WriteAllText(first.File, "A1");
        File.WriteAllText(second.File, "A2");
        WriteProject(B, first, "B1");
        WriteProject(B, second, "B2");

        Assert.IsTrue(saves.TrySwitch(A, B, out string error), error);

        Assert.AreEqual(B, owner);
        Assert.AreEqual("B1", File.ReadAllText(first.File));
        Assert.AreEqual("B2", File.ReadAllText(second.File));
        Assert.AreEqual("A1", ReadProject(A, first));
        Assert.AreEqual("A2", ReadProject(A, second));
        Assert.IsFalse(File.Exists(saves.JournalPath));
    }

    [Test]
    public void Switch_ToProjectWithoutSave_DeletesLiveFileAndMeta()
    {
        File.WriteAllText(first.File, "A1");
        File.WriteAllText(first.File + ".meta", "guid");

        Assert.IsTrue(saves.TrySwitch(A, B, out string error), error);

        Assert.IsFalse(File.Exists(first.File));
        Assert.IsFalse(File.Exists(first.File + ".meta"));
        Assert.AreEqual("A1", ReadProject(A, first));
    }

    [Test]
    public void BackupFailure_LeavesLiveFilesUntouched()
    {
        File.WriteAllText(first.File, "A1");
        File.WriteAllText(second.File, "A2");
        WriteProject(B, first, "B1");

        // Второй файл не прочитать: копия не соберётся целиком.
        using (new FileStream(second.File, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Assert.IsFalse(saves.TrySwitch(A, B, out _));

        Assert.AreEqual(A, owner);
        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.AreEqual("A2", File.ReadAllText(second.File));
        Assert.IsFalse(File.Exists(saves.JournalPath));
    }

    [Test]
    public void SwapFailure_RollsBackEveryStore()
    {
        File.WriteAllText(first.File, "A1");
        File.WriteAllText(second.File, "A2");
        WriteProject(B, first, "B1");
        WriteProject(B, second, "B2");

        // Второй файл читается (копия собирается), но не перезаписывается: первая площадка уже подменена.
        using (new FileStream(second.File, FileMode.Open, FileAccess.Read, FileShare.Read))
            Assert.IsFalse(saves.TrySwitch(A, B, out _));

        Assert.AreEqual(A, owner);
        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.AreEqual("A2", File.ReadAllText(second.File));
        Assert.IsFalse(File.Exists(saves.JournalPath));
    }

    [Test]
    public void OwnerWriteFailure_RollsBackFiles()
    {
        File.WriteAllText(first.File, "A1");
        File.WriteAllText(second.File, "A2");
        WriteProject(B, first, "B1");
        WriteProject(B, second, "B2");
        // Новый владелец не пишется вовсе, прежний — пишется.
        writeOwner = value => { if (value == B) throw new IOException("владелец занят"); owner = value; };

        Assert.IsFalse(saves.TrySwitch(A, B, out string error));

        StringAssert.Contains("владелец занят", error);
        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.AreEqual("A2", File.ReadAllText(second.File));
        Assert.AreEqual(A, owner);
        Assert.IsFalse(saves.HasUnfinished);
    }

    [Test]
    public void OwnerHalfWritten_RestoresFilesAndOwner()
    {
        File.WriteAllText(first.File, "A1");
        WriteProject(B, first, "B1");
        // Запись успела испортить владельца и упала.
        writeOwner = value => { if (value == B) { owner = "proj"; throw new IOException("оборвалось"); } owner = value; };

        Assert.IsFalse(saves.TrySwitch(A, B, out _));

        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.AreEqual(A, owner);
        Assert.IsFalse(saves.HasUnfinished);
    }

    [Test]
    public void OwnerWrittenThenFailure_CountsAsSwitched()
    {
        File.WriteAllText(first.File, "A1");
        WriteProject(B, first, "B1");
        // Владелец записан целиком, упало уже после: файлы и запись сходятся с B — переключение состоялось.
        writeOwner = value => { owner = value; throw new IOException("после записи"); };

        Assert.IsTrue(saves.TrySwitch(A, B, out _));

        Assert.AreEqual("B1", File.ReadAllText(first.File));
        Assert.AreEqual(B, owner);
        Assert.IsFalse(saves.HasUnfinished);
    }

    [Test]
    public void OwnerNotRestored_KeepsJournal_UntilRecover()
    {
        File.WriteAllText(first.File, "A1");
        WriteProject(B, first, "B1");
        // Запись владельца сломана целиком: испорчен и не возвращается.
        writeOwner = value => { owner = "proj"; throw new IOException("диск"); };

        Assert.IsFalse(saves.TrySwitch(A, B, out string error));

        StringAssert.Contains("Владелец не возвращён", error);
        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.IsTrue(saves.HasUnfinished);

        // Пока запись не починилась, восстановление тоже не заканчивается.
        Assert.AreEqual(PRSDKSaveSwitch.Recovery.Incomplete, saves.RecoverInterrupted());
        Assert.IsTrue(saves.HasUnfinished);

        writeOwner = value => owner = value;
        Assert.AreEqual(PRSDKSaveSwitch.Recovery.RolledBack, saves.RecoverInterrupted());
        Assert.AreEqual(A, owner);
        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.IsFalse(saves.HasUnfinished);
    }

    [Test]
    public void RollbackStarted_RecoverDoesNotTrustNewOwner()
    {
        File.WriteAllText(first.File, "A1");
        WriteProject(B, first, "B1");
        // Владелец записан B, но проверочное чтение один раз падает; вернуть A не выходит.
        bool readFails = false;
        readOwner = () => { if (readFails) { readFails = false; throw new IOException("занят"); } return owner; };
        writeOwner = value => { if (value == A) throw new IOException("диск"); owner = value; readFails = true; };

        Assert.IsFalse(saves.TrySwitch(A, B, out _));

        // Файлы откатились, владелец остался B — журнал с отметкой отката.
        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.AreEqual(B, owner);
        Assert.IsTrue(saves.HasUnfinished);

        // Владелец «куда» больше не довод: восстановление доводит откат, а не признаёт переключение.
        writeOwner = value => owner = value;
        Assert.AreEqual(PRSDKSaveSwitch.Recovery.RolledBack, saves.RecoverInterrupted());
        Assert.AreEqual(A, owner);
        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.IsFalse(saves.HasUnfinished);
    }

    [Test]
    public void CrashDuringRollback_RecoverFinishesRollback()
    {
        File.WriteAllText(first.File, "A1");
        WriteProject(B, first, "B1");
        Assert.IsTrue(saves.TrySwitch(A, B, out _));
        // Авария посреди отката: файлы и владелец ещё от B, в журнале отметка.
        File.WriteAllLines(saves.JournalPath, new[] { A, B, "rollback" });

        Assert.AreEqual(PRSDKSaveSwitch.Recovery.RolledBack, saves.RecoverInterrupted());

        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.AreEqual(A, owner);
        Assert.IsFalse(saves.HasUnfinished);
    }

    [Test]
    public void RollbackMarkFailure_LeavesFilesForRecovery()
    {
        File.WriteAllText(first.File, "A1");
        WriteProject(B, first, "B1");
        FileStream journalLock = null;
        // Запись владельца портит его и падает, а журнал в этот момент не дописать — отметку отката не поставить.
        writeOwner = value =>
        {
            owner = "proj";
            journalLock = new FileStream(saves.JournalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            throw new IOException("оборвалось");
        };

        try
        {
            Assert.IsFalse(saves.TrySwitch(A, B, out string error));
            StringAssert.Contains("отметка отката", error);
        }
        finally { journalLock?.Dispose(); }

        // Без отметки откат не начат: файлы от B, журнал без отметки.
        Assert.AreEqual("B1", File.ReadAllText(first.File));
        Assert.IsTrue(saves.HasUnfinished);

        // Владелец не «куда» — восстановление откатывает.
        writeOwner = value => owner = value;
        Assert.AreEqual(PRSDKSaveSwitch.Recovery.RolledBack, saves.RecoverInterrupted());
        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.AreEqual(A, owner);
    }

    [Test]
    public void DamagedJournal_RecoverTouchesNothing()
    {
        File.WriteAllText(first.File, "A1");
        WriteProject(B, first, "B1");
        Assert.IsTrue(saves.TrySwitch(A, B, out _));
        // Журнал без «куда»: из него нельзя понять ни владельца, ни направления.
        File.WriteAllLines(saves.JournalPath, new[] { "proj" });

        Assert.AreEqual(PRSDKSaveSwitch.Recovery.Unreadable, saves.RecoverInterrupted());

        Assert.AreEqual("B1", File.ReadAllText(first.File));
        Assert.AreEqual(B, owner);
        Assert.IsTrue(saves.HasUnfinished);
    }

    [Test]
    public void LockedJournal_RecoverTouchesNothing()
    {
        File.WriteAllText(first.File, "A1");
        WriteProject(B, first, "B1");
        Assert.IsTrue(saves.TrySwitch(A, B, out _));
        File.WriteAllLines(saves.JournalPath, new[] { A, B });
        owner = A;

        using (new FileStream(saves.JournalPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Assert.AreEqual(PRSDKSaveSwitch.Recovery.Unreadable, saves.RecoverInterrupted());

        Assert.AreEqual("B1", File.ReadAllText(first.File));
        Assert.AreEqual(A, owner);
        Assert.IsTrue(saves.HasUnfinished);
    }

    [Test]
    public void JournalWriteFailure_LeavesNoJournal()
    {
        File.WriteAllText(first.File, "A1");
        WriteProject(B, first, "B1");
        Directory.CreateDirectory(root);
        // Временный файл журнала занят: журнал не появляется вовсе, а не появляется наполовину.
        using (new FileStream(saves.JournalPath + ".new", FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            Assert.IsFalse(saves.TrySwitch(A, B, out _));

        Assert.IsFalse(saves.HasUnfinished);
        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.AreEqual(A, owner);
    }

    [Test]
    public void SwapFailure_RestoresDeletedMeta()
    {
        File.WriteAllText(first.File, "A1");
        File.WriteAllText(first.File + ".meta", "guid");
        File.WriteAllText(second.File, "A2");
        WriteProject(B, second, "B2");

        // У B нет первого сохранения — первый файл с .meta удаляется; второй не перезаписать — откат.
        using (new FileStream(second.File, FileMode.Open, FileAccess.Read, FileShare.Read))
            Assert.IsFalse(saves.TrySwitch(A, B, out _));

        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.AreEqual("guid", File.ReadAllText(first.File + ".meta"));
    }

    [Test]
    public void Recover_WithJournalAndOldOwner_RestoresFilesFromBackup()
    {
        File.WriteAllText(first.File, "A1");
        File.WriteAllText(second.File, "A2");
        WriteProject(B, first, "B1");
        WriteProject(B, second, "B2");
        // Авария до записи владельца: файлы подменены, журнал остался, владелец прежний.
        Assert.IsTrue(saves.TrySwitch(A, B, out _));
        File.WriteAllLines(saves.JournalPath, new[] { A, B });

        owner = A;
        PRSDKSaveSwitch.Recovery result = saves.RecoverInterrupted();

        Assert.AreEqual(PRSDKSaveSwitch.Recovery.RolledBack, result);
        Assert.AreEqual(A, owner);
        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.AreEqual("A2", File.ReadAllText(second.File));
        Assert.IsFalse(File.Exists(saves.JournalPath));
    }

    [Test]
    public void Recover_WhenOwnerAlreadyNew_KeepsFiles()
    {
        File.WriteAllText(first.File, "A1");
        WriteProject(B, first, "B1");
        Assert.IsTrue(saves.TrySwitch(A, B, out _));
        // Авария после записи владельца, до удаления журнала.
        File.WriteAllLines(saves.JournalPath, new[] { A, B });

        owner = B;
        PRSDKSaveSwitch.Recovery result = saves.RecoverInterrupted();

        Assert.AreEqual(PRSDKSaveSwitch.Recovery.Completed, result);
        Assert.AreEqual(B, owner);
        Assert.AreEqual("B1", File.ReadAllText(first.File));
        Assert.IsFalse(File.Exists(saves.JournalPath));
    }

    [Test]
    public void Recover_WithoutJournal_DoesNothing()
    {
        File.WriteAllText(first.File, "A1");

        PRSDKSaveSwitch.Recovery result = saves.RecoverInterrupted();

        Assert.AreEqual(PRSDKSaveSwitch.Recovery.None, result);
        Assert.AreEqual(A, owner);
        Assert.AreEqual("A1", File.ReadAllText(first.File));
    }

    [Test]
    public void SecondSwitch_ReplacesBackupWhole()
    {
        File.WriteAllText(first.File, "A1");
        WriteProject(B, first, "B1");
        Assert.IsTrue(saves.TrySwitch(A, B, out _));
        Assert.IsTrue(saves.TrySwitch(B, A, out _));

        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.IsFalse(Directory.Exists(Path.Combine(root, "Backup.new")));
        Assert.IsFalse(Directory.Exists(Path.Combine(root, "Backup.old")));
        Assert.AreEqual("B1", File.ReadAllText(Path.Combine(root, "Backup", first.Platform, "first.json")));
    }

    [Test]
    public void IncompleteRollBack_KeepsJournal()
    {
        File.WriteAllText(first.File, "A1");
        File.WriteAllText(second.File, "A2");
        WriteProject(B, first, "B1");
        WriteProject(B, second, "B2");
        FileStream firstLock = null;
        // Откат — четвёртое перечисление: в этот момент первый файл держит кто-то другой.
        first.OnPass = pass => { if (pass == 4) firstLock = new FileStream(first.File, FileMode.Open, FileAccess.Read, FileShare.None); };

        try
        {
            using (new FileStream(second.File, FileMode.Open, FileAccess.Read, FileShare.Read))
                Assert.IsFalse(saves.TrySwitch(A, B, out _));
        }
        finally { firstLock?.Dispose(); }

        Assert.IsTrue(saves.HasUnfinished);
        Assert.AreEqual(A, owner);
        // Отпустили файл — восстановление при следующем запуске доводит откат.
        PRSDKSaveSwitch.Recovery result = saves.RecoverInterrupted();
        Assert.AreEqual(PRSDKSaveSwitch.Recovery.RolledBack, result);
        Assert.AreEqual(A, owner);
        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.IsFalse(saves.HasUnfinished);
    }

    [Test]
    public void Recover_WhenFileStillLocked_KeepsJournal()
    {
        File.WriteAllText(first.File, "A1");
        WriteProject(B, first, "B1");
        Assert.IsTrue(saves.TrySwitch(A, B, out _));
        File.WriteAllLines(saves.JournalPath, new[] { A, B });

        using (new FileStream(first.File, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            owner = A;
            PRSDKSaveSwitch.Recovery result = saves.RecoverInterrupted();
            Assert.AreEqual(PRSDKSaveSwitch.Recovery.Incomplete, result);
            Assert.AreEqual(A, owner);
        }

        Assert.IsTrue(saves.HasUnfinished);
    }

    [Test]
    public void Meta_TravelsWithProjectSave()
    {
        File.WriteAllText(first.File, "A1");
        File.WriteAllText(first.File + ".meta", "guidA");

        // У B сохранения нет: файл и .meta уходят, а .meta остаётся у сохранения A.
        Assert.IsTrue(saves.TrySwitch(A, B, out _));
        Assert.IsFalse(File.Exists(first.File + ".meta"));
        Assert.IsTrue(saves.TrySwitch(B, A, out _));

        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.AreEqual("guidA", File.ReadAllText(first.File + ".meta"));
    }

    [Test]
    public void Switch_ToSaveWithoutMeta_RemovesForeignMeta()
    {
        File.WriteAllText(first.File, "A1");
        File.WriteAllText(first.File + ".meta", "guidA");
        WriteProject(B, first, "B1");

        Assert.IsTrue(saves.TrySwitch(A, B, out _));

        Assert.AreEqual("B1", File.ReadAllText(first.File));
        Assert.IsFalse(File.Exists(first.File + ".meta"));
        Assert.AreEqual("guidA", File.ReadAllText(Path.Combine(saves.GetFolder(A, first.Platform), "first.json.meta")));
    }

    [Test]
    public void RollBack_ToSaveWithoutMeta_RemovesForeignMeta()
    {
        File.WriteAllText(first.File, "A1");
        File.WriteAllText(second.File, "A2");
        WriteProject(B, first, "B1");
        File.WriteAllText(Path.Combine(saves.GetFolder(B, first.Platform), "first.json.meta"), "guidB");
        WriteProject(B, second, "B2");

        // Первый файл получает .meta от B, второй не перезаписать — откат к A, у которого .meta не было.
        using (new FileStream(second.File, FileMode.Open, FileAccess.Read, FileShare.Read))
            Assert.IsFalse(saves.TrySwitch(A, B, out _));

        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.IsFalse(File.Exists(first.File + ".meta"));
        Assert.IsFalse(saves.HasUnfinished);
    }

    private void WriteProject(string project, Store store, string text)
    {
        string path = saves.GetFolder(project, store.Platform);
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, Path.GetFileName(store.File)), text);
    }

    private string ReadProject(string project, Store store) =>
        File.ReadAllText(Path.Combine(saves.GetFolder(project, store.Platform), Path.GetFileName(store.File)));
}
#endif
