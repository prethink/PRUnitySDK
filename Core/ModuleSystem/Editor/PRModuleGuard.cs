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
        string body = Unwrapped(text, newLine);

        if (!body.EndsWith("\n", StringComparison.Ordinal))
            body += newLine;

        Write(path, OpenLine(moduleId) + newLine + body + CloseLine + newLine, hasBom);
        return true;
    }

    /// <summary>
    /// Оборачивает несколько файлов и обновляет их в проекте.
    /// </summary>
    /// <returns>Файлы, которые остались без обёртки, потому что они не в UTF-8.</returns>
    public static List<string> WrapAll(IEnumerable<string> paths, string moduleId)
    {
        var failed = new List<string>();

        foreach (string path in paths.ToList())
        {
            if (!Wrap(path, moduleId))
                failed.Add(path);
        }

        AssetDatabase.Refresh();
        return failed;
    }

    /// <summary>
    /// Снимает обёртку модуля с файла.
    /// </summary>
    /// <returns><c>false</c>, если файл не в UTF-8 и не изменён.</returns>
    public static bool Unwrap(string path)
    {
        if (!TryRead(path, out string text, out bool hasBom))
            return false;

        string newLine = text.Contains("\r\n") ? "\r\n" : "\n";
        Write(path, Unwrapped(text, newLine), hasBom);
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

    private static string Unwrapped(string text, string newLine)
    {
        if (ReadGuardIdFromText(text) == null)
            return text;

        string[] lines = text.Split('\n');
        int first = Array.FindIndex(lines, line => line.Trim().Length > 0);
        int last = Array.FindLastIndex(lines, line => line.Trim().Length > 0);

        var builder = new StringBuilder();
        for (int index = first + 1; index < last; index++)
            builder.Append(lines[index].TrimEnd('\r')).Append(newLine);

        return builder.ToString();
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

    private static void Write(string path, string text, bool hasBom)
    {
        File.WriteAllText(path, text, new UTF8Encoding(hasBom));
    }
}
