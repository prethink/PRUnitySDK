using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Вкладка Tools окна PRUnitySDK Debug: снимки экрана и видимость блоков интерфейса.
/// </summary>
/// <remarks>
/// Обе части служат одному делу — собрать кадр: спрятать лишнее с экрана и снять то, что
/// осталось. Поэтому они на одной вкладке, а не в разных углах окна.
/// </remarks>
public sealed class ToolsDebugTab : IPRDebugTab
{
    /// <summary>
    /// Чьим именем вкладка прячет интерфейс целиком.
    /// </summary>
    /// <remarks>
    /// Свой источник, а не общий переключатель: игра прячет интерфейс и сама, и по выходе
    /// из отладки он должен вернуться к тому, что просит игра, а не к «показать».
    /// </remarks>
    private static readonly object HideSource = new();

    private IReadOnlyList<IHudBlock> blocks = new List<IHudBlock>();

    /// <inheritdoc />
    public string Title => "Tools";

    /// <inheritdoc />
    public int Order => -200;

    /// <inheritdoc />
    public bool AvailableInEditMode => true;

    /// <inheritdoc />
    public void Refresh(PRDebugTabContext context)
    {
        blocks = context.IsPlaying ? PRUnitySDK.Trackers.HudBlocks.Elements : new List<IHudBlock>();
    }

    /// <inheritdoc />
    public void Draw(PRDebugTabContext context)
    {
        DrawScreenshots(context);
        EditorGUILayout.Space(8f);
        DrawHud(context);
    }

    #region Снимки

    private static void DrawScreenshots(PRDebugTabContext context)
    {
        EditorGUILayout.LabelField("Screenshots", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            string folder = EditorGUILayout.DelayedTextField("Folder", PRScreenshotTool.Folder);

            if (EditorGUI.EndChangeCheck())
                PRScreenshotTool.Folder = folder;

            if (GUILayout.Button("…", EditorStyles.miniButton, GUILayout.Width(26f)))
            {
                string selected = EditorUtility.OpenFolderPanel("Screenshots folder", PRScreenshotTool.Folder, string.Empty);

                if (!string.IsNullOrEmpty(selected))
                    PRScreenshotTool.Folder = selected;
            }

            if (GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(48f)))
            {
                Directory.CreateDirectory(PRScreenshotTool.Folder);
                EditorUtility.RevealInFinder(PRScreenshotTool.Folder);
            }
        }

        Vector2Int size = PRScreenshotTool.GameViewSize;
        int scale = EditorGUILayout.IntSlider(
            new GUIContent("Scale", "Во сколько раз снимок больше окна Game. Размер самого окна задаётся в его заголовке."),
            PRScreenshotTool.Scale, 1, PRScreenshotTool.MaxScale);

        if (scale != PRScreenshotTool.Scale)
            PRScreenshotTool.Scale = scale;

        EditorGUILayout.LabelField("Size", $"{size.x * scale} x {size.y * scale}  (Game view {size.x} x {size.y})");

        using (new EditorGUILayout.HorizontalScope())
        {
            bool hotkeyEnabled = EditorGUILayout.ToggleLeft(
                new GUIContent("Hotkey in Play Mode", "Клавиша слушается, пока идёт игра, и при закрытом окне отладки тоже."),
                PRScreenshotTool.HotkeyEnabled, GUILayout.Width(150f));

            if (hotkeyEnabled != PRScreenshotTool.HotkeyEnabled)
                PRScreenshotTool.HotkeyEnabled = hotkeyEnabled;

            using (new EditorGUI.DisabledScope(!hotkeyEnabled))
            {
                var hotkey = (KeyCode)EditorGUILayout.EnumPopup(PRScreenshotTool.Hotkey);

                if (hotkey != PRScreenshotTool.Hotkey)
                    PRScreenshotTool.Hotkey = hotkey;
            }
        }

        if (GUILayout.Button("Capture", GUILayout.Height(26f)))
        {
            // За пределами отрисовки: снимок просит кадр, а посреди OnGUI это ломает разметку.
            EditorApplication.delayCall += () => PRScreenshotTool.Capture();
        }

        if (!context.IsPlaying)
        {
            EditorGUILayout.HelpBox(
                "Вне Play Mode снимается то, что сейчас показывает окно Game; оно должно быть открыто.",
                MessageType.None);
        }

        string last = PRScreenshotTool.LastPath;

        if (string.IsNullOrEmpty(last))
            return;

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Last", Path.GetFileName(last));

            using (new EditorGUI.DisabledScope(!File.Exists(last)))
            {
                if (GUILayout.Button("Show", EditorStyles.miniButton, GUILayout.Width(48f)))
                    EditorUtility.RevealInFinder(last);
            }
        }
    }

    #endregion

    #region Интерфейс

    private void DrawHud(PRDebugTabContext context)
    {
        EditorGUILayout.LabelField("HUD", EditorStyles.boldLabel);

        if (!context.IsPlaying)
        {
            EditorGUILayout.HelpBox("Блоки интерфейса создаёт игра: список появится в Play Mode.", MessageType.None);
            return;
        }

        HudTracker hud = PRUnitySDK.Trackers.Hud;
        bool wholeShown = !hud.IsHiddenBy(HideSource);
        bool newWholeShown = EditorGUILayout.ToggleLeft(
            new GUIContent("Whole HUD", "Весь постоянный интерфейс разом. Окна и уведомления остаются."), wholeShown);

        if (newWholeShown != wholeShown)
        {
            if (newWholeShown)
                hud.Release(HideSource);
            else
                hud.Hide(HideSource);
        }

        if (blocks.Count == 0)
        {
            EditorGUILayout.HelpBox("На сцене нет блоков интерфейса (установщиков UIInstaller).", MessageType.None);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Show all", EditorStyles.miniButtonLeft))
                SetAllHidden(false);

            if (GUILayout.Button("Hide all", EditorStyles.miniButtonRight))
                SetAllHidden(true);
        }

        var names = new Dictionary<string, int>();

        foreach (IHudBlock block in blocks)
        {
            if (IsDestroyed(block))
                continue;

            string name = GetUniqueName(block, names);

            if (!context.Matches(name))
                continue;

            // Блок без вида тоже переключается: признак запоминается, и вид появится уже спрятанным.
            string label = block.HasBlockView ? name : $"{name}  (not shown yet)";
            bool shown = !block.IsBlockHidden;
            bool newShown = EditorGUILayout.ToggleLeft(label, shown);

            if (newShown != shown)
                block.SetBlockHidden(!newShown);
        }
    }

    private void SetAllHidden(bool hidden)
    {
        foreach (IHudBlock block in blocks)
        {
            if (!IsDestroyed(block))
                block.SetBlockHidden(hidden);
        }
    }

    /// <summary>
    /// Название блока, а при повторе — с номером.
    /// </summary>
    /// <remarks>
    /// Два одинаковых установщика на сцене — обычное дело, и без номера в списке стояли бы
    /// две неразличимые строки.
    /// </remarks>
    private static string GetUniqueName(IHudBlock block, Dictionary<string, int> names)
    {
        string name = string.IsNullOrEmpty(block.BlockName) ? block.GetType().Name : block.BlockName;

        names.TryGetValue(name, out int count);
        names[name] = count + 1;

        return count == 0 ? name : $"{name} #{count + 1}";
    }

    /// <remarks>
    /// Снимок списка мог устареть между обновлениями окна: сцену успели сменить.
    /// </remarks>
    private static bool IsDestroyed(IHudBlock block)
    {
        return block == null || block is Object unityObject && unityObject == null;
    }

    #endregion
}
