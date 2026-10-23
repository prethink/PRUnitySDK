#if PRSDK_TESTS
using System.IO;
using NUnit.Framework;

/// <summary>
/// Хранилище площадки в редакторе: запись на диск, чтение заново, файл там, где его ищет перенос сохранений.
/// </summary>
public sealed class PRSDKEditorStorageTests
{
    private string platform;

    [SetUp]
    public void SetUp() => platform = "Test_" + Path.GetRandomFileName().Replace(".", string.Empty);

    [TearDown]
    public void TearDown()
    {
        string folder = Path.Combine(PRSDKEditorStorage.EditorFolder, platform);
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
    }

    [Test]
    public void Flush_ThenNewInstance_ReadsSameValues()
    {
        var storage = new PRSDKEditorStorage(platform);
        storage.Set("prsave", "{\"a\":1}");
        storage.Set("prlang", "ru");
        storage.Flush();

        var again = new PRSDKEditorStorage(platform);
        Assert.AreEqual("{\"a\":1}", again.Get("prsave"));
        Assert.AreEqual("ru", again.Get("prlang"));
        Assert.AreEqual(string.Empty, again.Get("missing"));
    }

    [Test]
    public void File_LiesWhereSaveStoresLook()
    {
        var storage = new PRSDKEditorStorage(platform);
        storage.Set("prsave", "x");
        storage.Flush();

        Assert.IsTrue(File.Exists($"{PRSDKEditorStorage.EditorFolder}/{platform}/{PRSDKEditorStorage.FileName}"));
        Assert.AreEqual($"{PRSDKEditorStorage.EditorFolder}/{platform}/{PRSDKEditorStorage.FileName}",
            PRSDKEditorStorage.PathOf(platform, PRSDKEditorStorage.FileName));
    }

    [Test]
    public void SetWithoutFlush_DoesNotWrite()
    {
        new PRSDKEditorStorage(platform).Set("prsave", "x");

        Assert.IsFalse(File.Exists(PRSDKEditorStorage.PathOf(platform, PRSDKEditorStorage.FileName)));
    }
}
#endif
