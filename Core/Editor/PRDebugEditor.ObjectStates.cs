using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Вкладка состояний объектов сцены.
/// </summary>
/// <remarks>
/// Отвечает на вопросы, которые иначе приходится выяснять через файл сохранения:
/// какие объекты стоят на уровне, что из них показано, у кого какой ключ и есть ли
/// по нему запись. Отдельно показывает записи, оставшиеся от объектов, которых
/// на текущем уровне нет, — по ним видно и прогресс других уровней, и мусор,
/// накопившийся от удалённых объектов.
/// </remarks>
public partial class PRDebugEditor
{
    private readonly List<ObjectStateRow> objectStates = new();
    private readonly HashSet<string> objectStateKeys = new(StringComparer.Ordinal);
    private int objectStatesSavedTotal;
    private int objectStatesOrphanCount;

    private struct ObjectStateRow
    {
        public string Key;
        public string Name;
        public string Group;
        public bool IsOpened;
        public bool IsSaved;
        public bool SavedIsActive;
        public bool HasValues;

        /// <summary>
        /// Запись есть, а объекта на сцене нет.
        /// </summary>
        public bool IsOrphan;

        public GameObject GameObject;
    }

    private void CaptureObjectStates()
    {
        ObjectStateTracker tracker = PRUnitySDK.Trackers.ObjectStates;
        IReadOnlyDictionary<string, SceneObjectState> saved = tracker.Saved;

        objectStatesSavedTotal = saved.Count;

        objectStateKeys.Clear();

        foreach (SaveableObjectState state in tracker.Loaded)
        {
            if (state == null)
                continue;

            string key = state.StateId;
            SceneObjectState record = null;
            bool hasRecord = !string.IsNullOrEmpty(key) && saved.TryGetValue(key, out record);

            if (!string.IsNullOrEmpty(key))
                objectStateKeys.Add(key);

            objectStates.Add(new ObjectStateRow
            {
                Key = string.IsNullOrEmpty(key) ? "<нет ключа>" : key,
                Name = state.Target != null ? state.Target.name : state.name,
                Group = state.Group != null ? state.Group.Value : "-",
                IsOpened = state.IsOpened,
                IsSaved = hasRecord,
                SavedIsActive = record != null && record.IsActive,
                HasValues = record != null && record.HasValues,
                GameObject = state.Target != null ? state.Target : state.gameObject
            });
        }

        // Записи без объекта на сцене: чужие уровни и мусор от удалённых объектов.
        foreach (KeyValuePair<string, SceneObjectState> pair in saved)
        {
            if (objectStateKeys.Contains(pair.Key))
                continue;

            objectStatesOrphanCount++;

            objectStates.Add(new ObjectStateRow
            {
                Key = pair.Key,
                Name = "<не на этой сцене>",
                Group = "-",
                IsSaved = true,
                SavedIsActive = pair.Value != null && pair.Value.IsActive,
                HasValues = pair.Value != null && pair.Value.HasValues,
                IsOrphan = true
            });
        }

        objectStates.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
    }

    private void DrawObjectStates()
    {
        DrawTabDescription("Состояния объектов сцены, которые переживают перезапуск: что стоит на уровне, что из этого показано и что записано в сохранение. Запись появляется только при отличии от значений по умолчанию.");
        DrawSectionHeader($"Object states ({objectStates.Count})");

        ObjectStateProgress progress = PRUnitySDK.IsInitialized
            ? PRUnitySDK.Trackers.ObjectStates.GetSceneProgress()
            : default;

        EditorGUILayout.HelpBox(
            $"На сцене: показано {progress.Opened} из {progress.Total}. " +
            $"В сохранении: {objectStatesSavedTotal}, из них не на этой сцене: {objectStatesOrphanCount}. " +
            "Запись появляется только при отличии от значений по умолчанию, поэтому нетронутых объектов в сохранении нет.",
            MessageType.None);

        DrawFixedRow(true, ("Object", 200), ("Group", 110), ("Key", 260),
            ("Scene", 60), ("Saved", 60), ("Values", 55), ("Object", 60));

        int count = 0;

        // Перебираем сам список: отрисовка его не меняет, а копия создавалась
        // на каждое событие GUI.
        foreach (ObjectStateRow row in objectStates)
        {
            if (!MatchesSearch(row.Name, row.Key, row.Group))
                continue;

            count++;
            EditorGUILayout.BeginHorizontal();
            Label(row.Name, 200);
            Label(row.Group, 110);
            Label(row.Key, 260);
            Label(row.IsOrphan ? "-" : (row.IsOpened ? "показан" : "спрятан"), 60);
            Label(row.IsSaved ? (row.SavedIsActive ? "показан" : "спрятан") : "нет", 60);
            Label(row.HasValues ? "да" : "-", 55);
            DrawObjectButton(row.GameObject);
            EditorGUILayout.EndHorizontal();
        }

        DrawEmpty(count, "No object states match the current search.");
    }
}
