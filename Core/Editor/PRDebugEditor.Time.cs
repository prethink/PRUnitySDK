using System;
using System.Collections.Generic;
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
    /// Верхняя граница шкалы длительности модификатора, в секундах <see cref="PRTime.RealTime"/>.
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

    /// <summary>
    /// Масштаб времени по слоям, наложенные модификаторы и форма нового.
    /// </summary>
    private void DrawTimeScale()
    {
        DrawSectionHeader("PRTimeScale");
        DrawKeyValue("Event subscribers", timeScaleSubscriberCount);

        using (new EditorGUI.DisabledScope(!PRUnitySDK.IsInitialized))
        {
            DrawTimeScaleLayers();
            DrawTimeScaleModifiers();
            DrawModifierForm();
        }
    }

    /// <summary>
    /// Итоговый масштаб каждого слоя и во сколько раз слой идёт быстрее или медленнее мира.
    /// </summary>
    /// <remarks>
    /// Отношение к миру — то, что драйверы добавляют телам и аниматорам поверх шага физики: при замедлении
    /// «всего, кроме игрока» у Player оно больше единицы.
    /// </remarks>
    private void DrawTimeScaleLayers()
    {
        EditorGUILayout.LabelField("Layers", EditorStyles.miniBoldLabel);

        foreach (TimeScaleRow row in timeScaleRows.ToArray())
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(row.Layer.ToString(), GUILayout.MinWidth(65f), GUILayout.MaxWidth(110f));
            EditorGUILayout.LabelField($"× {row.Value:0.###}", GUILayout.Width(70f));
            if (row.Layer != TimeLayer.World)
                EditorGUILayout.LabelField($"to world × {row.RelativeToWorld:0.###}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// Список наложенных модификаторов: кого задевают, кто наложил, сколько осталось.
    /// </summary>
    private void DrawTimeScaleModifiers()
    {
        if (!PRUnitySDK.IsInitialized)
            return;

        IReadOnlyList<TimeScaleModifier> modifiers = PRTimeScale.Instance.Modifiers;
        if (modifiers.Count == 0)
            return;

        EditorGUILayout.Space(3f);
        EditorGUILayout.LabelField("Active modifiers", EditorStyles.miniBoldLabel);

        foreach (TimeScaleModifier modifier in modifiers.ToArray())
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"× {modifier.Value:0.###}", GUILayout.Width(60f));
            EditorGUILayout.LabelField(DescribeTargets(modifier), GUILayout.MinWidth(120f));
            EditorGUILayout.LabelField(modifier.OwnerName, EditorStyles.miniLabel, GUILayout.MinWidth(70f));
            EditorGUILayout.LabelField(GetModifierRemaining(modifier), EditorStyles.miniLabel, GUILayout.Width(70f));

            if (GUILayout.Button("×", GUILayout.Width(22f)))
            {
                PRTimeScale.Instance.RemoveModifier(new TimeScaleModifierHandle(modifier.Id));
                RefreshTimeScaleSnapshotDelayed();
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Remove all", GUILayout.Width(90f)))
            ResetTimeScale();
    }

    /// <summary>
    /// Кого задевает модификатор: «Global except Player, UI».
    /// </summary>
    private static string DescribeTargets(TimeScaleModifier modifier)
    {
        return modifier.Except == TimeLayer.None
            ? modifier.Affects.ToString()
            : $"{modifier.Affects} except {modifier.Except}";
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

    /// <summary>
    /// Форма нового модификатора: множитель, кого задеть, кого не трогать, срок.
    /// </summary>
    private void DrawModifierForm()
    {
        EditorGUILayout.Space(3f);
        EditorGUILayout.LabelField("Add modifier", EditorStyles.miniBoldLabel);

        modifierScale = DrawTimeScaleSlider("Scale", modifierScale, 0f, TimeScaleSliderMax);
        modifierAffects = (TimeLayer)EditorGUILayout.EnumFlagsField("Affects", modifierAffects);
        modifierExcept = (TimeLayer)EditorGUILayout.EnumFlagsField("Except", modifierExcept);
        modifierDurationSeconds = DrawTimeScaleSlider("Duration, s (0 = ∞)", modifierDurationSeconds, 0f, DurationSliderMax);

        EditorGUILayout.BeginHorizontal();
        // Кронос из Crash Bandicoot 4: мир и враги медленно, игрок и интерфейс — в обычном темпе.
        DrawModifierPreset("Kronos", 0.3f, TimeLayer.Global, TimeLayer.Player | TimeLayer.UI, 5f);
        DrawModifierPreset("Slow all", 0.5f, TimeLayer.Global, TimeLayer.None, 5f);
        DrawModifierPreset("Freeze NPC", 0f, TimeLayer.NPC | TimeLayer.Enemy, TimeLayer.None, 3f);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Apply", GUILayout.Width(55f)))
            ApplyModifier();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawModifierPreset(string label, float scale, TimeLayer affects, TimeLayer except, float duration)
    {
        if (!GUILayout.Button(label, EditorStyles.miniButton))
            return;

        modifierScale = scale;
        modifierAffects = affects;
        modifierExcept = except;
        modifierDurationSeconds = duration;
    }

    /// <summary>
    /// Источник модификаторов, наложенных из окна отладки: так они видны в списке под своим именем.
    /// </summary>
    private sealed class DebugWindow
    {
    }

    private static readonly DebugWindow DebugModifierOwner = new();

    private void ApplyModifier()
    {
        if (float.IsNaN(modifierScale) || float.IsInfinity(modifierScale)
            || float.IsNaN(modifierDurationSeconds) || float.IsInfinity(modifierDurationSeconds))
        {
            snapshotError = "Time scale modifier requires finite values.";
            return;
        }

        try
        {
            PRTimeScale.Instance.AddModifier(Mathf.Max(0f, modifierScale), modifierAffects, modifierExcept,
                DebugModifierOwner, Mathf.Max(0f, modifierDurationSeconds));
            RefreshTimeScaleSnapshotDelayed();
        }
        catch (Exception exception)
        {
            snapshotError = $"Time scale modifier failed: {exception.GetType().Name}: {exception.Message}";
        }
    }

    private void ResetTimeScale()
    {
        try
        {
            PRTimeScale.Instance.Reset();
            RefreshTimeScaleSnapshotDelayed();
        }
        catch (Exception exception)
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
