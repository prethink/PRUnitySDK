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

    private void DrawOverview()
    {
        DrawSectionHeader("Runtime");
        DrawKeyValue("SDK initialized", PRUnitySDK.IsInitialized);
        DrawKeyValue("Editor paused", EditorApplication.isPaused);
        DrawKeyValue("Last snapshot", lastRefreshUtc == default ? "-" : lastRefreshUtc.ToLocalTime().ToString("T"));

        DrawSectionHeader("Pause");
        using (new EditorGUI.DisabledScope(true))
            DrawToggleGrid(("Project", pause.Project), ("Logic", pause.Logic), ("Focus", pause.Focus),
                ("Music", pause.Music), ("Tutorial", pause.Tutorial), ("Cutscene", pause.Cutscene));

        DrawLanguage();

        DrawTimeScale();

        DrawSaveInfo();

        DrawSectionHeader("Summary");
        DrawSummaryLine(("Players", players.Count), ("Humans", humanCount), ("AI", aiCount),
            ("Initialized", initializationEntries.Count), ("Entities", entityTotal), ("On scene", entityOnScene),
            ("In pool", entityInPool), ("Pools", pools.Count),
            ("Errors", problems.Count(problem => problem.Severity == PRDebugProblemSeverity.Error)),
            ("Warnings", problems.Count(problem => problem.Severity == PRDebugProblemSeverity.Warning)));
    }

    /// <summary>
    /// Показывает активный runtime-язык и позволяет переключить его через текущую
    /// реализацию <see cref="ILanguageManager"/>.
    /// </summary>
    private void DrawLanguage()
    {
        DrawSectionHeader("Language");

        ILanguageManager languageManager = PRUnitySDK.LanguageManager;
        if (!PRUnitySDK.IsInitialized || languageManager == null)
        {
            EditorGUILayout.HelpBox("LanguageManager is not initialized.", MessageType.Info);
            return;
        }

        string currentCode = languageManager.GetCurrentLang();
        LangType currentLanguage = LocalizationUtils.GetLanguageEnum(currentCode);

        DrawKeyValue("Code", string.IsNullOrWhiteSpace(currentCode) ? "-" : currentCode);
        DrawKeyValue("Manager", languageManager.GetType().Name);

        EditorGUI.BeginChangeCheck();
        LangType selectedLanguage = (LangType)EditorGUILayout.EnumPopup("Runtime language", currentLanguage);
        if (EditorGUI.EndChangeCheck() && selectedLanguage != currentLanguage)
            EditorApplication.delayCall += () => ApplyLanguage(selectedLanguage);
    }

    /// <summary>
    /// Переключает язык через зарегистрированный SDK-менеджер, чтобы Debug-окно
    /// не обходило платформенную реализацию локализации.
    /// </summary>
    private void ApplyLanguage(LangType language)
    {
        if (this == null)
            return;

        try
        {
            ILanguageManager languageManager = PRUnitySDK.LanguageManager;
            if (languageManager == null)
            {
                snapshotError = "LanguageManager is not initialized.";
                return;
            }

            string languageCode = LocalizationUtils.GetLanguageCode(language);
            languageManager.SwitchLang(languageCode);

            string appliedCode = languageManager.GetCurrentLang();
            if (!string.Equals(appliedCode, languageCode, System.StringComparison.OrdinalIgnoreCase))
            {
                snapshotError =
                    $"{languageManager.GetType().Name} rejected language '{languageCode}' (current: '{appliedCode}').";
            }
            else
            {
                snapshotError = null;
            }

            RefreshSnapshot();
            Repaint();
        }
        catch (System.Exception exception)
        {
            snapshotError = $"Language change failed: {exception.GetType().Name}: {exception.Message}";
        }
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

        var provider = new PRTimeScaleEnumerationProvider();
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

        float remaining = modifier.EndRealTime.Value - Time.realtimeSinceStartup;

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
            if (layer == PRTimeScaleEnumerationProvider.Global)
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

    private void DrawSaveInfo()
    {
        DrawSectionHeader("Save Info");
        DrawKeyValue("Loaded existing save", hasLoadedSave);
        DrawKeyValue("State", saveState.ToString());
        DrawKeyValue("Created", saveCreationTimeUtc?.ToLocalTime().ToString("G") ?? "Unknown");
        DrawKeyValue("Last updated", lastSaveTimeUtc?.ToLocalTime().ToString("G") ?? "Never");
        DrawKeyValue("Cooldown", saveCooldownRemainingSeconds > 0
            ? $"{saveCooldownRemainingSeconds}s remaining"
            : "Ready");

        using (new EditorGUI.DisabledScope(!canStartSave))
        {
            if (GUILayout.Button("Save", GUILayout.Width(90f)))
            {
                PRUnitySDK.Managers.Game.StartSaveTask();
                RefreshSnapshot();
            }
        }
    }

    private void DrawProblems()
    {
        int errorCount = problems.Count(problem => problem.Severity == PRDebugProblemSeverity.Error);
        int warningCount = problems.Count(problem => problem.Severity == PRDebugProblemSeverity.Warning);
        int infoCount = problems.Count(problem => problem.Severity == PRDebugProblemSeverity.Info);

        DrawSectionHeader($"Health check — {errorCount} errors, {warningCount} warnings, {infoCount} info");
        DrawFixedRow(true, ("Severity", 65), ("Category", 95), ("Code", 145),
            ("Message", 420), ("Object", 60), ("Source", 60));

        int count = 0;
        foreach (PRDebugProblem problem in problems)
        {
            string targetName = SafeValue(() => problem.Target == null ? null : problem.Target.name, null);
            if (!MatchesSearch(problem.Severity, problem.Category, problem.Code, problem.Message,
                    problem.SourceType?.FullName, targetName))
                continue;

            count++;
            EditorGUILayout.BeginHorizontal();
            Color previousColor = GUI.color;
            GUI.color = ProblemColor(problem.Severity);
            Label(problem.Severity, 65);
            GUI.color = previousColor;
            Label(problem.Category, 95);
            Label(problem.Code, 145);
            Label(problem.Message, 420);
            DrawObjectButton(problem.Target);
            using (new EditorGUI.DisabledScope(problem.SourceType == null))
                DrawScriptButton(problem.SourceType, null);
            EditorGUILayout.EndHorizontal();
        }

        DrawEmpty(count, problems.Count == 0
            ? "No problems were found."
            : "No problems match the current search.");
    }

    private void DrawInitialization()
    {
        DrawSectionHeader("PRUnitySDK initialization");
        DrawKeyValue("Tracked total", $"{initializationTotalMilliseconds:F2} ms");

        DrawInitializationTable(PRInitializationCategory.Module, "Modules");
        DrawInitializationTable(PRInitializationCategory.Manager, "Managers");
        DrawInitializationTable(PRInitializationCategory.Singleton, "Singletons");
        DrawInitializationTable(PRInitializationCategory.Factory, "Factories");
        DrawInitializationTable(PRInitializationCategory.MonoWindow, "MonoWindows");
        DrawInitializationTable(PRInitializationCategory.Notifier, "Notifiers");
        DrawInitializationTable(PRInitializationCategory.Type, "Other initialized types");
    }

    private void DrawMonoWindows()
    {
        DrawSectionHeader($"MonoWindows runtime ({monoWindows.Count})");
        DrawFixedRow(true, ("Implementation", 230), ("Key", 180), ("Visible", 55),
            ("Active", 50), ("Current", 55), ("Object", 60), ("Source", 60), ("Action", 65));

        int count = 0;
        foreach (MonoWindowRow row in monoWindows.ToArray())
        {
            if (!MatchesSearch(row.Type?.FullName, row.Key, row.Visible, row.Active, row.Current))
                continue;

            count++;
            EditorGUILayout.BeginHorizontal();
            Label(row.Type?.FullName ?? "<unknown>", 230);
            Label(row.Key, 180);
            Label(row.Visible, 55);
            Label(row.Active, 50);
            Label(row.Current, 55);
            DrawObjectButton(row.GameObject);
            DrawScriptButton(row.Type, null);
            DrawMonoWindowAction(row);
            EditorGUILayout.EndHorizontal();
        }

        DrawEmpty(count, "No MonoWindows match the current search.");
        EditorGUILayout.HelpBox(
            "Open uses MonoWindowArgsEmpty. Windows that require typed args should be opened through their normal game flow.",
            MessageType.None);
    }

    private void DrawGameRules()
    {
        DrawSectionHeader($"Stat rules ({statRules.Count})");

        if (!GameRules.IsInitialized)
        {
            EditorGUILayout.HelpBox("GameRules are not initialized yet.", MessageType.Info);
            return;
        }

        DrawRuleTester();

        DrawFixedRow(true, ("Stat", 190), ("#", 30), ("Rule", 190), ("Priority", 60),
            ("Parameters", 240), ("Source", 60));

        int count = 0;
        string previousStat = null;

        foreach (StatRuleRow row in statRules.ToArray())
        {
            if (!MatchesSearch(row.StatName, row.RuleType?.Name, row.Parameters))
                continue;

            count++;

            // Имя характеристики повторяется в каждой строке, но у второго и следующих
            // правил одного стата гасится: пустая ячейка читалась как «правило без стата».
            bool sameStat = row.StatName == previousStat;
            previousStat = row.StatName;

            EditorGUILayout.BeginHorizontal();
            DrawStatName(row.StatName, sameStat, 190);
            Label(row.Order + 1, 30);
            Label(row.RuleType?.Name ?? "<unknown>", 190);
            Label(row.Priority, 60);
            Label(row.Parameters, 240);
            DrawScriptButton(row.RuleType, null);
            EditorGUILayout.EndHorizontal();
        }

        DrawEmpty(count, "No stat rules match the current search.");
        EditorGUILayout.HelpBox(
            "Rules are applied in the listed order: lower Priority first, declaration order breaks ties.",
            MessageType.None);
    }

    /// <summary>
    /// Рисует имя характеристики: у продолжений группы - приглушённым стилем,
    /// чтобы правила одного стата читались как блок, но строка не выглядела безымянной.
    /// </summary>
    private static void DrawStatName(string statName, bool isContinuation, float width)
    {
        if (!isContinuation)
        {
            Label(statName, width);
            return;
        }

        EditorGUILayout.LabelField(statName, EditorStyles.centeredGreyMiniLabel,
            GUILayout.Width(width));
    }

    /// <summary>
    /// Прогоняет введённое значение через правила выбранной характеристики.
    /// Отвечает на вопрос «почему значение обрезано», не требуя запуска геймплея.
    /// </summary>
    private void DrawRuleTester()
    {
        string[] stats = statRules
            .Select(row => row.StatName)
            .Distinct()
            .ToArray();

        if (stats.Length == 0)
            return;

        ruleTestStatIndex = Mathf.Clamp(ruleTestStatIndex, 0, stats.Length - 1);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Apply rules to value", EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();
        ruleTestStatIndex = EditorGUILayout.Popup(ruleTestStatIndex, stats, GUILayout.Width(190f));
        ruleTestValue = EditorGUILayout.FloatField(ruleTestValue, GUILayout.Width(90f));

        Enumeration stat = statRules.FirstOrDefault(row => row.StatName == stats[ruleTestStatIndex])?.Stat;
        float result = stat == null
            ? ruleTestValue
            : SafeValue(() => GameRules.ApplyStatRules(stat, ruleTestValue), ruleTestValue);

        bool changed = !Mathf.Approximately(result, ruleTestValue);

        Color previous = GUI.contentColor;
        GUI.contentColor = changed ? new Color(0.9f, 0.8f, 0.4f) : previous;
        EditorGUILayout.LabelField($"→  {result}", GUILayout.Width(140f));
        GUI.contentColor = previous;

        if (changed)
            EditorGUILayout.LabelField("value was clamped", EditorStyles.miniLabel);

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawBackgroundTasks()
    {
        DrawSectionHeader($"Background tasks ({backgroundTasks.Count})");
        DrawFixedRow(true, ("Key", 150), ("Type", 170), ("Status", 105), ("Every", 60),
            ("Next", 60), ("Last", 60), ("Run", 45), ("Skip", 45), ("Err", 40),
            ("ms", 50), ("Value", 110), ("Object", 60), ("Source", 60), ("Actions", 150));

        int count = 0;
        foreach (BackgroundTaskRow row in backgroundTasks.ToArray())
        {
            if (!MatchesSearch(row.Key, row.Name, row.Type?.FullName, row.Status, row.WatchedValue))
                continue;

            count++;
            EditorGUILayout.BeginHorizontal();
            Label(row.Key, 150);
            Label(row.Type?.Name ?? "<unknown>", 170);
            DrawTaskStatus(row, 105);
            Label(FormatSeconds(row.RepeatSeconds), 60);
            Label(FormatCountdown(row), 60);
            Label(row.SecondsSinceLastRun < 0f ? "-" : FormatSeconds(row.SecondsSinceLastRun), 60);
            Label(row.ExecutedCount, 45);
            Label(row.SkippedCount, 45);
            Label(row.ErrorCount, 40);
            Label(row.LastRunDurationMs.ToString("F1"), 50);
            Label(row.WatchedValue ?? "-", 110);
            DrawObjectButton(row.Component == null ? null : row.Component.gameObject);
            DrawScriptButton(row.Type, null);
            DrawBackgroundTaskActions(row);
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(row.LastError))
                EditorGUILayout.HelpBox($"{row.Key}: {row.LastError}", MessageType.Warning);
        }

        DrawEmpty(count, "No background tasks are registered.");
        EditorGUILayout.HelpBox(
            "Run now ignores CanExecute(). Tasks marked GameTime follow the logic pause, so their countdown freezes with the game.",
            MessageType.None);
    }

    /// <summary>
    /// Красит статус так, чтобы проблемные состояния были видны без чтения таблицы.
    /// </summary>
    private static void DrawTaskStatus(BackgroundTaskRow row, float width)
    {
        Color color = row.Status switch
        {
            BackgroundTaskStatus.Faulted => new Color(0.9f, 0.4f, 0.35f),
            BackgroundTaskStatus.Completed => new Color(0.55f, 0.75f, 0.95f),
            BackgroundTaskStatus.Paused => new Color(0.9f, 0.8f, 0.4f),
            BackgroundTaskStatus.Skipped => new Color(0.75f, 0.75f, 0.75f),
            _ => GUI.contentColor
        };

        Color previous = GUI.contentColor;
        GUI.contentColor = color;

        string suffix = row.ConsecutiveErrors > 0 && row.Status != BackgroundTaskStatus.Faulted
            ? $" ({row.ConsecutiveErrors})"
            : string.Empty;

        Label($"{row.Status}{suffix}", width);
        GUI.contentColor = previous;
    }

    private void DrawBackgroundTaskActions(BackgroundTaskRow row)
    {
        using (new EditorGUI.DisabledScope(row.Task == null))
        {
            if (GUILayout.Button("Run", GUILayout.Width(40f)))
                EditorApplication.delayCall += () => ExecuteBackgroundTaskAction(row, TaskDebugAction.Run);

            bool paused = row.Status == BackgroundTaskStatus.Paused;
            using (new EditorGUI.DisabledScope(row.Status == BackgroundTaskStatus.Faulted ||
                                               row.Status == BackgroundTaskStatus.Completed))
            {
                if (GUILayout.Button(paused ? "Resume" : "Pause", GUILayout.Width(60f)))
                {
                    TaskDebugAction action = paused ? TaskDebugAction.Resume : TaskDebugAction.Pause;
                    EditorApplication.delayCall += () => ExecuteBackgroundTaskAction(row, action);
                }
            }

            using (new EditorGUI.DisabledScope(row.Status != BackgroundTaskStatus.Faulted &&
                                               row.Status != BackgroundTaskStatus.Completed))
            {
                if (GUILayout.Button("Reset", GUILayout.Width(45f)))
                    EditorApplication.delayCall += () => ExecuteBackgroundTaskAction(row, TaskDebugAction.Reset);
            }
        }
    }

    private enum TaskDebugAction
    {
        Run,
        Pause,
        Resume,
        Reset
    }

    private void ExecuteBackgroundTaskAction(BackgroundTaskRow row, TaskDebugAction action)
    {
        if (this == null || row?.Task == null)
            return;

        try
        {
            switch (action)
            {
                case TaskDebugAction.Run:
                    PRUnitySDK.Trackers.BackgroundTasks.ForceExecute(row.Task.Key);
                    break;
                case TaskDebugAction.Pause:
                    row.Task.Runtime.Pause();
                    break;
                case TaskDebugAction.Resume:
                    row.Task.Runtime.Resume();
                    break;
                case TaskDebugAction.Reset:
                    row.Task.Runtime.ResetFault();
                    row.Task.Runtime.ResetRepeatCount();
                    break;
            }

            RefreshSnapshot();
            Repaint();
        }
        catch (System.Exception exception)
        {
            snapshotError = $"Background task action failed: {exception.GetType().Name}: {exception.Message}";
        }
    }

    /// <summary>
    /// Форматирует обратный отсчёт: для остановленных задач его показывать незачем.
    /// </summary>
    private static string FormatCountdown(BackgroundTaskRow row)
    {
        if (row.Status == BackgroundTaskStatus.Faulted ||
            row.Status == BackgroundTaskStatus.Completed ||
            row.Status == BackgroundTaskStatus.Paused)
        {
            return "-";
        }

        return row.SecondsToNextRun <= 0f ? "now" : FormatSeconds(row.SecondsToNextRun);
    }

    private static string FormatSeconds(float seconds)
    {
        if (seconds <= 0f)
            return "0s";

        if (seconds < 60f)
            return $"{seconds:F1}s";

        return seconds < 3600f
            ? $"{seconds / 60f:F1}m"
            : $"{seconds / 3600f:F1}h";
    }

    private void DrawMonoWindowAction(MonoWindowRow row)
    {
        using (new EditorGUI.DisabledScope(row.Window == null || row.Key == "<null>"))
        {
            string label = row.Visible ? "Close" : "Open";
            if (!GUILayout.Button(label, GUILayout.Width(60f)))
                return;
        }

        EditorApplication.delayCall += () => ExecuteMonoWindowAction(row);
    }

    private void ExecuteMonoWindowAction(MonoWindowRow row)
    {
        if (this == null || row?.Window == null)
            return;

        try
        {
            if (row.Window.IsVisible)
                row.Window.Hide(isForceClose: true);
            else
                PRUnitySDK.Trackers.MonoWindows.TryShowWindow(row.Window.Key, new MonoWindowArgsEmpty());

            RefreshSnapshot();
            Repaint();
        }
        catch (System.Exception exception)
        {
            snapshotError = $"MonoWindow action failed: {exception.GetType().Name}: {exception.Message}";
        }
    }

    private void DrawEvents()
    {
        DrawSectionHeader($"EventBus monitor — {aggregatedEventRows.Count} aggregated, " +
                          $"latest {eventRows.Count}/{EventHistoryCapacity}");
        EditorGUILayout.BeginHorizontal();
        captureEvents = GUILayout.Toggle(captureEvents, "Capture", EditorStyles.toolbarButton, GUILayout.Width(70f));
        if (GUILayout.Button("Clear", GUILayout.Width(55f)))
            ClearEventHistory();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        DrawSectionHeader("High-frequency events");
        DrawFixedRow(true, ("Event interface", 310), ("Calls", 75), ("Average/s", 75),
            ("Subscribers", 80), ("Last", 90), ("Source", 60));

        int aggregatedCount = 0;
        foreach (AggregatedEventBusRow row in aggregatedEventRows)
        {
            if (!MatchesSearch(row.EventType?.FullName, row.Count, row.SubscriberCount))
                continue;

            aggregatedCount++;
            EditorGUILayout.BeginHorizontal();
            Label(row.EventType?.FullName ?? "<unknown>", 310);
            Label(row.Count, 75);
            Label($"{row.AverageCallsPerSecond:F1}", 75);
            Label(row.SubscriberCount, 80);
            Label(row.LastTimestampUtc.ToLocalTime().ToString("HH:mm:ss.fff"), 90);
            DrawScriptButton(row.EventType, null);
            EditorGUILayout.EndHorizontal();
        }

        DrawEmpty(aggregatedCount, captureEvents
            ? "No high-frequency EventBus events have been captured yet."
            : "EventBus capture is paused.");

        DrawSectionHeader("Recent events");
        DrawFixedRow(true, ("#", 55), ("Time", 90), ("Event interface", 360),
            ("Subscribers", 80), ("Source", 60));

        int count = 0;
        for (int index = eventRows.Count - 1; index >= 0; index--)
        {
            EventBusRow row = eventRows[index];
            if (!MatchesSearch(row.Sequence, row.EventType?.FullName, row.SubscriberCount))
                continue;

            count++;
            EditorGUILayout.BeginHorizontal();
            Label(row.Sequence, 55);
            Label(row.TimestampUtc.ToLocalTime().ToString("HH:mm:ss.fff"), 90);
            Label(row.EventType?.FullName ?? "<unknown>", 360);
            Label(row.SubscriberCount, 80);
            DrawScriptButton(row.EventType, null);
            EditorGUILayout.EndHorizontal();
        }

        DrawEmpty(count, captureEvents
            ? "No regular EventBus events have been captured yet."
            : "EventBus capture is paused.");
        EditorGUILayout.HelpBox(
            "IOnUpdateEvent and IOnPRUpdateEvent are aggregated above and do not occupy the recent-events ring buffer. " +
            "The monitor records only while this Debug window is open. Payload is not captured.",
            MessageType.None);
    }

    private void DrawInitializationTable(PRInitializationCategory category, string title)
    {
        int totalCount = 0;
        int visibleCount = 0;
        double totalMilliseconds = 0d;

        foreach (var row in initializationEntries)
        {
            if (row.Category != category)
                continue;

            totalCount++;
            totalMilliseconds += row.DurationMilliseconds;
        }

        if (totalCount == 0)
            return;

        DrawSectionHeader($"{title} ({totalCount}) — {totalMilliseconds:F2} ms");
        DrawFixedRow(true, ("Implementation", 280), ("Contract", 250),
            ("Time", 75), ("Source", 60));

        foreach (var row in initializationEntries)
        {
            if (row.Category != category)
                continue;

            if (!MatchesSearch(row.Category, row.Name, row.ContractType, row.ImplementationType))
                continue;

            visibleCount++;
            EditorGUILayout.BeginHorizontal();
            Label(row.ImplementationType, 280);
            Label(row.ContractType, 250);
            Label($"{row.DurationMilliseconds:F2} ms", 75);
            DrawScriptButton(row.ImplementationTypeReference, row.ContractTypeReference);
            EditorGUILayout.EndHorizontal();
        }

        DrawEmpty(visibleCount, $"No {title.ToLowerInvariant()} match the current search.");
    }

    private void DrawPlayers()
    {
        DrawSectionHeader("Players");
        DrawFixedRow(true, ("Player ID", 70), ("Entity ID", 70), ("Type", 65), ("Name", 140),
            ("Team", 90), ("Ready", 55), ("Points", 65), ("K/D", 55), ("Object", 60));

        int count = 0;
        foreach (var row in players)
        {
            if (!MatchesSearch(row.PlayerId, row.EntityId, row.Type, row.Name, row.Team))
                continue;

            count++;
            EditorGUILayout.BeginHorizontal();
            Label(row.PlayerId, 70); Label(row.EntityId, 70); Label(row.Type, 65); Label(row.Name, 140);
            Label(row.Team, 90); Label(row.Ready?.ToString() ?? "-", 55); Label(row.Points, 65);
            Label($"{row.Kills}/{row.Deaths}", 55); DrawObjectButton(row.GameObject);
            EditorGUILayout.EndHorizontal();
        }
        DrawEmpty(count, "No players match the current search.");
    }

    private void DrawEntities()
    {
        // Оба списка длинные и нужны одновременно: тип выбирают в верхнем, а смотрят
        // на экземпляры в нижнем. Общая прокрутка увела бы один из них за край экрана,
        // поэтому у каждого своя, и высота делится поровну.
        float half = ResolveEntitiesSectionHeight();

        DrawSectionHeader("Entities by type");
        entityTypeScroll = EditorGUILayout.BeginScrollView(entityTypeScroll, GUILayout.Height(half));
        DrawEntityTypes();
        EditorGUILayout.EndScrollView();

        DrawSectionHeader($"Entity instances ({entityInstances.Count})");
        entityInstanceScroll = EditorGUILayout.BeginScrollView(entityInstanceScroll, GUILayout.Height(half));
        DrawEntityInstances();
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Высота одного из двух списков сущностей.
    /// </summary>
    /// <remarks>
    /// Считается от высоты окна, а не измеряется по факту: <c>GUILayoutUtility</c> в фазе
    /// раскладки ещё не знает координат, и значение разошлось бы с фазой отрисовки.
    /// </remarks>
    private float ResolveEntitiesSectionHeight()
    {
        const float chromeHeight = 168f;
        const float minSectionHeight = 120f;

        return Mathf.Max(minSectionHeight, (position.height - chromeHeight) * 0.5f);
    }

    private void DrawEntityTypes()
    {
        DrawFixedRow(true, ("", 18), ("Icon", 46), ("Type / kind", 174), ("Registered", 90),
            ("On scene", 80), ("Hidden", 70), ("In pool", 70), ("Quality", 70));

        int count = 0;
        foreach (var row in entities)
        {
            bool matchesType = MatchesSearch(row.Type);
            bool matchesKind = row.Kinds.Any(kind => MatchesSearch(kind.Name));

            if (!matchesType && !matchesKind)
                continue;

            count++;
            bool expanded = expandedEntityTypes.Contains(row.Type);

            EditorGUILayout.BeginHorizontal(GUILayout.Height(40f));

            // Разворот только там, где есть что разворачивать: тип без живых экземпляров
            // разбивки не даёт, и пустой треугольник рядом с ним только сбивает с толку.
            using (new EditorGUI.DisabledScope(row.Kinds.Count == 0))
            {
                bool toggled = EditorGUILayout.Toggle(expanded, EditorStyles.foldout, GUILayout.Width(18f));
                if (toggled != expanded && row.Kinds.Count > 0)
                {
                    if (toggled)
                        expandedEntityTypes.Add(row.Type);
                    else
                        expandedEntityTypes.Remove(row.Type);
                }
            }

            DrawIcon(row.Icon, 46f, 36f);
            Label(row.Type, 174); Label(row.Registered, 90); Label(row.OnScene, 80);
            Label(row.Hidden, 70); Label(row.InPool, 70); Label("-", 70);
            EditorGUILayout.EndHorizontal();

            // Поиск по виду сам раскрывает тип: иначе найденное осталось бы спрятанным
            // под свёрнутой строкой, и поиск выглядел бы сломанным.
            if (expandedEntityTypes.Contains(row.Type) || (matchesKind && !matchesType))
                DrawEntityKinds(row);
        }

        DrawEmpty(count, "No entity types match the current search.");
    }

    /// <summary>
    /// Рисует разбивку типа по видам предметов.
    /// </summary>
    /// <remarks>
    /// В колонке Registered у вида стоит число живых экземпляров: сколько предметов
    /// каждого вида зарегистрировано, трекер не знает - он ведёт счёт по типу.
    /// </remarks>
    private void DrawEntityKinds(EntityRow row)
    {
        if (row.Kinds.Count == 0)
            return;

        foreach (var kind in row.Kinds)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(34f));
            GUILayout.Space(18f);
            DrawIcon(kind.Icon, 46f, 30f);
            Label(kind.Name, 174);
            Label(kind.Total, 90);
            Label(kind.OnScene, 80);
            Label(kind.Total - kind.OnScene, 70);
            Label(kind.InPool, 70);
            Label(kind.Quality, 70);
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawEntityInstances()
    {
        DrawFixedRow(true, ("ID", 65), ("Type", 145), ("Name", 145), ("Lifetime", 75),
            ("Scene", 50), ("Pool", 60), ("Object", 60), ("Dispose", 60));

        int instanceCount = 0;
        foreach (var row in entityInstances)
        {
            if (!MatchesSearch(row.Id, row.Type, row.Name, row.LifeTime))
                continue;

            instanceCount++;
            EditorGUILayout.BeginHorizontal();
            Label(row.Id, 65);
            Label(row.Type, 145);
            Label(row.Name, 145);
            Label(row.LifeTime, 75);
            Label(row.OnScene, 50);
            Label(row.PoolStatus, 60);
            DrawObjectButton(row.GameObject);
            DrawEntityDisposeButton(row);
            EditorGUILayout.EndHorizontal();
        }

        DrawEmpty(instanceCount, "No entity instances match the current search.");
    }

    private void DrawEntityDisposeButton(EntityInstanceRow row)
    {
        bool unavailable = row.Entity == null || row.Entity.IsNull() || row.InPool;
        using (new EditorGUI.DisabledScope(unavailable))
        {
            if (!GUILayout.Button("Dispose", GUILayout.Width(55f)))
                return;
        }

        string message = $"Dispose entity '{row.Name}' ({row.Type}, ID {row.Id}) through IEntity.DestroyEntity()?\n\n" +
                         "Depending on EntityDisposeAction, it may be returned to the pool instead of being fully destroyed.";
        if (!EditorUtility.DisplayDialog("Dispose entity", message, "Dispose", "Cancel"))
            return;

        try
        {
            row.Entity.DestroyEntity();
            EditorApplication.delayCall += () =>
            {
                RefreshSnapshot();
                Repaint();
            };
        }
        catch (System.Exception exception)
        {
            snapshotError = $"Entity dispose failed: {exception.GetType().Name}: {exception.Message}";
        }
    }

    private void DrawPools()
    {
        DrawSectionHeader($"Object pools ({pools.Count})");

        using (new EditorGUI.DisabledScope(pools.Count == 0 || PRUnitySDK.Managers.ObjectPool == null))
        {
            if (GUILayout.Button("Clear all pools", GUILayout.Width(120f)))
                ClearAllPools();
        }

        EditorGUILayout.Space(3f);
        DrawFixedRow(true, ("Type", 150), ("Category", 185), ("Total", 55),
            ("Active", 55), ("Free", 55), ("Usage", 60), ("Clear", 55));
        int count = 0;
        foreach (var row in pools)
        {
            if (!MatchesSearch(row.Type, row.Category)) continue;
            count++;
            float usage = row.TotalCount > 0 ? row.ShowCount / (float)row.TotalCount : 0f;
            EditorGUILayout.BeginHorizontal();
            Label(row.Type, 150);
            Label(row.Category, 185);
            Label(row.TotalCount, 55);
            Label(row.ShowCount, 55);
            Label(row.HideCount, 55);
            Label($"{usage:P0}", 60);
            DrawPoolClearButton(row);
            EditorGUILayout.EndHorizontal();
        }
        DrawEmpty(count, "No pools match the current search.");
    }

    private void DrawPoolClearButton(PoolSystemTableData row)
    {
        using (new EditorGUI.DisabledScope(PRUnitySDK.Managers.ObjectPool == null))
        {
            if (!GUILayout.Button("Clear", GUILayout.Width(50f)))
                return;
        }

        string message = $"Clear pool '{row.Type}/{row.Category}'?\n\n" +
                         $"All {row.TotalCount} GameObjects ({row.ShowCount} active, {row.HideCount} free) " +
                         "will be destroyed and the pool registration will be removed.";
        if (!EditorUtility.DisplayDialog("Clear object pool", message, "Clear", "Cancel"))
            return;

        try
        {
            if (!PRUnitySDK.Managers.ObjectPool.ClearPool(row.Type, row.Category))
                snapshotError = $"Pool '{row.Type}/{row.Category}' no longer exists.";

            RefreshAfterPoolClear();
        }
        catch (System.Exception exception)
        {
            snapshotError = $"Pool clear failed: {exception.GetType().Name}: {exception.Message}";
        }
    }

    private void ClearAllPools()
    {
        long total = pools.Sum(row => row.TotalCount);
        long active = pools.Sum(row => row.ShowCount);
        long free = pools.Sum(row => row.HideCount);
        string message = $"Clear all {pools.Count} pools?\n\n" +
                         $"All {total} GameObjects ({active} active, {free} free) will be destroyed " +
                         "and every pool registration will be removed.";
        if (!EditorUtility.DisplayDialog("Clear all object pools", message, "Clear all", "Cancel"))
            return;

        try
        {
            PRUnitySDK.Managers.ObjectPool.ClearData();
            RefreshAfterPoolClear();
        }
        catch (System.Exception exception)
        {
            snapshotError = $"Pools clear failed: {exception.GetType().Name}: {exception.Message}";
        }
    }

    private void RefreshAfterPoolClear()
    {
        EditorApplication.delayCall += () =>
        {
            RefreshSnapshot();
            Repaint();
        };
    }

    private void DrawFlags()
    {
        DrawGlobalFlagControls();
        DrawSectionHeader("Flag resolvers");
        int count = 0;
        foreach (var resolver in flagResolvers)
        {
            bool resolverMatch = MatchesSearch(resolver.Name);
            if (!resolverMatch && !resolver.Flags.Any(flag => MatchesSearch(flag.Key?.Value)) && !string.IsNullOrWhiteSpace(search))
                continue;

            count++;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{resolver.Name} ({resolver.Flags.Count})", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            DrawObjectButton(resolver.Owner);
            EditorGUILayout.EndHorizontal();

            foreach (var flag in resolver.Flags)
            {
                if (!resolverMatch && !MatchesSearch(flag.Key?.Value)) continue;
                Color old = GUI.color;
                GUI.color = DecisionColor(flag.Decision);
                EditorGUILayout.LabelField($"{flag.Key}: {flag.Decision}", EditorStyles.miniBoldLabel);
                GUI.color = old;

                foreach (var influence in flag.Influences)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(16f);
                    Label(influence.IsFrame ? "Frame" : "Persistent", 70);
                    Label(influence.Decision, 55);
                    EditorGUILayout.LabelField(SourceName(influence.Source));
                    DrawObjectButton(influence.Source as Object);
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
        }
        DrawEmpty(count, "No flag resolvers match the current search.");
    }

    private void DrawGlobalFlagControls()
    {
        DrawSectionHeader("Global test flags");

        var manager = PRUnitySDK.Managers.Flags;
        if (manager == null)
        {
            EditorGUILayout.HelpBox("FlagsManager is not initialized.", MessageType.Info);
            return;
        }

        if (flagProviders.Count == 0)
        {
            EditorGUILayout.HelpBox("No concrete FlagsProviderBase providers with flags were found.", MessageType.Info);
            return;
        }

        string[] providerNames = flagProviders.Select(provider => provider.Name).ToArray();
        int previousProviderIndex = selectedFlagProviderIndex;
        selectedFlagProviderIndex = EditorGUILayout.Popup("Provider", selectedFlagProviderIndex, providerNames);
        if (selectedFlagProviderIndex != previousProviderIndex)
            selectedFlagIndex = 0;

        FlagProviderRow providerRow = flagProviders[selectedFlagProviderIndex];
        string[] flagNames = providerRow.Flags.Select(flag => flag.Value).ToArray();
        selectedFlagIndex = Mathf.Clamp(selectedFlagIndex, 0, flagNames.Length - 1);
        selectedFlagIndex = EditorGUILayout.Popup("Flag", selectedFlagIndex, flagNames);

        Enumeration flag = providerRow.Flags[selectedFlagIndex];
        var globalSnapshot = manager.Global.GetDebugSnapshot();
        FlagDecision debugDecision = GetDebugFlagDecision(globalSnapshot, flag);
        FlagDecision globalDecision = manager.Global.Resolve(flag);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Global: {globalDecision}", GUILayout.Width(130f));
        EditorGUILayout.LabelField($"Debug: {debugDecision}", GUILayout.Width(130f));
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Allow", GUILayout.Width(55f)))
            SetDebugFlag(flag, FlagDecision.Allow);
        if (GUILayout.Button("Deny", GUILayout.Width(55f)))
            SetDebugFlag(flag, FlagDecision.Deny);

        using (new EditorGUI.DisabledScope(debugDecision == FlagDecision.Unspecified))
        {
            if (GUILayout.Button("Remove", GUILayout.Width(65f)))
                SetDebugFlag(flag, FlagDecision.Unspecified);
        }

        bool hasDebugFlags = globalSnapshot.Any(info => info.Influences.Any(influence =>
            ReferenceEquals(influence.Source, debugFlagSource)));
        using (new EditorGUI.DisabledScope(!hasDebugFlags))
        {
            if (GUILayout.Button("Clear test flags", GUILayout.Width(100f)))
                ClearDebugFlags(true);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox(
            "These changes affect only the project-wide Global resolver and are cleared when the Debug window closes or scripts reload.",
            MessageType.None);
    }

    private FlagDecision GetDebugFlagDecision(
        System.Collections.Generic.IReadOnlyList<FlagDebugInfo> snapshot,
        Enumeration flag)
    {
        foreach (var info in snapshot)
        {
            if (info.Key != flag)
                continue;

            foreach (var influence in info.Influences)
            {
                if (ReferenceEquals(influence.Source, debugFlagSource))
                    return influence.Decision;
            }
        }

        return FlagDecision.Unspecified;
    }

    private void SetDebugFlag(Enumeration flag, FlagDecision decision)
    {
        try
        {
            var manager = PRUnitySDK.Managers.Flags;
            if (manager == null || flag == null)
                return;

            switch (decision)
            {
                case FlagDecision.Allow:
                    manager.Allow(flag, debugFlagSource);
                    break;
                case FlagDecision.Deny:
                    manager.Deny(flag, debugFlagSource);
                    break;
                default:
                    manager.Remove(flag, debugFlagSource);
                    break;
            }

            RefreshSnapshot();
            Repaint();
        }
        catch (System.Exception exception)
        {
            snapshotError = $"Test flag change failed: {exception.GetType().Name}: {exception.Message}";
        }
    }

    private void ClearDebugFlags(bool refresh)
    {
        try
        {
            PRUnitySDK.Managers.Flags?.ClearSource(debugFlagSource);
            if (refresh)
            {
                RefreshSnapshot();
                Repaint();
            }
        }
        catch (System.Exception exception)
        {
            if (refresh)
                snapshotError = $"Test flags clear failed: {exception.GetType().Name}: {exception.Message}";
        }
    }
}
