using System;
using System.IO;

/// <summary>
/// Один файл настроек площадки: живой — в плагине, копия — у проекта SDK.
/// </summary>
/// <remarks>
/// Только файловые операции, без AssetDatabase: их проверяют тесты (<c>PRSDKPlatformSettingsFileTests</c>).
/// Копируется сам файл, без <c>.meta</c>: GUID ассета плагина один на все игры, меняется лишь содержимое.
/// </remarks>
public static class PRSDKPlatformSettingsFile
{
    /// <summary>
    /// Чем кончилась загрузка копии проекта в плагин.
    /// </summary>
    public enum LoadResult
    {
        /// <summary>Нет ни живого файла, ни копии проекта — делать нечего.</summary>
        None,
        /// <summary>Файл уже такой, как у проекта.</summary>
        Same,
        /// <summary>На место встала копия проекта.</summary>
        Loaded,
        /// <summary>Копии у проекта не было: она снята с нынешних настроек, они остаются.</summary>
        Seeded,
        /// <summary>Живой файл новее копии: правку не затираем, копия обновлена.</summary>
        KeptNewer,
        /// <summary>Файл не прочитать или не записать: оба остались как были.</summary>
        Failed
    }

    /// <summary>
    /// Запоминает живой файл в копию проекта, если они различаются.
    /// </summary>
    /// <returns><c>true</c> — копия записана.</returns>
    public static bool Save(string live, string stored, out string error)
    {
        error = null;
        try
        {
            if (!File.Exists(live) || SameContent(live, stored))
                return false;

            Directory.CreateDirectory(Path.GetDirectoryName(stored)!);
            WriteThroughTemp(live, stored);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            error = exception.Message;
            return false;
        }
    }

    /// <summary>
    /// Ставит в плагин копию проекта; копии нет — снимает её с нынешнего файла.
    /// </summary>
    /// <param name="preferNewer">Живой файл новее копии — оставить его и обновить копию. Для запуска редактора: правка
    /// могла не дойти до копии. При смене проекта — <c>false</c>: живой файл от прежнего проекта и уже запомнен.</param>
    public static LoadResult Load(string live, string stored, bool preferNewer, out string error)
    {
        error = null;
        try
        {
            // Файла в плагине нет (плагин переустановили, файл удалили), а у проекта копия есть — она и встаёт.
            if (!File.Exists(live))
            {
                if (!File.Exists(stored))
                    return LoadResult.None;

                Directory.CreateDirectory(Path.GetDirectoryName(live)!);
                File.Copy(stored, live);
                return LoadResult.Loaded;
            }

            if (!File.Exists(stored))
                return Save(live, stored, out error) ? LoadResult.Seeded : LoadResult.Failed;

            if (SameContent(live, stored))
                return LoadResult.Same;

            if (preferNewer && File.GetLastWriteTimeUtc(live) > File.GetLastWriteTimeUtc(stored))
                return Save(live, stored, out error) ? LoadResult.KeptNewer : LoadResult.Failed;

            File.Copy(stored, live, overwrite: true);
            return LoadResult.Loaded;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            error = exception.Message;
            return LoadResult.Failed;
        }
    }

    /// <summary>
    /// Пишет копию через временный файл: оборванная запись остаётся во временном, прежняя копия цела.
    /// </summary>
    /// <remarks>
    /// <c>File.Replace</c> падает, пока свежий файл держит антивирус или индексатор: тогда — прямой записью, она не
    /// начнётся, если файл держат, а иначе пройдёт.
    /// </remarks>
    private static void WriteThroughTemp(string source, string target)
    {
        string temp = target + ".new";
        try
        {
            File.Copy(source, temp, overwrite: true);
            if (!File.Exists(target))
            {
                File.Move(temp, target);
                return;
            }

            try
            {
                File.Replace(temp, target, null);
            }
            catch (IOException)
            {
                File.Copy(temp, target, overwrite: true);
            }
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    private static bool SameContent(string left, string right)
    {
        if (!File.Exists(right))
            return false;

        var a = new FileInfo(left);
        var b = new FileInfo(right);
        if (a.Length != b.Length)
            return false;

        using FileStream x = a.OpenRead();
        using FileStream y = b.OpenRead();
        int first, second;
        do
        {
            first = x.ReadByte();
            second = y.ReadByte();
            if (first != second) return false;
        } while (first != -1);
        return true;
    }
}
