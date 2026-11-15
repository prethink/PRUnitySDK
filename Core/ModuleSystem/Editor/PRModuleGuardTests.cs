#if PRSDK_TESTS
using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;

/// <summary>
/// Запись обёртки модуля в исходник: файл меняется целиком или остаётся прежним.
/// </summary>
public class PRModuleGuardTests
{
    private const string ModuleId = "GUARD_TEST";

    private static readonly byte[] Bom = { 0xEF, 0xBB, 0xBF };

    private string folder;

    [SetUp]
    public void SetUp()
    {
        // Папка Temp проекта: вне Assets, поэтому редактор пробные файлы не импортирует.
        folder = FileUtil.GetUniqueTempPathInProject();
        Directory.CreateDirectory(folder);
    }

    [TearDown]
    public void TearDown()
    {
        if (!Directory.Exists(folder))
            return;

        // Защищённый от записи файл удалить нельзя, пока метка не снята.
        foreach (string path in Directory.GetFiles(folder))
            File.SetAttributes(path, FileAttributes.Normal);

        Directory.Delete(folder, true);
    }

    private string Create(string name, byte[] bytes)
    {
        string path = Path.Combine(folder, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private static byte[] Utf8(string text)
    {
        return Encoding.UTF8.GetBytes(text);
    }

    private int TempFilesLeft()
    {
        return Directory.GetFiles(folder, "*~").Length;
    }

    [Test]
    public void Wrap_ReplacesFile_AndUnwrapRestoresIt()
    {
        byte[] original = Utf8("using System;\n\npublic class A\n{\n}\n");
        string path = Create("lf.cs", original);

        Assert.IsTrue(PRModuleGuard.Wrap(path, ModuleId));
        Assert.AreEqual(ModuleId, PRModuleGuard.ReadGuardId(path));

        string wrapped = File.ReadAllText(path);
        StringAssert.StartsWith(PRModuleGuard.OpenLine(ModuleId) + "\n", wrapped);
        StringAssert.EndsWith("#endif\n", wrapped);
        Assert.IsFalse(wrapped.Contains("\r"), "В файл с LF попал CRLF");

        Assert.IsTrue(PRModuleGuard.Unwrap(path));
        CollectionAssert.AreEqual(original, File.ReadAllBytes(path));
        Assert.AreEqual(0, TempFilesLeft(), "Остался временный файл");
    }

    [Test]
    public void Wrap_KeepsBomAndCrlf()
    {
        byte[] original = Bom.Concat(Utf8("// Проверка\r\npublic class B\r\n{\r\n}\r\n")).ToArray();
        string path = Create("crlf.cs", original);

        Assert.IsTrue(PRModuleGuard.Wrap(path, ModuleId));

        byte[] wrapped = File.ReadAllBytes(path);
        CollectionAssert.AreEqual(Bom, wrapped.Take(3).ToArray(), "BOM потерян");

        string text = Encoding.UTF8.GetString(wrapped, 3, wrapped.Length - 3);
        StringAssert.StartsWith(PRModuleGuard.OpenLine(ModuleId) + "\r\n", text);
        StringAssert.EndsWith("#endif\r\n", text);
        Assert.AreEqual(text.Count(symbol => symbol == '\n'), text.Count(symbol => symbol == '\r'), "Переводы строк смешаны");
        StringAssert.Contains("// Проверка", text);

        Assert.IsTrue(PRModuleGuard.Unwrap(path));
        CollectionAssert.AreEqual(original, File.ReadAllBytes(path));
        Assert.AreEqual(0, TempFilesLeft(), "Остался временный файл");
    }

    [Test]
    public void Wrap_NoTrailingNewLine_UnwrapRestoresBytes()
    {
        byte[] original = Utf8("public class E\n{\n}");
        string path = Create("no-eol.cs", original);

        Assert.IsTrue(PRModuleGuard.Wrap(path, ModuleId));
        Assert.AreEqual(ModuleId, PRModuleGuard.ReadGuardId(path));
        StringAssert.EndsWith("}\n#endif", File.ReadAllText(path));

        // Повторная обёртка не копит переводы строк.
        Assert.IsTrue(PRModuleGuard.Wrap(path, ModuleId));
        Assert.IsTrue(PRModuleGuard.Unwrap(path));
        CollectionAssert.AreEqual(original, File.ReadAllBytes(path));
    }

    [Test]
    public void Wrap_MixedLineEndings_UnwrapRestoresBytes()
    {
        byte[] original = Utf8("// A\r\npublic class F\n{\r\n}\n");
        string path = Create("mixed.cs", original);

        Assert.IsTrue(PRModuleGuard.Wrap(path, ModuleId));
        Assert.IsTrue(PRModuleGuard.Unwrap(path));
        CollectionAssert.AreEqual(original, File.ReadAllBytes(path));
    }

    [Test]
    public void Wrap_WriteFails_LeavesOriginalAndNoTempFile()
    {
        byte[] original = Utf8("public class C\n{\n}\n");
        string path = Create("locked.cs", original);
        File.SetAttributes(path, FileAttributes.ReadOnly);

        Assert.Catch<UnauthorizedAccessException>(() => PRModuleGuard.Wrap(path, ModuleId));

        CollectionAssert.AreEqual(original, File.ReadAllBytes(path), "Исходник изменён, хотя запись не удалась");
        Assert.AreEqual(0, TempFilesLeft(), "Остался временный файл");
    }

    [Test]
    public void Wrap_NotUtf8_LeavesFileUntouched()
    {
        // Русский текст в Windows-1251: как UTF-8 не читается.
        byte[] original = { 0x2F, 0x2F, 0x20, 0xCF, 0xF0, 0xE8, 0xE2, 0xE5, 0xF2, 0x0A };
        string path = Create("cp1251.cs", original);

        Assert.IsFalse(PRModuleGuard.Wrap(path, ModuleId));
        CollectionAssert.AreEqual(original, File.ReadAllBytes(path));
        Assert.AreEqual(0, TempFilesLeft(), "Остался временный файл");
    }

    [Test]
    public void WrapAll_FailedFilesDoNotStopTheRest()
    {
        byte[] plain = Utf8("public class D\n{\n}\n");

        // Отказы стоят первыми: обход должен дойти до годного файла после них.
        string locked = Create("locked.cs", plain);
        File.SetAttributes(locked, FileAttributes.ReadOnly);
        string wrongEncoding = Create("cp1251.cs", new byte[] { 0xCF, 0xF0, 0xE8, 0x0A });
        string good = Create("good.cs", plain);
        string other = Create("other.cs", plain);

        PRModuleWrapResult result = PRModuleGuard.WrapAll(
            new[] { (locked, ModuleId), (wrongEncoding, ModuleId), (good, ModuleId), (other, "OTHER_TEST") },
            refresh: false);

        Assert.AreEqual(2, result.Wrapped);
        Assert.AreEqual(4, result.Total);
        Assert.IsTrue(result.HasFailures);
        CollectionAssert.AreEqual(new[] { wrongEncoding }, result.WrongEncoding);
        Assert.AreEqual(1, result.NotWritten.Count);
        StringAssert.StartsWith(locked, result.NotWritten[0]);

        // У каждого файла обёртка своего модуля.
        Assert.AreEqual(ModuleId, PRModuleGuard.ReadGuardId(good));
        Assert.AreEqual("OTHER_TEST", PRModuleGuard.ReadGuardId(other));
        CollectionAssert.AreEqual(plain, File.ReadAllBytes(locked));
        Assert.AreEqual(0, TempFilesLeft(), "Остался временный файл");

        string report = result.Describe();
        StringAssert.Contains("2 из 4", report);
        StringAssert.Contains(wrongEncoding, report);
        StringAssert.Contains(locked, report);
    }
}
#endif
