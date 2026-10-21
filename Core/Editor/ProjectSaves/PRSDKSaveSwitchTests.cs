#if PRSDK_TESTS
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

/// <summary>
/// Переключение сохранений площадок: подмена файлов, сбой копии и подмены, восстановление после аварии, <c>.meta</c>.
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
        public IEnumerable<string> Files { get { yield return File; } }
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
        saves = new PRSDKSaveSwitch(root, new IPRSDKEditorSaveStore[] { first, second });
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
        bool committed = false;

        Assert.IsTrue(saves.TrySwitch(A, B, () => committed = true, out string error), error);

        Assert.IsTrue(committed);
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

        Assert.IsTrue(saves.TrySwitch(A, B, null, out string error), error);

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
        bool committed = false;

        // Второй файл не прочитать: копия не соберётся целиком.
        using (new FileStream(second.File, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Assert.IsFalse(saves.TrySwitch(A, B, () => committed = true, out _));

        Assert.IsFalse(committed);
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
        bool committed = false;

        // Второй файл читается (копия собирается), но не перезаписывается: первая площадка уже подменена.
        using (new FileStream(second.File, FileMode.Open, FileAccess.Read, FileShare.Read))
            Assert.IsFalse(saves.TrySwitch(A, B, () => committed = true, out _));

        Assert.IsFalse(committed);
        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.AreEqual("A2", File.ReadAllText(second.File));
        Assert.IsFalse(File.Exists(saves.JournalPath));
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
            Assert.IsFalse(saves.TrySwitch(A, B, null, out _));

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
        Assert.IsTrue(saves.TrySwitch(A, B, null, out _));
        File.WriteAllLines(saves.JournalPath, new[] { A, B });

        string owner = saves.RecoverInterrupted(A, out bool rolledBack);

        Assert.IsTrue(rolledBack);
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
        Assert.IsTrue(saves.TrySwitch(A, B, null, out _));
        // Авария после записи владельца, до удаления журнала.
        File.WriteAllLines(saves.JournalPath, new[] { A, B });

        string owner = saves.RecoverInterrupted(B, out bool rolledBack);

        Assert.IsFalse(rolledBack);
        Assert.AreEqual(B, owner);
        Assert.AreEqual("B1", File.ReadAllText(first.File));
        Assert.IsFalse(File.Exists(saves.JournalPath));
    }

    [Test]
    public void Recover_WithoutJournal_DoesNothing()
    {
        File.WriteAllText(first.File, "A1");

        string owner = saves.RecoverInterrupted(A, out bool rolledBack);

        Assert.IsFalse(rolledBack);
        Assert.AreEqual(A, owner);
        Assert.AreEqual("A1", File.ReadAllText(first.File));
    }

    [Test]
    public void SecondSwitch_ReplacesBackupWhole()
    {
        File.WriteAllText(first.File, "A1");
        WriteProject(B, first, "B1");
        Assert.IsTrue(saves.TrySwitch(A, B, null, out _));
        Assert.IsTrue(saves.TrySwitch(B, A, null, out _));

        Assert.AreEqual("A1", File.ReadAllText(first.File));
        Assert.IsFalse(Directory.Exists(Path.Combine(root, "Backup.new")));
        Assert.IsFalse(Directory.Exists(Path.Combine(root, "Backup.old")));
        Assert.AreEqual("B1", File.ReadAllText(Path.Combine(root, "Backup", first.Platform, "first.json")));
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
