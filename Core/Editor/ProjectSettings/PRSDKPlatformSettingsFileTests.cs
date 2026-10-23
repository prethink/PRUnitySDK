#if PRSDK_TESTS
using System;
using System.IO;
using NUnit.Framework;

/// <summary>
/// Копии настроек площадок по проектам: загрузка, стартовая копия, правка новее копии, сбой записи.
/// </summary>
public sealed class PRSDKPlatformSettingsFileTests
{
    private string folder;
    private string live;
    private string stored;

    [SetUp]
    public void SetUp()
    {
        folder = Path.Combine(Path.GetTempPath(), "PRSDKPlatformSettingsFileTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(folder, "Plugin"));
        live = Path.Combine(folder, "Plugin", "Settings.asset");
        stored = Path.Combine(folder, "Project", "PlatformSettings~", "YG2", "Settings.asset");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
    }

    [Test]
    public void Load_PutsProjectCopy_IntoPlugin()
    {
        File.WriteAllText(live, "game A");
        Write(stored, "game B", DateTime.UtcNow.AddMinutes(-5));

        Assert.AreEqual(PRSDKPlatformSettingsFile.LoadResult.Loaded, PRSDKPlatformSettingsFile.Load(live, stored, false, out _));

        Assert.AreEqual("game B", File.ReadAllText(live));
    }

    [Test]
    public void Load_WithoutCopy_SeedsFromCurrent_AndKeepsIt()
    {
        File.WriteAllText(live, "game A");

        Assert.AreEqual(PRSDKPlatformSettingsFile.LoadResult.Seeded, PRSDKPlatformSettingsFile.Load(live, stored, false, out _));

        Assert.AreEqual("game A", File.ReadAllText(live));
        Assert.AreEqual("game A", File.ReadAllText(stored));
    }

    [Test]
    public void Load_PreferNewer_KeepsFreshEdit_AndUpdatesCopy()
    {
        Write(stored, "old", DateTime.UtcNow.AddMinutes(-5));
        File.WriteAllText(live, "edited");

        Assert.AreEqual(PRSDKPlatformSettingsFile.LoadResult.KeptNewer, PRSDKPlatformSettingsFile.Load(live, stored, true, out _));

        Assert.AreEqual("edited", File.ReadAllText(live));
        Assert.AreEqual("edited", File.ReadAllText(stored));
    }

    [Test]
    public void Load_PreferNewer_TakesCopyFromGit_WhenItIsNewer()
    {
        Write(live, "local", DateTime.UtcNow.AddMinutes(-5));
        File.WriteAllText(stored = PrepareStored(), "pulled");

        Assert.AreEqual(PRSDKPlatformSettingsFile.LoadResult.Loaded, PRSDKPlatformSettingsFile.Load(live, stored, true, out _));

        Assert.AreEqual("pulled", File.ReadAllText(live));
    }

    [Test]
    public void Load_Same_TouchesNothing()
    {
        File.WriteAllText(live, "same");
        Write(stored, "same", DateTime.UtcNow.AddMinutes(-5));
        DateTime before = File.GetLastWriteTimeUtc(stored);

        Assert.AreEqual(PRSDKPlatformSettingsFile.LoadResult.Same, PRSDKPlatformSettingsFile.Load(live, stored, true, out _));

        Assert.AreEqual(before, File.GetLastWriteTimeUtc(stored));
    }

    [Test]
    public void Load_WithoutLiveFile_DoesNothing()
    {
        Write(stored, "game B", DateTime.UtcNow);

        Assert.AreEqual(PRSDKPlatformSettingsFile.LoadResult.None, PRSDKPlatformSettingsFile.Load(live, stored, false, out _));

        Assert.IsFalse(File.Exists(live));
    }

    [Test]
    public void Load_LockedPluginFile_FailsAndKeepsBoth()
    {
        File.WriteAllText(live, "game A");
        Write(stored, "game B", DateTime.UtcNow.AddMinutes(-5));

        // Открытый без общего доступа файл Windows не даёт перезаписать.
        using (new FileStream(live, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Assert.AreEqual(PRSDKPlatformSettingsFile.LoadResult.Failed, PRSDKPlatformSettingsFile.Load(live, stored, false, out string error));
            Assert.IsNotNull(error);
        }

        Assert.AreEqual("game A", File.ReadAllText(live));
        Assert.AreEqual("game B", File.ReadAllText(stored));
    }

    [Test]
    public void Save_SkipsEqual_AndWritesChanged()
    {
        File.WriteAllText(live, "v1");
        Assert.IsTrue(PRSDKPlatformSettingsFile.Save(live, stored, out _));
        Assert.IsFalse(PRSDKPlatformSettingsFile.Save(live, stored, out _));

        File.WriteAllText(live, "v2");
        Assert.IsTrue(PRSDKPlatformSettingsFile.Save(live, stored, out _));
        Assert.AreEqual("v2", File.ReadAllText(stored));
    }

    private string PrepareStored()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(stored)!);
        return stored;
    }

    private static void Write(string path, string text, DateTime time)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        File.SetLastWriteTimeUtc(path, time);
    }
}
#endif
