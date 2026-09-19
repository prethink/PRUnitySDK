using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Вкладка текущего содержимого сохранения.
/// </summary>
/// <remarks>
/// Отвечает на вопрос, который иначе решается через расшифровку файла сохранения: что
/// сейчас лежит в <see cref="ProjectData"/> и менялось ли оно только что. Значения берутся
/// из памяти, а не с диска, поэтому видно и то, что ещё не сохранено.
/// <para>
/// Изменения ловятся сравнением со снимком прошлого обновления окна: строка помечается
/// временем и прежним значением. Поэтому частота обновления окна задаёт и точность
/// момента — два изменения внутри одного интервала сольются в одно.
/// </para>
/// </remarks>
public partial class PRDebugEditor
{
    /// <summary>
    /// Сколько секунд строка считается «только что изменившейся» и подсвечивается.
    /// </summary>
    private const double SaveValueHighlightSeconds = 5d;

    private readonly List<SaveValueRow> saveValues = new();

    /// <summary>
    /// Значения на момент прошлого обновления: по ним считается, что изменилось.
    /// </summary>
    private readonly Dictionary<string, string> saveValueBaseline = new(StringComparer.Ordinal);

    /// <summary>
    /// История изменений: прежнее значение и когда оно сменилось.
    /// </summary>
    private readonly Dictionary<string, SaveValueChange> saveValueChanges = new(StringComparer.Ordinal);

    private bool saveValuesOnlyChanged;
    private int saveValuesChangedCount;
    private string saveValuesError;

    private static GUIStyle saveValueChangedStyle;

    private struct SaveValueRow
    {
        public string Category;
        public string Key;
        public string Type;
        public string Value;
    }

    private struct SaveValueChange
    {
        public string PreviousValue;
        public DateTime ChangedAtUtc;

        /// <summary>
        /// Ключа не было вовсе: значение появилось, а не изменилось.
        /// </summary>
        public bool IsNew;
    }

    private void CaptureSaveValues()
    {
        saveValuesError = null;

        if (!GameManager.HasInstance)
        {
            saveValuesError = "GameManager ещё не поднялся: сохранение не загружено.";
            return;
        }

        ProjectData data;

        try
        {
            data = GameManager.Instance.GetProjectData();
        }
        catch (Exception exception)
        {
            // Данные проекта запрашиваются до их загрузки: это не поломка окна,
            // а нормальное состояние первых секунд запуска.
            saveValuesError = exception.Message;
            return;
        }

        if (data == null)
            return;

        CaptureProperties(data.ProjectProperties);
        CaptureResources(data);
        CaptureRewards(data);
        CaptureCollections(data);

        DetectChanges();
    }

    private void CaptureProperties(ProjectProperties properties)
    {
        if (properties == null)
            return;

        AddValues("long", "long", properties.LongProperties);
        AddValues("float", "float", properties.FloatProperties);
        AddValues("decimal", "decimal", properties.DecimalProperties);
        AddValues("bool", "bool", properties.BoolProperties);
        AddValues("string", "string", properties.StringProperties);
        AddValues("DateTime", "DateTime", properties.DateTimeProperties);
        AddValues("object", "object", properties.ObjectProperties);
    }

    private void CaptureResources(ProjectData data)
    {
        if (data.Resources == null)
            return;

        AddValues("Resources", "long", data.Resources);
    }

    private void CaptureRewards(ProjectData data)
    {
        if (data.TimeLimitedRewards == null)
            return;

        AddValues("Rewards", "DateTime", data.TimeLimitedRewards);
    }

    /// <summary>
    /// Списки сохранения одной строкой: их содержимое смотрят на своих вкладках,
    /// а здесь важно заметить, что их размер изменился.
    /// </summary>
    private void CaptureCollections(ProjectData data)
    {
        AddValue("Collections", nameof(data.OpenedItems), "count",
            data.OpenedItems != null ? data.OpenedItems.Count.ToString() : "-");

        AddValue("Collections", nameof(data.SelectedPlayerItems), "count",
            data.SelectedPlayerItems != null ? data.SelectedPlayerItems.Count.ToString() : "-");
    }

    private void AddValues<T>(string category, string type, IDictionary<string, T> values)
    {
        if (values == null)
            return;

        foreach (KeyValuePair<string, T> pair in values)
            AddValue(category, pair.Key, type, FormatSaveValue(pair.Value));
    }

    private void AddValue(string category, string key, string type, string value)
    {
        saveValues.Add(new SaveValueRow
        {
            Category = category,
            Key = key,
            Type = type,
            Value = value
        });
    }

    /// <summary>
    /// Значение в виде, пригодном для сравнения и показа.
    /// </summary>
    /// <remarks>
    /// Инвариантная культура: иначе дробное число в одной локали пишется через запятую,
    /// а в другой через точку, и смена локали редактора читалась бы как изменение
    /// сохранения.
    /// </remarks>
    private static string FormatSaveValue(object value)
    {
        return value switch
        {
            null => "null",
            decimal number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            float number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            double number => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            DateTime date => date.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    /// <summary>
    /// Сравнивает собранные значения с прошлым обновлением.
    /// </summary>
    /// <remarks>
    /// Исчезнувшие ключи из истории не убираются: запись «было столько» остаётся
    /// единственным следом удалённого свойства, а ради него и открывают эту вкладку.
    /// </remarks>
    private void DetectChanges()
    {
        bool hasBaseline = saveValueBaseline.Count > 0;
        DateTime now = DateTime.UtcNow;

        foreach (SaveValueRow row in saveValues)
        {
            string key = GetSaveValueKey(row);
            bool hadValue = saveValueBaseline.TryGetValue(key, out string previous);

            if (hadValue && previous == row.Value)
                continue;

            // Первый сбор: значения не менялись, их просто ещё не с чем было сравнивать.
            if (!hasBaseline)
            {
                saveValueBaseline[key] = row.Value;
                continue;
            }

            saveValueChanges[key] = new SaveValueChange
            {
                PreviousValue = hadValue ? previous : "-",
                ChangedAtUtc = now,
                IsNew = !hadValue
            };

            saveValueBaseline[key] = row.Value;
        }

        saveValuesChangedCount = saveValueChanges.Count;
    }

    private static string GetSaveValueKey(SaveValueRow row) => $"{row.Category}/{row.Key}";

    private void DrawSaveValues()
    {
        DrawTabDescription(
            "Что лежит в сохранении прямо сейчас. Значения берутся из памяти, а не с диска, " +
            "поэтому видно и то, что ещё не записано. Изменения ловятся сравнением с прошлым " +
            "обновлением окна: строка помечается прежним значением и временем.");

        if (!string.IsNullOrEmpty(saveValuesError))
        {
            EditorGUILayout.HelpBox(saveValuesError, MessageType.Info);
            return;
        }

        DrawSaveValuesToolbar();
        DrawSectionHeader($"Save values ({saveValues.Count})");

        DrawFixedRow(true, ("Category", 100), ("Key", 240), ("Type", 70),
            ("Value", 200), ("Was", 160), ("Changed", 80));

        int count = 0;
        DateTime now = DateTime.UtcNow;

        foreach (SaveValueRow row in saveValues)
        {
            string key = GetSaveValueKey(row);
            bool hasChange = saveValueChanges.TryGetValue(key, out SaveValueChange change);

            if (saveValuesOnlyChanged && !hasChange)
                continue;

            if (!MatchesSearch(row.Category, row.Key, row.Value, row.Type))
                continue;

            count++;
            DrawSaveValueRow(row, hasChange, change, now);
        }

        DrawEmptySaveValues(count);
    }

    private void DrawSaveValuesToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField($"Изменилось значений: {saveValuesChangedCount}",
                EditorStyles.miniLabel, GUILayout.Width(190f));

            saveValuesOnlyChanged = GUILayout.Toggle(saveValuesOnlyChanged, "Только изменённые",
                EditorStyles.miniButton, GUILayout.Width(140f));

            if (GUILayout.Button("Забыть изменения", EditorStyles.miniButton, GUILayout.Width(130f)))
            {
                saveValueChanges.Clear();
                saveValuesChangedCount = 0;
            }

            GUILayout.FlexibleSpace();

            // Кулдаун сохранения здесь мешает: смотрят ровно на то, что уйдёт на диск.
            if (GUILayout.Button("Сохранить сейчас", EditorStyles.miniButton, GUILayout.Width(130f)))
                GameManager.Instance.SaveProjectData(true);
        }
    }

    private void DrawSaveValueRow(SaveValueRow row, bool hasChange, SaveValueChange change, DateTime now)
    {
        double secondsSinceChange = hasChange ? (now - change.ChangedAtUtc).TotalSeconds : double.MaxValue;
        bool highlight = hasChange && secondsSinceChange <= SaveValueHighlightSeconds;

        saveValueChangedStyle ??= new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = EditorGUIUtility.isProSkin
                ? new Color(0.45f, 0.85f, 0.48f)
                : new Color(0.13f, 0.52f, 0.18f) }
        };

        EditorGUILayout.BeginHorizontal();
        Label(row.Category, 100);
        Label(row.Key, 240);
        Label(row.Type, 70);

        EditorGUILayout.LabelField(row.Value, highlight ? saveValueChangedStyle : EditorStyles.label,
            GUILayout.Width(200f));

        Label(hasChange ? (change.IsNew ? "<появилось>" : change.PreviousValue) : "-", 160);
        Label(hasChange ? FormatChangeAge(secondsSinceChange) : "-", 80);
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Сколько прошло с изменения.
    /// </summary>
    /// <remarks>
    /// Относительное время, а не часы: вопрос к этой вкладке всегда «только что или
    /// давно», и разница в секундах отвечает на него быстрее отметки времени.
    /// </remarks>
    private static string FormatChangeAge(double seconds)
    {
        if (seconds < 1d)
            return "сейчас";

        if (seconds < 60d)
            return $"{seconds:F0} с назад";

        return $"{seconds / 60d:F0} мин назад";
    }

    private void DrawEmptySaveValues(int count)
    {
        if (count > 0)
            return;

        EditorGUILayout.HelpBox(
            saveValuesOnlyChanged
                ? "Пока ничего не менялось. Значения сравниваются с прошлым обновлением окна."
                : "В сохранении нет значений, подходящих под поиск.",
            MessageType.Info);
    }
}
