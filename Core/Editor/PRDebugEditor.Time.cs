using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class PRDebugEditor
{
    /// <summary>
    /// Верхняя граница шкалы масштаба времени. Значение выше можно ввести в поле:
    /// шкала задаёт рабочий диапазон, а не предел.
    /// </summary>
    private const float TimeScaleSliderMax = 2f;

    /// <summary>
    /// Верхняя граница шкалы длительности временного изменения, в реальных секундах.
    /// </summary>
    private const float DurationSliderMax = 30f;

    /// <summary>
    /// Вкладка Time: ускорение для проверки (<see cref="PRDebugTime"/>), часы <see cref="PRTime"/> и слои
    /// <see cref="PRTimeScale"/>.
    /// </summary>
    private void DrawTime()
    {
        DrawTabDescription("Всё о течении времени в игре: ускорение для проверки idle-механик, текущие часы и масштаб " +
                           "времени по слоям — замедления, хитстоп, слоу-мо и кто их наложил.");

        DrawSectionHeader("Скорость времени");
        PRDebugTimeSection.Draw(EditorApplication.isPlaying);

        DrawClock();

        DrawTimeScale();
    }

    /// <summary>
    /// Часы <see cref="PRTime"/> и масштаб времени Unity, через который идёт ускорение.
    /// </summary>
    private void DrawClock()
    {
        // HasInstance, а не Instance: Instance создал бы PRTime, если его нет, — окно отладки игру не меняет.
        if (!EditorApplication.isPlaying || !PRUnitySDK.IsInitialized || !PRTime.HasInstance)
            return;

        PRTime time = PRTime.Instance;

        DrawSectionHeader("PRTime");
        DrawKeyValue("Game time", FormatClock(time.GameTime));
        DrawKeyValue("Real time", FormatClock(time.RealTime));
        DrawKeyValue("Game delta", time.GameDeltaTime.ToString("0.0000"));
        DrawKeyValue("Unity timeScale", Time.timeScale.ToString("0.##"));
    }

    private static string FormatClock(float seconds)
    {
        TimeSpan span = TimeSpan.FromSeconds(seconds);

        return $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
    }

    private void DrawTimeScale()
    {
        DrawSectionHeader("PRTimeScale");
        DrawKeyValue("Combine mode", timeScaleCombineMode);
        DrawKeyValue("Event subscribers", timeScaleSubscriberCount);

        using (new EditorGUI.DisabledScope(!PRUnitySDK.IsInitialized || timeScaleRows.Count == 0))
        {
            EditorGUILayout.LabelField("Persistent layer values", EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(hasActiveTemporaryTimeScales))
            {
                foreach (TimeScaleRow row in timeScaleRows.ToArray())
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(row.Layer.Value, GUILayout.MinWidth(65f), GUILayout.MaxWidth(110f));

                    // Slider рисует и шкалу, и поле ввода: значение можно и тянуть мышью,
                    // и вписать точно. Шкала ограничена TimeScaleSliderMax, поле - нет.
                    EditorGUI.BeginChangeCheck();
                    float value = EditorGUILayout.Slider(row.Value, 0f, TimeScaleSliderMax);
                    if (EditorGUI.EndChangeCheck())
                        SetTimeScale(row.Layer, value);

                    GUILayout.Space(6f);
                    EditorGUILayout.LabelField($"Resolved: {row.ResolvedValue:0.###}", EditorStyles.miniLabel,
                        GUILayout.MinWidth(90f));
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Reset persistent values", GUILayout.Width(145f)))
                    ResetTimeScale();
            }

            DrawTimeScaleModifiers();

            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("Temporary global override", EditorStyles.miniBoldLabel);
            temporaryGlobalTimeScale = DrawTimeScaleSlider("Scale", temporaryGlobalTimeScale,
                0f, TimeScaleSliderMax);
            temporaryGlobalDurationSeconds = DrawTimeScaleSlider("Duration (real seconds)",
                temporaryGlobalDurationSeconds, 0f, DurationSliderMax);

            EditorGUILayout.BeginHorizontal();
            DrawTemporaryGlobalTimeScalePreset("0", 0f);
            DrawTemporaryGlobalTimeScalePreset(".25", 0.25f);
            DrawTemporaryGlobalTimeScalePreset(".5", 0.5f);
            DrawTemporaryGlobalTimeScalePreset("1", 1f);
            DrawTemporaryGlobalTimeScalePreset("2", 2f);
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(globalTemporaryTimeScaleActive))
            {
                if (GUILayout.Button(globalTemporaryTimeScaleActive ? "Active" : "Apply", GUILayout.Width(55f)))
                    ApplyTemporaryGlobalTimeScale();
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// Список наложенных модификаторов с их источниками.
    /// <para>
    /// Раньше по значению слоя нельзя было понять, кто именно его изменил, - теперь
    /// видно каждый источник и оставшееся время, и любой можно снять по отдельности.
    /// </para>
    /// </summary>
    private void DrawTimeScaleModifiers()
    {
        if (!PRUnitySDK.IsInitialized)
            return;

        var provider = new PRTimeScaleEnumerations();
        var hasAny = false;

        foreach (Enumeration layer in provider.GetOptions())
        {
            var layerModifiers = PRTimeScale.Instance.GetModifiers(layer);
            if (layerModifiers.Count == 0)
                continue;

            if (!hasAny)
            {
                EditorGUILayout.Space(3f);
                EditorGUILayout.LabelField("Active modifiers", EditorStyles.miniBoldLabel);
                hasAny = true;
            }

            foreach (TimeScaleModifier modifier in layerModifiers.ToArray())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{layer.Value} × {modifier.Value:0.###}",
                    GUILayout.MinWidth(90f), GUILayout.MaxWidth(140f));
                EditorGUILayout.LabelField(modifier.OwnerName, EditorStyles.miniLabel,
                    GUILayout.MinWidth(70f));
                EditorGUILayout.LabelField(GetModifierRemaining(modifier), EditorStyles.miniLabel,
                    GUILayout.Width(70f));

                if (GUILayout.Button("×", GUILayout.Width(22f)))
                {
                    PRTimeScale.Instance.RemoveModifier(
                        new TimeScaleModifierHandle(modifier.Id, layer));
                    RefreshTimeScaleSnapshotDelayed();
                }

                EditorGUILayout.EndHorizontal();
            }
        }
    }

    private static string GetModifierRemaining(TimeScaleModifier modifier)
    {
        if (!modifier.EndRealTime.HasValue)
            return "permanent";

        // Срок модификатора записан по часам PRTime (без пауз и с ускорением для проверки), а не по часам Unity:
        // они расходятся после первой же паузы, и остаток показывался бы «expiring» при живом модификаторе.
        float now = PRTime.HasInstance ? PRTime.Instance.RealTime : Time.realtimeSinceStartup;
        float remaining = modifier.EndRealTime.Value - now;

        return remaining > 0f ? $"{remaining:0.0}s" : "expiring";
    }

    /// <summary>
    /// Строка настройки со шкалой и полем точного ввода.
    /// <para>
    /// Значение можно тянуть мышью в пределах шкалы или вписать в поле. Поле шкалой
    /// не ограничено: если вписать больше максимума, значение сохранится, а ползунок
    /// встанет в крайнее положение - шкала задаёт удобный диапазон, а не предел.
    /// </para>
    /// </summary>
    private static float DrawTimeScaleSlider(string label, float value, float min, float max)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(135f));

        float sliderValue = GUILayout.HorizontalSlider(
            Mathf.Clamp(value, min, max), min, max, GUILayout.MinWidth(80f));

        // Ползунок двигаем только когда он действительно изменился: иначе он бы
        // затирал точное значение, введённое в поле и выходящее за пределы шкалы.
        if (!Mathf.Approximately(sliderValue, Mathf.Clamp(value, min, max)))
            value = sliderValue;

        GUILayout.Space(6f);
        value = EditorGUILayout.FloatField(value, GUILayout.Width(55f));
        EditorGUILayout.EndHorizontal();

        return value;
    }

    private void DrawTemporaryGlobalTimeScalePreset(string label, float value)
    {
        if (GUILayout.Button(label, GUILayout.Width(34f)))
            temporaryGlobalTimeScale = value;
    }

    private void ApplyTemporaryGlobalTimeScale()
    {
        if (float.IsNaN(temporaryGlobalTimeScale) || float.IsInfinity(temporaryGlobalTimeScale)
            || float.IsNaN(temporaryGlobalDurationSeconds) || float.IsInfinity(temporaryGlobalDurationSeconds)
            || temporaryGlobalDurationSeconds <= 0f)
        {
            snapshotError = "Temporary time scale requires finite values and a duration greater than zero.";
            return;
        }

        try
        {
            temporaryGlobalTimeScale = Mathf.Max(0f, temporaryGlobalTimeScale);
            PRTimeScale.Instance.SetGlobalTimeScaleTemporarily(
                temporaryGlobalTimeScale,
                temporaryGlobalDurationSeconds);
            RefreshTimeScaleSnapshotDelayed();
        }
        catch (System.Exception exception)
        {
            snapshotError = $"Temporary time scale failed: {exception.GetType().Name}: {exception.Message}";
        }
    }

    private void SetTimeScale(Enumeration layer, float value)
    {
        if (layer == null || float.IsNaN(value) || float.IsInfinity(value))
        {
            snapshotError = "Time scale must be a finite number.";
            return;
        }

        try
        {
            value = Mathf.Max(0f, value);
            if (layer == PRTimeScaleEnumerations.Global)
                PRTimeScale.Instance.SetGlobalTimeScale(value);
            else
                PRTimeScale.Instance.SetTimeScale(layer, value);

            RefreshTimeScaleSnapshotDelayed();
        }
        catch (System.Exception exception)
        {
            snapshotError = $"Time scale change failed: {exception.GetType().Name}: {exception.Message}";
        }
    }

    private void ResetTimeScale()
    {
        try
        {
            PRTimeScale.Instance.Reset();
            RefreshTimeScaleSnapshotDelayed();
        }
        catch (System.Exception exception)
        {
            snapshotError = $"Time scale reset failed: {exception.GetType().Name}: {exception.Message}";
        }
    }

    private void RefreshTimeScaleSnapshotDelayed()
    {
        EditorApplication.delayCall += () =>
        {
            if (this == null)
                return;

            RefreshSnapshot();
            Repaint();
        };
    }
}
