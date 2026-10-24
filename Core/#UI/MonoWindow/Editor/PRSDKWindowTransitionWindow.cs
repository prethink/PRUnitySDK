using System;
using DG.Tweening;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Окно перехода окон: выбор пресета, свои значения и превью появления и закрытия.
/// </summary>
/// <remarks>
/// Правит переход проекта по умолчанию (<see cref="PRSDKSettings.WindowTransition"/>).
/// Превью считает тот же переход, что <c>MonoWindowBase</c>: масштаб и сдвиг идут по кривой
/// <see cref="MonoWindowTransition.ShowEase"/> / <see cref="MonoWindowTransition.HideEase"/>
/// (<see cref="DOVirtual.EasedValue"/>), прозрачность — по OutQuad при появлении и InQuad
/// при закрытии, вместе с затемнением фона.
/// <para>
/// Снизу — все пресеты разом, каждый в своей ячейке по кругу: сравнить их удобнее рядом,
/// чем переключая по одному. Нажатие на ячейку выбирает пресет.
/// </para>
/// <para>
/// Свой переход у отдельного окна (<c>Transition Mode = Override</c>) правится на его префабе;
/// здесь — только переход по умолчанию. Окна читают его при каждом открытии, поэтому правка
/// во время игры действует сразу.
/// </para>
/// </remarks>
public sealed class PRSDKWindowTransitionWindow : EditorWindow, IPRSDKGameWindow
{
    private const float SettingsWidth = 380f;
    private const float ShownPause = 0.7f;
    private const float HiddenPause = 0.45f;
    private const float CellHeight = 92f;

    private static readonly MonoWindowTransitionPreset[] Presets =
    {
        MonoWindowTransitionPreset.Pop,
        MonoWindowTransitionPreset.Soft,
        MonoWindowTransitionPreset.Fade,
        MonoWindowTransitionPreset.SlideUp,
        MonoWindowTransitionPreset.SlideDown,
        MonoWindowTransitionPreset.Elastic,
        MonoWindowTransitionPreset.Custom
    };

    private static readonly Color Screen = new(0.32f, 0.5f, 0.38f);
    private static readonly Color ScreenDetail = new(0.27f, 0.43f, 0.32f);
    private static readonly Color Panel = new(0.12f, 0.14f, 0.2f);
    private static readonly Color HeaderColor = new(0.95f, 0.71f, 0.23f);
    private static readonly Color CloseColor = new(0.88f, 0.27f, 0.24f);
    private static readonly Color Row = new(0.19f, 0.22f, 0.3f);
    private static readonly Color ButtonColor = new(0.3f, 0.76f, 0.29f);

    private readonly PRSDKSettingsSection section = new("WindowTransition");

    private Vector2 scroll;
    private bool loop = true;
    private bool targetShown = true;
    private double phaseStart;
    private GUIStyle noteStyle;
    private GUIStyle titleStyle;
    private GUIStyle cellStyle;

    [MenuItem(MonoWindowTransitionSettings.MenuPath, false, 63)]
    private static void Open()
    {
        var window = PRSDKGameWindows.Open<PRSDKWindowTransitionWindow>("Window Transition");
        window.minSize = new Vector2(880f, 520f);
    }

    private void OnEnable()
    {
        EditorApplication.update += Repaint;
        Undo.undoRedoPerformed += Repaint;
        phaseStart = EditorApplication.timeSinceStartup;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Repaint;
        Undo.undoRedoPerformed -= Repaint;
        section.Dispose();
    }

    private MonoWindowTransitionSettings Settings =>
        section.Settings != null ? section.Settings.WindowTransition : null;

    private void OnGUI()
    {
        if (!section.Begin())
        {
            EditorGUILayout.HelpBox("Настройки проекта не найдены. Выберите проект в PRUnitySDK → Project.", MessageType.Warning);
            return;
        }

        noteStyle ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
        titleStyle ??= new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = new Color(0.15f, 0.1f, 0.05f) } };
        cellStyle ??= new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.UpperLeft };

        section.DrawToolbar();

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(SettingsWidth)))
            {
                scroll = EditorGUILayout.BeginScrollView(scroll);
                DrawSettings();
                EditorGUILayout.EndScrollView();
            }

            using (new EditorGUILayout.VerticalScope())
                DrawPreview();
        }

        section.End();
    }

    #region Поля

    private void DrawSettings()
    {
        EditorGUILayout.Space(6f);
        section.Field("Enabled", new GUIContent("Плавный переход", "Выключено — окна появляются и закрываются сразу."));

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Пресет", EditorStyles.boldLabel);

        SerializedProperty preset = section.Find("Preset");

        foreach (MonoWindowTransitionPreset option in Presets)
        {
            bool selected = preset.intValue == (int)option;

            using (new EditorGUILayout.HorizontalScope())
            {
                bool pressed = GUILayout.Toggle(selected, Title(option), EditorStyles.radioButton, GUILayout.Width(120f));

                if (pressed && !selected)
                {
                    preset.intValue = (int)option;
                    GUI.FocusControl(null);
                }

                GUILayout.Label(Description(option), noteStyle);
            }
        }

        if (preset.intValue != (int)MonoWindowTransitionPreset.Custom)
        {
            EditorGUILayout.Space(6f);

            if (GUILayout.Button(new GUIContent("Настроить своё на основе пресета",
                    "Переключиться на Custom и взять значения выбранного пресета за отправную точку.")))
            {
                CopyPresetToCustom((MonoWindowTransitionPreset)preset.intValue);
                preset.intValue = (int)MonoWindowTransitionPreset.Custom;
            }

            return;
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Свои значения", EditorStyles.boldLabel);

        foreach (string field in new[] { "ShowDuration", "ShowEase", "HideDuration", "HideEase", "HiddenScale", "Fade", "SlideOffset" })
            section.Field("Custom." + field);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField(
            "Масштаб 0 — окно вырастает из точки, 1 — без роста. Сдвиг — в долях размера окна: (0, -0.35) — снизу на треть.",
            noteStyle);
    }

    /// <summary>
    /// Переносит значения пресета в свои — чтобы править от него, а не с нуля.
    /// </summary>
    private void CopyPresetToCustom(MonoWindowTransitionPreset preset)
    {
        MonoWindowTransition source = MonoWindowTransition.FromPreset(preset);

        if (source == null)
            return;

        section.Find("Custom.ShowDuration").floatValue = source.ShowDuration;
        section.Find("Custom.ShowEase").intValue = (int)source.ShowEase;
        section.Find("Custom.HideDuration").floatValue = source.HideDuration;
        section.Find("Custom.HideEase").intValue = (int)source.HideEase;
        section.Find("Custom.HiddenScale").floatValue = source.HiddenScale;
        section.Find("Custom.Fade").boolValue = source.Fade;
        section.Find("Custom.SlideOffset").vector2Value = source.SlideOffset;
    }

    private static string Title(MonoWindowTransitionPreset preset) => preset switch
    {
        MonoWindowTransitionPreset.Pop => "Pop",
        MonoWindowTransitionPreset.Soft => "Soft",
        MonoWindowTransitionPreset.Fade => "Fade",
        MonoWindowTransitionPreset.SlideUp => "Slide Up",
        MonoWindowTransitionPreset.SlideDown => "Slide Down",
        MonoWindowTransitionPreset.Elastic => "Elastic",
        _ => "Custom"
    };

    private static string Description(MonoWindowTransitionPreset preset) => preset switch
    {
        MonoWindowTransitionPreset.Pop => "выпрыгивает из центра с лёгким перелётом",
        MonoWindowTransitionPreset.Soft => "мягко проявляется, чуть подрастая",
        MonoWindowTransitionPreset.Fade => "только проявляется и гаснет",
        MonoWindowTransitionPreset.SlideUp => "въезжает снизу",
        MonoWindowTransitionPreset.SlideDown => "въезжает сверху",
        MonoWindowTransitionPreset.Elastic => "пружинит, выскакивая из центра",
        _ => "свои значения ниже"
    };

    #endregion

    #region Превью

    private void DrawPreview()
    {
        MonoWindowTransitionSettings settings = Settings;
        MonoWindowTransition current = settings != null ? settings.Resolve() : null;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Открыть", EditorStyles.miniButtonLeft, GUILayout.Width(70f)))
                StartPhase(shown: true);

            if (GUILayout.Button("Закрыть", EditorStyles.miniButtonRight, GUILayout.Width(70f)))
                StartPhase(shown: false);

            loop = GUILayout.Toggle(loop, "По кругу", EditorStyles.miniButton, GUILayout.Width(70f));
            GUILayout.FlexibleSpace();

            if (current == null)
                GUILayout.Label("Переход выключен: окна появляются сразу.", EditorStyles.miniLabel);
            else
                GUILayout.Label($"Появление {current.ShowDuration:0.00} с · закрытие {current.HideDuration:0.00} с", EditorStyles.miniLabel);
        }

        // Кадр 16:9 по ширине, но не выше места над ячейками: иначе под ним оставалась пустота
        // или ячейки уезжали за край.
        Rect area = GUILayoutUtility.GetAspectRect(16f / 9f, GUILayout.MaxHeight(Mathf.Max(120f, position.height - CellHeight - 90f)));
        area = FitScreen(new Rect(area.x + 4f, area.y + 4f, area.width - 8f, area.height - 8f));

        double now = EditorApplication.timeSinceStartup;
        AdvanceLoop(current, now);

        if (Event.current.type == EventType.Repaint)
            DrawScreen(area, current, targetShown, (float)(now - phaseStart), large: true);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Все пресеты — нажмите, чтобы выбрать", EditorStyles.miniBoldLabel);
        DrawPresetCells(settings, now);
    }

    private void StartPhase(bool shown)
    {
        targetShown = shown;
        phaseStart = EditorApplication.timeSinceStartup;
    }

    /// <summary>
    /// Большое превью по кругу: появилось, постояло, закрылось, постояло пустым.
    /// </summary>
    private void AdvanceLoop(MonoWindowTransition transition, double now)
    {
        if (!loop)
            return;

        float elapsed = (float)(now - phaseStart);
        float showDuration = transition?.ShowDuration ?? 0f;
        float hideDuration = transition?.HideDuration ?? 0f;

        if (targetShown && elapsed > showDuration + ShownPause)
            StartPhase(shown: false);
        else if (!targetShown && elapsed > hideDuration + HiddenPause)
            StartPhase(shown: true);
    }

    private void DrawPresetCells(MonoWindowTransitionSettings settings, double now)
    {
        Rect strip = GUILayoutUtility.GetRect(100f, CellHeight, GUILayout.ExpandWidth(true));
        float width = (strip.width - 6f * (Presets.Length - 1)) / Presets.Length;

        for (int i = 0; i < Presets.Length; i++)
        {
            MonoWindowTransitionPreset option = Presets[i];
            var cell = new Rect(strip.x + i * (width + 6f), strip.y, width, CellHeight);

            MonoWindowTransition transition = option == MonoWindowTransitionPreset.Custom
                ? settings?.Custom ?? new MonoWindowTransition()
                : MonoWindowTransition.FromPreset(option);

            bool selected = settings != null && settings.Preset == option;

            if (Event.current.type == EventType.MouseDown && cell.Contains(Event.current.mousePosition))
            {
                section.Find("Preset").intValue = (int)option;
                Event.current.Use();
            }

            if (Event.current.type != EventType.Repaint)
                continue;

            // Свой круг у каждой ячейки: длительности у пресетов разные.
            float cycle = transition.ShowDuration + ShownPause + transition.HideDuration + HiddenPause;
            float t = (float)(now % cycle);
            bool shown = t < transition.ShowDuration + ShownPause;
            float phase = shown ? t : t - transition.ShowDuration - ShownPause;

            var screen = new Rect(cell.x, cell.y + 14f, cell.width, cell.height - 14f);
            DrawScreen(screen, transition, shown, phase, large: false);

            GUI.Label(new Rect(cell.x, cell.y - 1f, cell.width, 14f), Title(option), cellStyle);

            if (selected)
                DrawFrame(screen, HeaderColor, 2f);
        }
    }

    private static Rect FitScreen(Rect area)
    {
        float width = Mathf.Min(area.width, area.height * 16f / 9f);
        float height = width * 9f / 16f;
        return new Rect(area.x + (area.width - width) * 0.5f, area.y, width, height);
    }

    /// <summary>
    /// Экран игры с окном в нужный момент перехода.
    /// </summary>
    /// <param name="screen">Где рисовать.</param>
    /// <param name="transition">Переход; пусто — без перехода.</param>
    /// <param name="shown">Окно открывается (иначе закрывается).</param>
    /// <param name="elapsed">Секунды с начала этой фазы.</param>
    /// <param name="large">Большое превью: с подписями на окне.</param>
    private void DrawScreen(Rect screen, MonoWindowTransition transition, bool shown, float elapsed, bool large)
    {
        GUI.BeginClip(screen);
        var local = new Rect(0f, 0f, screen.width, screen.height);

        // Мир игры позади окна — чтобы было видно затемнение.
        EditorGUI.DrawRect(local, Screen);
        EditorGUI.DrawRect(new Rect(0f, local.height * 0.62f, local.width, local.height * 0.38f), ScreenDetail);
        EditorGUI.DrawRect(new Rect(local.width * 0.08f, local.height * 0.07f, local.width * 0.22f, local.height * 0.07f), new Color(0f, 0f, 0f, 0.25f));

        Evaluate(transition, shown, elapsed, out float scale, out Vector2 slide, out float alpha, out bool visible);

        if (visible)
        {
            EditorGUI.DrawRect(local, new Color(0f, 0f, 0f, 0.55f * alpha));

            var panel = new Rect(0f, 0f, local.width * 0.46f, local.height * 0.62f);
            panel.center = local.center + new Vector2(slide.x * panel.width, -slide.y * panel.height);

            if (scale > 0.01f)
                DrawPanel(panel, scale, alpha, large);
        }

        GUI.EndClip();
    }

    /// <summary>
    /// Состояние окна в момент перехода — те же правила, что у <c>MonoWindowBase</c>.
    /// </summary>
    private static void Evaluate(MonoWindowTransition transition, bool shown, float elapsed,
        out float scale, out Vector2 slide, out float alpha, out bool visible)
    {
        scale = 1f;
        slide = Vector2.zero;
        alpha = 1f;
        visible = shown;

        if (transition == null)
            return;

        float duration = shown ? transition.ShowDuration : transition.HideDuration;
        float progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

        // Появление: из скрытого к обычному; закрытие: наоборот.
        float eased = DOVirtual.EasedValue(0f, 1f, progress, shown ? transition.ShowEase : transition.HideEase);
        float toShown = shown ? eased : 1f - eased;

        scale = Mathf.LerpUnclamped(transition.HiddenScale, 1f, toShown);
        slide = Vector2.LerpUnclamped(transition.SlideOffset, Vector2.zero, toShown);

        if (transition.Fade)
        {
            alpha = shown
                ? DOVirtual.EasedValue(0f, 1f, progress, Ease.OutQuad)
                : DOVirtual.EasedValue(1f, 0f, progress, Ease.InQuad);
        }

        visible = shown || progress < 1f;
    }

    /// <summary>
    /// Макет окна в одобренном стиле: тёмная панель, жёлтая шапка, красный крестик, строки, зелёная кнопка.
    /// </summary>
    private void DrawPanel(Rect panel, float scale, float alpha, bool large)
    {
        Matrix4x4 matrix = GUI.matrix;
        Color tint = GUI.color;

        GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), panel.center);

        float header = panel.height * 0.16f;
        float pad = panel.width * 0.05f;

        EditorGUI.DrawRect(panel, Faded(Panel, alpha));
        EditorGUI.DrawRect(new Rect(panel.x, panel.y, panel.width, header), Faded(HeaderColor, alpha));

        float close = header * 0.7f;
        EditorGUI.DrawRect(new Rect(panel.xMax - close - header * 0.15f, panel.y + header * 0.15f, close, close), Faded(CloseColor, alpha));

        float rowHeight = panel.height * 0.13f;

        for (int i = 0; i < 3; i++)
        {
            var row = new Rect(panel.x + pad, panel.y + header + pad + i * (rowHeight + pad * 0.6f), panel.width - pad * 2f, rowHeight);
            EditorGUI.DrawRect(row, Faded(Row, alpha));
        }

        var button = new Rect(panel.center.x - panel.width * 0.22f, panel.yMax - pad - rowHeight * 1.1f, panel.width * 0.44f, rowHeight * 1.1f);
        EditorGUI.DrawRect(button, Faded(ButtonColor, alpha));

        if (large)
        {
            // Прямоугольники гаснут своим цветом, а подписи — через общий цвет GUI.
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(panel.x + pad, panel.y, panel.width, header), "Награда", titleStyle);
        }

        GUI.color = tint;
        GUI.matrix = matrix;
    }

    private static Color Faded(Color color, float alpha) => new(color.r, color.g, color.b, color.a * alpha);

    private static void DrawFrame(Rect rect, Color color, float width)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, width), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - width, rect.width, width), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, width, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - width, rect.y, width, rect.height), color);
    }

    #endregion
}
