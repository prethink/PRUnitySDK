using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Отправки адаптерам метрик и их параметры в окне PRUnitySDK Debug.
/// </summary>
public sealed class MetricsDebugTab : IPRDebugTab
{
    private MetricDebugEntry[] entries = Array.Empty<MetricDebugEntry>();
    private readonly HashSet<long> expanded = new();

    /// <inheritdoc />
    public string Title => $"Metrics ({entries.Length})";
    /// <inheritdoc />
    public int Order => -100;
    /// <inheritdoc />
    public bool AvailableInEditMode => true;

    /// <inheritdoc />
    public void Refresh(PRDebugTabContext context)
    {
        entries = MetricDebugHistory.Snapshot();
        var present = new HashSet<long>();
        foreach (MetricDebugEntry entry in entries)
            present.Add(entry.Sequence);
        expanded.RemoveWhere(sequence => !present.Contains(sequence));
    }

    /// <inheritdoc />
    public void Draw(PRDebugTabContext context)
    {
        EditorGUILayout.LabelField(
            "Metric calls and parameters. Expand a record to inspect JSON.",
            EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.BeginHorizontal();
        MetricDebugHistory.CaptureEnabled = GUILayout.Toggle(
            MetricDebugHistory.CaptureEnabled, "Capture", EditorStyles.toolbarButton, GUILayout.Width(65f));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(55f)))
        {
            MetricDebugHistory.Clear();
            Refresh(context);
        }
        EditorGUILayout.EndHorizontal();

        int sent = 0;
        foreach (MetricDebugEntry entry in entries)
            if (!entry.Ignored)
                sent++;
        EditorGUILayout.LabelField(
            new GUIContent($"Sent: {sent} | Ignored: {entries.Length - sent} | Last {MetricDebugHistory.Capacity} calls",
                "Sent means handed to the adapter; server delivery is not confirmed."),
            EditorStyles.wordWrappedMiniLabel);

        if (entries.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "No metrics recorded. Calls are captured in Editor even while this window is closed.",
                MessageType.Info);
            return;
        }

        int visible = 0;
        for (int index = entries.Length - 1; index >= 0; index--)
        {
            MetricDebugEntry entry = entries[index];
            if (!context.Matches(entry.EventName) && !context.Matches(entry.Adapter)
                && !context.Matches(entry.ParametersJson) && !context.Matches(entry.Ignored ? "Ignored" : "Sent"))
                continue;

            visible++;
            DrawEntry(entry);
        }

        if (visible == 0)
            EditorGUILayout.HelpBox("No metrics match the search.", MessageType.Info);
    }

    private void DrawEntry(MetricDebugEntry entry)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        string heading = $"#{entry.Sequence}  {entry.TimestampUtc:HH:mm:ss.fff} UTC";
        bool isExpanded = EditorGUILayout.Foldout(expanded.Contains(entry.Sequence),
            new GUIContent(heading, entry.EventName), true);
        if (isExpanded)
            expanded.Add(entry.Sequence);
        else
            expanded.Remove(entry.Sequence);

        if (GUILayout.Button("Copy", GUILayout.Width(48f)))
            EditorGUIUtility.systemCopyBuffer = string.Join(Environment.NewLine,
                new[] { entry.TimestampUtc.ToString("O"), entry.EventName, entry.Adapter,
                    entry.Ignored ? "Ignored" : "Sent", entry.ParametersJson });
        EditorGUILayout.EndHorizontal();

        DrawSelectableText(entry.EventName);
        EditorGUILayout.LabelField(
            $"{(entry.Ignored ? "Ignored" : "Sent")} · {entry.Adapter}",
            EditorStyles.wordWrappedMiniLabel);

        if (isExpanded)
            DrawSelectableText(entry.ParametersJson);

        EditorGUILayout.EndVertical();
    }

    private static void DrawSelectableText(string text)
    {
        GUIStyle style = EditorStyles.wordWrappedLabel;
        Rect rect = GUILayoutUtility.GetRect(new GUIContent(text), style,
            GUILayout.MinWidth(0f), GUILayout.ExpandWidth(true));
        EditorGUI.SelectableLabel(rect, text, style);
    }
}
