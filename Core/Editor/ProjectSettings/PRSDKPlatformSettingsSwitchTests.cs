#if PRSDK_TESTS
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

/// <summary>
/// Переключение настроек площадок одной операцией: A → B → A, сбой с откатом, прерывание и восстановление по владельцу,
/// спрятанный плагин, удалённый прежний проект.
/// </summary>
/// <remarks>
/// Сбой — настоящая блокировка файла: открытый без общего доступа на запись файл Windows не даёт перезаписать.
/// </remarks>
public sealed class PRSDKPlatformSettingsSwitchTests
{
    private const string A = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string B = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    private string folder;
    private string yandex;
    private string gamePush;
    private PRSDKPlatformSettingsSwitch settings;

    [SetUp]
    public void SetUp()
    {
        folder = Path.Combine(Path.GetTempPath(), "PRSDKPlatformSettingsSwitchTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(folder, "Plugin"));
        yandex = Path.Combine(folder, "Plugin", "SettingsYG2.asset");
        gamePush = Path.Combine(folder, "Plugin", "EditorSaves.json");
        settings = new PRSDKPlatformSettingsSwitch(Path.Combine(folder, "Service"));
        Directory.CreateDirectory(Path.Combine(folder, "Service"));
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
    }

    [Test]
    public void RoundTrip_AToBToA_EachGameKeepsItsSettings()
    {
        File.WriteAllText(yandex, "A yandex");
        File.WriteAllText(gamePush, "A gp");
        Copy(B, yandex, "B yandex");
        Copy(B, gamePush, "B gp");

        Assert.IsTrue(settings.TrySwitch(A, B, Entries(A, B), out List<string> loaded, out string error), error);
        Assert.IsTrue(settings.Commit());
        Assert.AreEqual(2, loaded.Count);
        Assert.AreEqual("B yandex", File.ReadAllText(yandex));
        Assert.AreEqual("B gp", File.ReadAllText(gamePush));

        // Правка у B между переключениями.
        File.WriteAllText(yandex, "B yandex edited");

        Assert.IsTrue(settings.TrySwitch(B, A, Entries(B, A), out _, out error), error);
        Assert.IsTrue(settings.Commit());
        Assert.AreEqual("A yandex", File.ReadAllText(yandex));
        Assert.AreEqual("A gp", File.ReadAllText(gamePush));
        Assert.AreEqual("B yandex edited", File.ReadAllText(CopyPath(B, yandex)));
        Assert.IsFalse(settings.HasUnfinished);
    }

    [Test]
    public void SecondFileLocked_FirstIsRestored_AndNothingSwitched()
    {
        File.WriteAllText(yandex, "A yandex");
        File.WriteAllText(gamePush, "A gp");
        Copy(B, yandex, "B yandex");
        Copy(B, gamePush, "B gp");

        string error;
        using (new FileStream(gamePush, FileMode.Open, FileAccess.Read, FileShare.Read))
            Assert.IsFalse(settings.TrySwitch(A, B, Entries(A, B), out _, out error));

        StringAssert.Contains("прежние возвращены", error);
        Assert.AreEqual("A yandex", File.ReadAllText(yandex));
        Assert.AreEqual("A gp", File.ReadAllText(gamePush));
        Assert.IsFalse(settings.HasUnfinished);
    }

    [Test]
    public void Interrupted_OwnerStillFrom_RecoverRollsBack()
    {
        File.WriteAllText(yandex, "A yandex");
        Copy(B, yandex, "B yandex");

        // Настройки встали, а до сохранений и владельца дело не дошло: процесс оборвался.
        Assert.IsTrue(settings.TrySwitch(A, B, Entries(A, B, gamePushToo: false), out _, out _));
        Assert.IsTrue(settings.HasUnfinished);

        var afterCrash = new PRSDKPlatformSettingsSwitch(Path.Combine(folder, "Service"));
        Assert.AreEqual(PRSDKPlatformSettingsSwitch.Recovery.RolledBack, afterCrash.Recover(A, out List<string> restored));

        Assert.AreEqual("A yandex", File.ReadAllText(yandex));
        CollectionAssert.AreEqual(new[] { yandex }, restored);
        Assert.IsFalse(afterCrash.HasUnfinished);
    }

    [Test]
    public void Interrupted_OwnerAlreadyTo_RecoverCompletes()
    {
        File.WriteAllText(yandex, "A yandex");
        Copy(B, yandex, "B yandex");

        Assert.IsTrue(settings.TrySwitch(A, B, Entries(A, B, gamePushToo: false), out _, out _));

        // Сохранения и владелец успели переключиться, не успели убрать журнал настроек.
        Assert.AreEqual(PRSDKPlatformSettingsSwitch.Recovery.Completed, settings.Recover(B, out _));
        Assert.AreEqual("B yandex", File.ReadAllText(yandex));
        Assert.IsFalse(settings.HasUnfinished);
    }

    [Test]
    public void SavesFailed_Revert_ReturnsPreviousSettings()
    {
        File.WriteAllText(yandex, "A yandex");
        Copy(B, yandex, "B yandex");

        Assert.IsTrue(settings.TrySwitch(A, B, Entries(A, B, gamePushToo: false), out _, out _));
        Assert.IsTrue(settings.Revert(out List<string> restored, out string error), error);

        Assert.AreEqual("A yandex", File.ReadAllText(yandex));
        CollectionAssert.AreEqual(new[] { yandex }, restored);
        Assert.IsFalse(settings.HasUnfinished);
    }

    [Test]
    public void Unfinished_BlocksNextSwitch()
    {
        File.WriteAllText(yandex, "A yandex");
        Copy(B, yandex, "B yandex");
        Assert.IsTrue(settings.TrySwitch(A, B, Entries(A, B, gamePushToo: false), out _, out _));

        Assert.IsFalse(settings.TrySwitch(B, A, Entries(B, A, gamePushToo: false), out _, out string error));

        StringAssert.Contains("не доведено", error);
        Assert.AreEqual("B yandex", File.ReadAllText(yandex));
    }

    [Test]
    public void NewProjectWithoutCopy_GetsCurrent_AsStart()
    {
        File.WriteAllText(yandex, "A yandex");

        Assert.IsTrue(settings.TrySwitch(A, B, Entries(A, B, gamePushToo: false), out List<string> loaded, out _));

        Assert.AreEqual("A yandex", File.ReadAllText(yandex));
        Assert.AreEqual("A yandex", File.ReadAllText(CopyPath(B, yandex)));
        Assert.IsEmpty(loaded);
        CollectionAssert.AreEqual(new[] { yandex }, settings.Seeded);
    }

    [Test]
    public void FromProjectDeleted_StillSwitches()
    {
        File.WriteAllText(yandex, "orphan");
        Copy(B, yandex, "B yandex");
        var entries = new[] { new PRSDKPlatformSettingsSwitch.Entry(yandex, null, CopyPath(B, yandex)) };

        Assert.IsTrue(settings.TrySwitch(A, B, entries, out _, out string error), error);

        Assert.AreEqual("B yandex", File.ReadAllText(yandex));
    }

    [Test]
    public void LiveFolder_FindsHiddenPlugin()
    {
        string plugin = Path.Combine(folder, "PluginYourGames");
        Assert.IsNull(PRSDKPlatformSettingsSwitch.LiveFolder(plugin, "~"));

        Directory.CreateDirectory(plugin + "~");
        Assert.AreEqual(plugin + "~", PRSDKPlatformSettingsSwitch.LiveFolder(plugin, "~"));

        Directory.CreateDirectory(plugin);
        Assert.AreEqual(plugin, PRSDKPlatformSettingsSwitch.LiveFolder(plugin, "~"));
    }

    private List<PRSDKPlatformSettingsSwitch.Entry> Entries(string from, string to, bool gamePushToo = true)
    {
        var list = new List<PRSDKPlatformSettingsSwitch.Entry>
        {
            new(yandex, CopyPath(from, yandex), CopyPath(to, yandex)),
        };
        if (gamePushToo)
            list.Add(new PRSDKPlatformSettingsSwitch.Entry(gamePush, CopyPath(from, gamePush), CopyPath(to, gamePush)));
        return list;
    }

    private string CopyPath(string project, string live) =>
        Path.Combine(folder, "Projects", project, "PlatformSettings~", Path.GetFileName(live));

    private void Copy(string project, string live, string text)
    {
        string path = CopyPath(project, live);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }
}
#endif
