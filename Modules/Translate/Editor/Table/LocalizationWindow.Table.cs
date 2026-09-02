using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Вкладка «Проект»: переводы, собранные по всему проекту, и обмен ими через CSV.
/// </summary>
/// <remarks>
/// Живёт в том же окне, что и общий список, потому что задача одна — переводы. Разница
/// лишь в охвате: на вкладках `Common` и `Project` правится список в базе, здесь видно
/// всё остальное — словари предметов, наград и подписи на префабах.
/// </remarks>
public partial class LocalizationWindow
{
    private const string TableFoldersKey = "PRUnitySDK.LocalizationTable.Folders";
    private const string TablePrefabsKey = "PRUnitySDK.LocalizationTable.Prefabs";

    private const string SdkTableFolder = "Assets/PRUnitySDK";

    /// <summary>
    /// Собирает папки, по которым окно ищет переводы при первом открытии.
    /// </summary>
    /// <remarks>
    /// Свои папки добавляет проект: методы без параметров, помеченные
    /// <c>[InvokePartial]</c> и возвращающие <c>string</c>, <c>string[]</c> или
    /// <c>IEnumerable&lt;string&gt;</c>. Их может быть сколько угодно, порядок задаёт
    /// <c>Order</c>. SDK не знает, где лежит игра, и не ссылается на неё путём.
    /// </remarks>
    private string GetDefaultTableFolders()
    {
        var folders = new List<string> { SdkTableFolder };

        folders.AddRange(this.CollectPartialResult<string>()
            .Where(folder => !string.IsNullOrWhiteSpace(folder)));

        return string.Join(";", folders.Distinct());
    }

    private List<LocalizationTableEntry> tableEntries = new();
    private string tableFolders = SdkTableFolder;
    private bool tableIncludePrefabs = true;
    private LocalizationTableSource? tableSourceFilter;
    private bool tableOnlyIncomplete;
    private Vector2 tableScroll;
    private string tableStatus = string.Empty;

    /// <summary>
    /// Восстанавливает настройки поиска.
    /// </summary>
    private void InitializeTable()
    {
        tableFolders = EditorPrefs.GetString(TableFoldersKey, GetDefaultTableFolders());
        tableIncludePrefabs = EditorPrefs.GetBool(TablePrefabsKey, true);
    }

    /// <summary>
    /// Запоминает настройки поиска.
    /// </summary>
    private void SaveTableSettings()
    {
        EditorPrefs.SetString(TableFoldersKey, tableFolders);
        EditorPrefs.SetBool(TablePrefabsKey, tableIncludePrefabs);
    }

    /// <summary>
    /// Рисует вкладку.
    /// </summary>
    private void DrawTableTab()
    {
        DrawTableToolbar();
        DrawTableSettings();
        DrawTableFilters();
        DrawTableList();

        if (!string.IsNullOrEmpty(tableStatus))
            EditorGUILayout.HelpBox(tableStatus, MessageType.Info);
    }

    private void DrawTableToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Собрать", EditorStyles.toolbarButton, GUILayout.Width(80f)))
            CollectTable();

        using (new EditorGUI.DisabledScope(tableEntries.Count == 0))
        {
            if (GUILayout.Button("Экспорт CSV", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                ExportTable();
        }

        if (GUILayout.Button("Импорт CSV", EditorStyles.toolbarButton, GUILayout.Width(100f)))
            ImportTable();

        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"Строк: {tableEntries.Count}", GUILayout.Width(90f));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTableSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        tableFolders = EditorGUILayout.TextField(
            new GUIContent("Папки", "Через точку с запятой. Пусто — весь проект."),
            tableFolders);
        tableIncludePrefabs = EditorGUILayout.Toggle(
            new GUIContent("Искать в префабах", "Подписи интерфейса живут на компонентах окон."),
            tableIncludePrefabs);
        EditorGUILayout.EndVertical();
    }

    private void DrawTableFilters()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        string[] names = { "Все", "База", "Ассеты", "Префабы" };
        int current = tableSourceFilter == null ? 0 : (int)tableSourceFilter.Value + 1;
        int selected = GUILayout.Toolbar(current, names, EditorStyles.toolbarButton, GUILayout.Width(280f));

        if (selected != current)
            tableSourceFilter = selected == 0 ? null : (LocalizationTableSource)(selected - 1);

        tableOnlyIncomplete = GUILayout.Toggle(
            tableOnlyIncomplete,
            new GUIContent("Только неполные", "Строки, где заполнены не все языки."),
            EditorStyles.toolbarButton,
            GUILayout.Width(120f));

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTableList()
    {
        tableScroll = EditorGUILayout.BeginScrollView(tableScroll);

        foreach (LocalizationTableEntry entry in FilterTable())
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(entry.Owner, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(entry.Source.ToString(), GUILayout.Width(70f));

            if (GUILayout.Button("Показать", EditorStyles.miniButton, GUILayout.Width(70f)))
                PingTableEntry(entry);

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(entry.Key))
                EditorGUILayout.LabelField("Ключ", entry.Key);

            if (!string.IsNullOrEmpty(entry.Group))
                EditorGUILayout.LabelField("Группа", entry.Group);

            foreach (LangType language in languages)
            {
                entry.Values.TryGetValue(language, out string value);
                EditorGUILayout.LabelField(language.ToString(), string.IsNullOrEmpty(value) ? "—" : value);
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Отбирает строки по фильтрам и общему полю поиска окна.
    /// </summary>
    private IEnumerable<LocalizationTableEntry> FilterTable()
    {
        return tableEntries.Where(entry =>
        {
            if (tableSourceFilter != null && entry.Source != tableSourceFilter.Value)
                return false;

            if (tableOnlyIncomplete && languages.All(language =>
                    entry.Values.TryGetValue(language, out string value) && !string.IsNullOrEmpty(value)))
                return false;

            if (string.IsNullOrWhiteSpace(search))
                return true;

            return Contains(entry.Owner, search)
                   || Contains(entry.Key, search)
                   || Contains(entry.Group, search)
                   || Contains(entry.AssetPath, search)
                   || entry.Values.Values.Any(value => Contains(value, search));
        });
    }

    private void CollectTable()
    {
        string[] searchFolders = tableFolders
            .Split(';')
            .Select(folder => folder.Trim())
            .Where(folder => !string.IsNullOrEmpty(folder) && AssetDatabase.IsValidFolder(folder))
            .ToArray();

        try
        {
            EditorUtility.DisplayProgressBar("Локализация", "Сбор переводов…", 0.5f);
            tableEntries = LocalizationTableCollector.Collect(searchFolders, tableIncludePrefabs);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        int database = tableEntries.Count(entry => entry.Source == LocalizationTableSource.Database);
        int assets = tableEntries.Count(entry => entry.Source == LocalizationTableSource.Asset);
        int prefabs = tableEntries.Count(entry => entry.Source == LocalizationTableSource.Prefab);

        tableStatus = $"Найдено {tableEntries.Count}: база — {database}, ассеты — {assets}, префабы — {prefabs}.";
    }

    private void ExportTable()
    {
        string path = EditorUtility.SaveFilePanel(
            "Выгрузить переводы",
            string.Empty,
            LocalizationTableCsv.GetDefaultFileName(),
            "csv");

        if (string.IsNullOrEmpty(path))
            return;

        LocalizationTableCsv.Write(path, FilterTable().ToList());
        tableStatus = $"Выгружено в {path}.";
    }

    private void ImportTable()
    {
        string path = EditorUtility.OpenFilePanel("Загрузить переводы", string.Empty, "csv");

        if (string.IsNullOrEmpty(path))
            return;

        List<LocalizationTableEntry> imported;

        try
        {
            imported = LocalizationTableCsv.Read(path);
        }
        catch (Exception exception)
        {
            tableStatus = $"Не удалось прочитать файл: {exception.Message}";
            return;
        }

        // Спрашиваем всегда: импорт трогает ассеты по всему проекту, и отменить это
        // можно только системой контроля версий.
        bool confirmed = EditorUtility.DisplayDialog(
            "Загрузить переводы",
            $"В файле строк: {imported.Count}. Значения будут записаны в ассеты и префабы проекта.",
            "Загрузить",
            "Отмена");

        if (!confirmed)
            return;

        LocalizationTableWriter.Report report = LocalizationTableWriter.Apply(imported);
        tableStatus = report.ToString();

        foreach (string address in report.MissingAddresses.Take(10))
            PRLog.WriteWarning(typeof(LocalizationWindow), $"Адрес не найден: {address}");

        CollectTable();
    }

    private static void PingTableEntry(LocalizationTableEntry entry)
    {
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(entry.AssetPath);

        if (asset != null)
            EditorGUIUtility.PingObject(asset);
    }
}
