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
    /// Как часто перерисовывать вкладку Time в игре, секунды: часы на ней живые, а снимок окна — раз в секунду.
    /// </summary>
    private const double TimeTabRepaintInterval = 0.1d;

    private double nextTimeTabRepaint;

    /// <summary>
    /// Вкладка Time: ускорение для проверки (<see cref="PRDebugTime"/>), все часы <see cref="PRTime"/>, причины паузы и
    /// слои <see cref="PRTimeScale"/>.
    /// </summary>
    private void DrawTime()
    {
        DrawTabDescription("Всё о течении времени в игре: ускорение для проверки idle-механик, все часы и их шаг кадра, " +
                           "что сейчас стоит и кто поставил паузу, масштаб времени по слоям — замедления, хитстоп, слоу-мо " +
                           "и кто их наложил. Значения живые. Проверка правил времени целиком — меню PRUnitySDK → Play-тесты → Время.");

        DrawSectionHeader("Скорость времени");
        PRDebugTimeSection.Draw(EditorApplication.isPlaying);

        DrawClock();

        DrawPauseReasons();

        DrawTimeScale();
    }

    /// <summary>
    /// Перерисовывает вкладку Time чаще снимка: часы и шаг кадра на ней меняются каждый кадр.
    /// </summary>
    private void RepaintLiveTimeTab()
    {
        if (!EditorApplication.isPlaying || SelectedTabIndex != 1 || EditorApplication.timeSinceStartup < nextTimeTabRepaint)
            return;

        nextTimeTabRepaint = EditorApplication.timeSinceStartup + TimeTabRepaintInterval;
        Repaint();
    }

    /// <summary>
    /// Все часы SDK: что меряют, шаг кадра и что их останавливает.
    /// </summary>
    private void DrawClock()
    {
        // HasInstance, а не Instance: Instance создал бы PRTime, если его нет, — окно отладки игру не меняет.
        if (!EditorApplication.isPlaying || !PRUnitySDK.IsInitialized || !PRTime.HasInstance)
            return;

        PRTime time = PRTime.Instance;

        DrawSectionHeader("Часы");
        DrawClockHeader();
        DrawClockRow("GameTime", time.GameTime, time.GameDeltaTime, "мир: стоит на любой паузе, темп слоя World");
        DrawClockRow("RealTime", time.RealTime, time.RealDeltaTime, "без масштаба слоёв: стоит на любой паузе");
        DrawClockRow("UiTime", time.UiTime, time.UiDeltaTime, "окна и обучение: идёт на логической паузе, стоит на паузе проекта");
        DrawClockRow("WallTime", PRTime.WallTime, null, "настоящее время: идёт всегда (реклама, сеть, лимиты)");

        // Часы масок, которые кто-то уже спросил: игрок, боты, питомцы, интерфейс в мире. Окно их не заводит — иначе
        // само создало бы часы, которых в игре нет. Свои у каждой маски: сравнивать только с ними же.
        foreach (KeyValuePair<TimeLayer, float> clock in time.LayerClocks.ToArray())
        {
            DrawClockRow($"GetTime({clock.Key})", clock.Value, time.GetDeltaTime(clock.Key),
                $"часы маски: темп × {PRTimeScale.Instance.Resolve(clock.Key):0.###}, стоит на любой паузе");
        }

        EditorGUILayout.Space(3f);
        DrawKeyValue("Fixed: game / real", $"{time.GameFixedDeltaTime:0.0000} / {time.RealFixedDeltaTime:0.0000}");
        DrawKeyValue("Seconds: game / real", $"{time.CurrentGameSecond} / {time.CurrentRealSecond}");
        DrawKeyValue("Unity timeScale", Time.timeScale.ToString("0.##"));

        if (PRUnitySDK.ServerTime != null)
            DrawKeyValue("ServerTime (таймеры)", PRUnitySDK.ServerTime.GetNow().ToLocalTime().ToString("dd.MM HH:mm:ss"));
    }

    private static void DrawClockHeader()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Часы", EditorStyles.miniBoldLabel, GUILayout.Width(150f));
        EditorGUILayout.LabelField("Значение", EditorStyles.miniBoldLabel, GUILayout.Width(80f));
        EditorGUILayout.LabelField("Шаг кадра", EditorStyles.miniBoldLabel, GUILayout.Width(70f));
        EditorGUILayout.LabelField("Когда идёт", EditorStyles.miniBoldLabel, GUILayout.MinWidth(80f));
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawClockRow(string name, float value, float? delta, string note)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(name, GUILayout.Width(150f));
        EditorGUILayout.LabelField(FormatClock(value), GUILayout.Width(80f));

        // Нулевой шаг — часы сейчас стоят: так видно паузу и заморозку слоя, не читая причины.
        GUIStyle style = delta.HasValue && delta.Value <= 0f ? StoppedStyle : EditorStyles.label;
        EditorGUILayout.LabelField(delta.HasValue ? delta.Value.ToString("0.0000") : "—", style, GUILayout.Width(70f));
        EditorGUILayout.LabelField(note, EditorStyles.miniLabel, GUILayout.MinWidth(80f));
        EditorGUILayout.EndHorizontal();
    }

    private static GUIStyle stoppedStyle;

    /// <summary>
    /// Выделение того, что сейчас стоит: нулевой шаг часов, включённая пауза.
    /// </summary>
    private static GUIStyle StoppedStyle => stoppedStyle ??= new GUIStyle(EditorStyles.label)
    {
        normal = { textColor = new Color(0.95f, 0.6f, 0.25f) },
        fontStyle = FontStyle.Bold,
    };

    /// <summary>
    /// Что сейчас стоит и почему: каждая пауза, кто её поставил и что она останавливает.
    /// </summary>
    /// <remarks>
    /// Пауза — набор флагов, у каждого свой источник (<see cref="PauseManager"/> помнит последнего, кто флаг поставил).
    /// Сводка сверху отвечает на главный вопрос «почему мир или интерфейс стоит», ниже — по флагу.
    /// </remarks>
    private void DrawPauseReasons()
    {
        if (!EditorApplication.isPlaying || !PRUnitySDK.IsInitialized || PRUnitySDK.PauseManager == null)
            return;

        IPauseManager pause = PRUnitySDK.PauseManager;
        var manager = pause as PauseManager;

        // Флаги по отдельности: IsProjectPaused включает фокус, IsLogicPaused — всё сразу.
        bool project = manager != null ? manager.ProjectPausedBy != null : pause.IsProjectPaused && !pause.IsFocusPaused;
        bool logic = manager != null ? manager.LogicPausedBy != null : pause.IsLogicPaused;

        var world = new List<string>();
        var ui = new List<string>();
        AddReason(project, "проект", world, ui);
        AddReason(pause.IsFocusPaused, "нет фокуса", world, ui);
        AddReason(logic, "логика", world, null);
        AddReason(pause.IsTutorialPaused, "обучение", world, null);
        AddReason(pause.IsCutScenePaused, "катсцена", world, null);

        DrawSectionHeader("Пауза");
        DrawKeyValue("Мир стоит", pause.IsLogicPaused ? "да — " + (world.Count > 0 ? string.Join(", ", world) : "причина не записана") : "нет");
        DrawKeyValue("Интерфейс стоит", pause.IsProjectPaused ? "да — " + string.Join(", ", ui) : "нет");
        bool music = manager != null ? manager.MusicPausedBy != null : pause.IsMusicPaused && !pause.IsProjectPaused;
        AddReason(music, "музыка", ui, null);
        DrawKeyValue("Музыка стоит", pause.IsMusicPaused ? "да — " + string.Join(", ", ui) : "нет");

        EditorGUILayout.Space(3f);
        DrawPauseRow("Project", project, manager?.ProjectPausedBy, "мир и интерфейс; реклама, покупка");
        DrawPauseRow("Focus", pause.IsFocusPaused, manager?.FocusPausedBy, "мир и интерфейс; окно без фокуса");
        DrawPauseRow("Logic", logic, manager?.LogicPausedBy, "мир; окна, награды");
        DrawPauseRow("Tutorial", pause.IsTutorialPaused, manager?.TutorialPausedBy, "мир; шаг обучения");
        DrawPauseRow("Cutscene", pause.IsCutScenePaused, manager?.CutScenePausedBy, "мир; катсцена");
        DrawPauseRow("Music", music, manager?.MusicPausedBy, "только музыка");
    }

    private static void AddReason(bool active, string kind, List<string> world, List<string> ui)
    {
        if (!active)
            return;

        // В сводке — только вид паузы: источники стоят в строках ниже, а длинное имя обрезалось бы.
        world.Add(kind);
        ui?.Add(kind);
    }

    private static void DrawPauseRow(string name, bool active, object executer, string stops)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(name, GUILayout.Width(70f));
        EditorGUILayout.LabelField(active ? "ON" : "—", active ? StoppedStyle : EditorStyles.label, GUILayout.Width(35f));
        EditorGUILayout.LabelField(active ? DescribeExecuter(executer) : string.Empty, GUILayout.MinWidth(120f));
        EditorGUILayout.LabelField(stops, EditorStyles.miniLabel, GUILayout.MinWidth(80f));
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Кто поставил паузу: тип и имя объекта сцены, тип — для статических вызовов, иначе описание источника.
    /// </summary>
    private static string DescribeExecuter(object executer)
    {
        switch (executer)
        {
            case null:
                return "не записан";
            case UnityEngine.Object unityObject:
                return unityObject != null ? $"{unityObject.GetType().Name} «{unityObject.name}»" : $"{unityObject.GetType().Name} (уничтожен)";
            case Type type:
                return type.Name;
        }

        string text = executer.ToString();
        return text == executer.GetType().ToString() ? executer.GetType().Name : text;
    }

    private static string FormatClock(float seconds)
    {
        TimeSpan span = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));

        return $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}.{span.Milliseconds / 100}";
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
