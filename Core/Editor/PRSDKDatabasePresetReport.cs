using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Что делать с тем, что уже лежит в каталоге.
/// </summary>
public enum PRSDKDatabasePresetApplyMode
{
    /// <summary>
    /// Состав каталога равен набору: лишнее убирается.
    /// </summary>
    /// <remarks>
    /// Основной режим переключения между играми — состав должен быть ровно тем,
    /// что задумано, иначе в билд утечёт лишнее.
    /// </remarks>
    Replace,

    /// <summary>
    /// Существующее остаётся, из набора добавляется недостающее.
    /// </summary>
    /// <remarks>
    /// Нужен, когда набор — не полный состав игры, а добавка: например, общий пакет
    /// предметов поверх уже собранной базы.
    /// </remarks>
    Merge
}

/// <summary>
/// Насколько серьёзна находка при сверке набора с проектом.
/// </summary>
public enum PRSDKDatabasePresetSeverity
{
    /// <summary>
    /// Стоит знать, но делать ничего не нужно.
    /// </summary>
    Info,

    /// <summary>
    /// Применить можно, но результат может отличаться от ожидаемого.
    /// </summary>
    Warning,

    /// <summary>
    /// Часть набора применить не получится.
    /// </summary>
    Error
}

/// <summary>
/// Одна находка при сверке набора с проектом.
/// </summary>
public readonly struct PRSDKDatabasePresetIssue
{
    public PRSDKDatabasePresetSeverity Severity { get; }

    /// <summary>
    /// Каталог, к которому относится находка.
    /// </summary>
    public string Section { get; }

    public string Message { get; }

    public PRSDKDatabasePresetIssue(PRSDKDatabasePresetSeverity severity, string section, string message)
    {
        Severity = severity;
        Section = section;
        Message = message;
    }
}

/// <summary>
/// Каталог набора, сверенный с проектом.
/// </summary>
public sealed class PRSDKDatabasePresetResolvedSection
{
    /// <summary>
    /// Путь сериализованного свойства каталога.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Читаемое название каталога.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Ассеты, которые встанут в каталог.
    /// </summary>
    public List<UnityEngine.Object> Assets { get; } = new();

    /// <summary>
    /// Ассеты, которые сейчас в каталоге, но в набор не входят.
    /// </summary>
    /// <remarks>
    /// Что с ними станет, решает режим применения: при замене они уйдут из базы,
    /// при дополнении останутся. Сами файлы в проекте не трогаются в любом случае —
    /// набор описывает состав, а не содержимое.
    /// </remarks>
    public List<UnityEngine.Object> Outgoing { get; } = new();

    /// <summary>
    /// Каталог применять нельзя.
    /// </summary>
    public bool Skipped { get; set; }

    public PRSDKDatabasePresetResolvedSection(string path, string label)
    {
        Path = path;
        Label = label;
    }
}

/// <summary>
/// Результат сверки набора с проектом.
/// </summary>
/// <remarks>
/// Отчёт готовится до изменения базы: набор приезжает из другой игры или ветки, где ассеты
/// могли переехать или исчезнуть, и решение применять его принимает человек.
/// </remarks>
public sealed class PRSDKDatabasePresetReport
{
    /// <summary>
    /// Набор, который сверяли.
    /// </summary>
    public PRSDKDatabasePreset Preset { get; }

    /// <summary>
    /// Каталоги, готовые к применению.
    /// </summary>
    public List<PRSDKDatabasePresetResolvedSection> Sections { get; } = new();

    /// <summary>
    /// Находки сверки.
    /// </summary>
    public List<PRSDKDatabasePresetIssue> Issues { get; } = new();

    /// <summary>
    /// Сколько ассетов встанет в базу.
    /// </summary>
    public int ResolvedCount => Sections.Where(section => !section.Skipped).Sum(section => section.Assets.Count);

    /// <summary>
    /// Сколько элементов лежит в базе сверх набора.
    /// </summary>
    /// <remarks>
    /// Набор мог быть сохранён до того, как в базу добавили новые предметы. При замене
    /// состава такие предметы уйдут, при дополнении останутся.
    /// </remarks>
    public int OutgoingCount => Sections.Where(section => !section.Skipped).Sum(section => section.Outgoing.Count);

    /// <summary>
    /// Сколько записей набора применить не удалось.
    /// </summary>
    public int ErrorCount => Issues.Count(issue => issue.Severity == PRSDKDatabasePresetSeverity.Error);

    /// <summary>
    /// Сколько записей применится, но требует внимания.
    /// </summary>
    public int WarningCount => Issues.Count(issue => issue.Severity == PRSDKDatabasePresetSeverity.Warning);

    /// <summary>
    /// Применять нечего.
    /// </summary>
    public bool IsEmpty => Sections.All(section => section.Skipped);

    public PRSDKDatabasePresetReport(PRSDKDatabasePreset preset)
    {
        Preset = preset;
    }

    public void AddIssue(PRSDKDatabasePresetSeverity severity, string section, string message)
    {
        Issues.Add(new PRSDKDatabasePresetIssue(severity, section, message));
    }
}
