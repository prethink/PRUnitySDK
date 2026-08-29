using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

/// <summary>
/// Чтение и запись таблицы переводов в CSV.
/// </summary>
/// <remarks>
/// CSV, а не свой формат: файл открывается таблицей, в которой переводчик привык работать,
/// и возвращается обратно без посредников. Разделитель — точка с запятой: так Excel
/// с русской локалью открывает файл сразу, не спрашивая про столбцы.
/// </remarks>
public static class LocalizationTableCsv
{
    private const char Separator = ';';
    private const string AddressColumn = "Address";
    private const string KeyColumn = "Key";
    private const string GroupColumn = "Group";
    private const string SourceColumn = "Source";
    private const string OwnerColumn = "Owner";

    /// <summary>
    /// Пишет таблицу в файл.
    /// </summary>
    public static void Write(string path, IReadOnlyList<LocalizationTableEntry> entries)
    {
        LangType[] languages = Enum.GetValues(typeof(LangType)).Cast<LangType>().ToArray();
        var builder = new StringBuilder();

        builder.Append(string.Join(Separator.ToString(), new[]
        {
            AddressColumn, KeyColumn, GroupColumn, SourceColumn, OwnerColumn
        }.Concat(languages.Select(language => language.ToString())).Select(Escape)));
        builder.Append('\n');

        foreach (LocalizationTableEntry entry in entries)
        {
            var cells = new List<string>
            {
                entry.Address,
                entry.Key,
                entry.Group,
                entry.Source.ToString(),
                entry.Owner
            };

            cells.AddRange(languages.Select(language =>
                entry.Values.TryGetValue(language, out string value) ? value : string.Empty));

            builder.Append(string.Join(Separator.ToString(), cells.Select(Escape)));
            builder.Append('\n');
        }

        // BOM: без него Excel считает файл однобайтовым и портит кириллицу.
        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(true));
    }

    /// <summary>
    /// Читает таблицу из файла.
    /// </summary>
    /// <remarks>
    /// Колонки языков определяются по заголовку: переводчик мог прислать файл только
    /// с одним языком, и остальные значения трогать не нужно.
    /// </remarks>
    public static List<LocalizationTableEntry> Read(string path)
    {
        var entries = new List<LocalizationTableEntry>();
        List<List<string>> rows = ParseRows(File.ReadAllText(path));

        if (rows.Count == 0)
            return entries;

        List<string> header = rows[0];
        int addressIndex = header.IndexOf(AddressColumn);
        int keyIndex = header.IndexOf(KeyColumn);
        int groupIndex = header.IndexOf(GroupColumn);

        if (addressIndex < 0)
            throw new InvalidDataException($"В файле нет колонки «{AddressColumn}».");

        var languageColumns = new Dictionary<int, LangType>();

        for (int index = 0; index < header.Count; index++)
        {
            if (Enum.TryParse(header[index], out LangType language))
                languageColumns[index] = language;
        }

        for (int row = 1; row < rows.Count; row++)
        {
            List<string> cells = rows[row];

            if (cells.Count <= addressIndex || string.IsNullOrWhiteSpace(cells[addressIndex]))
                continue;

            string[] address = cells[addressIndex].Split('|');

            var entry = new LocalizationTableEntry
            {
                AssetPath = address.Length > 0 ? address[0] : string.Empty,
                ObjectPath = address.Length > 1 ? address[1] : string.Empty,
                PropertyPath = address.Length > 2 ? address[2] : string.Empty,
                Key = keyIndex >= 0 && cells.Count > keyIndex ? cells[keyIndex] : string.Empty,
                Group = groupIndex >= 0 && cells.Count > groupIndex ? cells[groupIndex] : string.Empty
            };

            foreach (KeyValuePair<int, LangType> column in languageColumns)
            {
                if (cells.Count > column.Key)
                    entry.Values[column.Value] = cells[column.Key];
            }

            entries.Add(entry);
        }

        return entries;
    }

    /// <summary>
    /// Разбирает файл на строки и ячейки.
    /// </summary>
    /// <remarks>
    /// Разбор посимвольный: перевод строки внутри кавычек — часть значения, а не новая
    /// строка таблицы. Длинные описания предметов именно так и выглядят.
    /// </remarks>
    private static List<List<string>> ParseRows(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        bool quoted = false;

        for (int index = 0; index < text.Length; index++)
        {
            char symbol = text[index];

            if (quoted)
            {
                if (symbol != '"')
                {
                    cell.Append(symbol);
                    continue;
                }

                bool escaped = index + 1 < text.Length && text[index + 1] == '"';

                if (escaped)
                {
                    cell.Append('"');
                    index++;
                    continue;
                }

                quoted = false;
                continue;
            }

            switch (symbol)
            {
                case '"':
                    quoted = true;
                    break;

                case Separator:
                    row.Add(cell.ToString());
                    cell.Clear();
                    break;

                case '\r':
                    break;

                case '\n':
                    row.Add(cell.ToString());
                    cell.Clear();
                    rows.Add(row);
                    row = new List<string>();
                    break;

                default:
                    cell.Append(symbol);
                    break;
            }
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row);
        }

        // BOM попадает в первую ячейку заголовка и ломает поиск колонки по имени.
        if (rows.Count > 0 && rows[0].Count > 0)
            rows[0][0] = rows[0][0].TrimStart('﻿');

        return rows;
    }

    /// <summary>
    /// Готовит значение к записи в ячейку.
    /// </summary>
    private static string Escape(string value)
    {
        value ??= string.Empty;

        if (value.IndexOfAny(new[] { Separator, '"', '\n', '\r' }) < 0)
            return value;

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    /// <summary>
    /// Имя файла по умолчанию.
    /// </summary>
    public static string GetDefaultFileName()
    {
        return $"localization-{DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.csv";
    }
}
