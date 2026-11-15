using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

/// <summary>
/// Обёртка скрипта модуля: <c>#if !PRSDK_DISABLE_&lt;Id&gt;</c> первой строкой и <c>#endif</c> последней.
/// </summary>
/// <remarks>
/// Кодировку, BOM и переводы строк файл сохраняет: меняются только две строки.
/// Файл не в UTF-8 не трогается, чтобы не испортить в нём русский текст.
/// </remarks>
public static class PRModuleGuard
{
    private const string CloseLine = "#endif";

    private static readonly Regex OpenPattern = new(@"^#if\s+!" + PRModuleManifest.DefinePrefix + @"([A-Z0-9_]+)\s*$");

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    /// <summary>
    /// Первая строка обёртки модуля.
    /// </summary>
    public static string OpenLine(string moduleId)
    {
        return "#if !" + PRModuleManifest.DefinePrefix + moduleId;
    }

    /// <summary>
    /// Идентификатор модуля из обёртки файла.
    /// </summary>
    /// <returns><c>null</c>, если файл не обёрнут.</returns>
    public static string ReadGuardId(string path)
    {
        return TryRead(path, out string text, out _) ? ReadGuardIdFromText(text) : null;
    }

    /// <summary>
    /// Оборачивает файл в обёртку модуля. Чужую обёртку заменяет.
    /// </summary>
    /// <returns><c>false</c>, если файл не в UTF-8 и не изменён.</returns>
    public static bool Wrap(string path, string moduleId)
    {
        if (!TryRead(path, out string text, out bool hasBom))
            return false;

        string newLine = text.Contains("\r\n") ? "\r\n" : "\n";
        string body = Unwrapped(text);

        // Исходник без перевода строки в конце: #endif нужна своя строка, а то, что перевода
        // не было, запоминает сама обёртка — после #endif его тоже нет. Иначе Unwrap
        // вернул бы файл с лишним переводом строки.
        bool endsWithNewLine = body.EndsWith("\n", StringComparison.Ordinal);
        string wrapped = endsWithNewLine
            ? OpenLine(moduleId) + newLine + body + CloseLine + newLine
            : OpenLine(moduleId) + newLine + body + newLine + CloseLine;

        Write(path, wrapped, hasBom);
        return true;
    }

    /// <summary>
    /// Оборачивает файлы одного модуля и обновляет их в проекте.
    /// </summary>
    public static PRModuleWrapResult WrapAll(IEnumerable<string> paths, string moduleId)
    {
        return WrapAll(paths.Select(path => (path, moduleId)));
    }

    /// <summary>
    /// Оборачивает файлы, каждый в обёртку своего модуля, и один раз обновляет проект.
    /// </summary>
    /// <remarks>
    /// Единственный путь пакетной обёртки: кнопка у модуля, «Обернуть все» и проверка проекта
    /// идут через него, поэтому ошибки у всех обрабатываются одинаково. Отказ одного файла
    /// не обрывает остальные — иначе уже обёрнутые остались бы без обновления проекта и без отчёта.
    /// </remarks>
    /// <param name="files">Путь файла и идентификатор модуля, которому он принадлежит.</param>
    /// <param name="refresh">
    /// Обновить проект после обхода. Выключается там, где файлы лежат вне проекта: в тестах.
    /// </param>
    public static PRModuleWrapResult WrapAll(IEnumerable<(string Path, string ModuleId)> files, bool refresh = true)
    {
        var result = new PRModuleWrapResult();

        try
        {
            foreach ((string path, string moduleId) in files.ToList())
            {
                try
                {
                    if (Wrap(path, moduleId))
                        result.Wrapped++;
                    else
                        result.WrongEncoding.Add(path);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    result.NotWritten.Add($"{path} — {exception.Message}");
                }
            }
        }
        finally
        {
            // Часть файлов уже изменена, чем бы ни кончился обход: проект должен это увидеть.
            if (refresh)
                AssetDatabase.Refresh();
        }

        return result;
    }

    /// <summary>
    /// Показывает, какие файлы не обёрнуты и почему.
    /// </summary>
    /// <returns><c>true</c>, если обёрнуты все файлы и показывать нечего.</returns>
    public static bool ShowReport(PRModuleWrapResult result)
    {
        if (!result.HasFailures)
            return true;

        EditorUtility.DisplayDialog("Не все файлы обёрнуты", result.Describe(), "Понятно");
        return false;
    }

    /// <summary>
    /// Снимает обёртку модуля с файла.
    /// </summary>
    /// <returns><c>false</c>, если файл не в UTF-8 и не изменён.</returns>
    public static bool Unwrap(string path)
    {
        if (!TryRead(path, out string text, out bool hasBom))
            return false;

        Write(path, Unwrapped(text), hasBom);
        return true;
    }

    private static string ReadGuardIdFromText(string text)
    {
        string[] lines = text.Split('\n');
        int first = Array.FindIndex(lines, line => line.Trim().Length > 0);
        int last = Array.FindLastIndex(lines, line => line.Trim().Length > 0);

        if (first < 0 || first == last || lines[last].Trim() != CloseLine)
            return null;

        Match match = OpenPattern.Match(lines[first].Trim());
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Текст файла без обёртки модуля.
    /// </summary>
    /// <remarks>
    /// Тело вырезается из текста целиком, а не собирается по строкам: переводы строк
    /// у каждой строки остаются свои, даже если в файле они смешаны. Файл, обёрнутый
    /// без перевода строки после <c>#endif</c>, исходно кончался без него — этот перевод
    /// дописала обёртка, и он снимается.
    /// </remarks>
    private static string Unwrapped(string text)
    {
        if (ReadGuardIdFromText(text) == null)
            return text;

        string[] lines = text.Split('\n');
        int first = Array.FindIndex(lines, line => line.Trim().Length > 0);
        int last = Array.FindLastIndex(lines, line => line.Trim().Length > 0);

        int bodyStart = LineOffset(lines, first + 1);
        int bodyEnd = LineOffset(lines, last);
        string body = text.Substring(bodyStart, bodyEnd - bodyStart);

        if (!text.EndsWith("\n", StringComparison.Ordinal) && body.EndsWith("\n", StringComparison.Ordinal))
        {
            int cut = body.EndsWith("\r\n", StringComparison.Ordinal) ? 2 : 1;
            body = body.Substring(0, body.Length - cut);
        }

        return body;
    }

    /// <summary>
    /// С какого символа текста начинается строка с этим номером.
    /// </summary>
    private static int LineOffset(string[] lines, int index)
    {
        int offset = 0;
        for (int line = 0; line < index; line++)
            offset += lines[line].Length + 1;

        return offset;
    }

    private static bool TryRead(string path, out string text, out bool hasBom)
    {
        byte[] bytes = File.ReadAllBytes(path);
        hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        int offset = hasBom ? 3 : 0;

        try
        {
            text = StrictUtf8.GetString(bytes, offset, bytes.Length - offset);
            return true;
        }
        catch (DecoderFallbackException)
        {
            text = null;
            return false;
        }
    }

    /// <summary>
    /// Суффикс временного файла записи.
    /// </summary>
    /// <remarks>
    /// Кончается на «~»: такие файлы Unity не импортирует, поэтому временный файл
    /// не получает .meta и не попадает в сборку, даже если остался после сбоя.
    /// </remarks>
    private const string TempSuffix = ".prguard~";

    /// <summary>
    /// Записывает файл так, что исходник либо меняется целиком, либо остаётся прежним.
    /// </summary>
    /// <remarks>
    /// Запись прямо в исходник сначала обрезает его: кончилось место на диске или запись
    /// оборвалась — и от C#-файла остаётся половина. Поэтому текст пишется во временный
    /// файл рядом, и только готовый файл подменяет оригинал одной операцией.
    /// </remarks>
    private static void Write(string path, string text, bool hasBom)
    {
        string temp = path + TempSuffix;

        try
        {
            File.WriteAllText(temp, text, new UTF8Encoding(hasBom));
            File.Replace(temp, path, null);
        }
        catch
        {
            // Оригинал не тронут; недописанный временный файл рядом с ним не оставляем.
            try
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            throw;
        }
    }
}

/// <summary>
/// Итог пакетной обёртки файлов.
/// </summary>
/// <remarks>
/// Причины отказа разведены: кодировку правят в самом файле, а занятый или защищённый
/// файл освобождают и повторяют.
/// </remarks>
public sealed class PRModuleWrapResult
{
    /// <summary>
    /// Сколько файлов обёрнуто.
    /// </summary>
    public int Wrapped;

    /// <summary>
    /// Файлы не в UTF-8: не тронуты.
    /// </summary>
    public readonly List<string> WrongEncoding = new();

    /// <summary>
    /// Файлы, которые не удалось прочитать или записать, с текстом ошибки: не тронуты.
    /// </summary>
    public readonly List<string> NotWritten = new();

    public int Total => Wrapped + WrongEncoding.Count + NotWritten.Count;

    public bool HasFailures => WrongEncoding.Count > 0 || NotWritten.Count > 0;

    /// <summary>
    /// Текст отчёта для пользователя.
    /// </summary>
    public string Describe()
    {
        string report = $"Обёрнуто файлов: {Wrapped} из {Total}.";

        if (WrongEncoding.Count > 0)
            report += "\n\nНе в UTF-8 — перекодируйте и оберните заново:\n" + string.Join("\n", WrongEncoding);

        if (NotWritten.Count > 0)
        {
            report += "\n\nНе удалось прочитать или записать — освободите файл и повторите:\n" +
                      string.Join("\n", NotWritten);
        }

        return report;
    }
}
