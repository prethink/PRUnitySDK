using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

/// <summary>
/// Снимки окна Game из редактора.
/// </summary>
/// <remarks>
/// Снимает сам Unity (<see cref="ScreenCapture"/>), а не экран: в файл попадает только кадр
/// игры в размере окна Game, умноженном на масштаб, — без рамок редактора и чужих окон.
/// <para>
/// Клавиша снимка слушается всегда, пока идёт игра, а не только при открытом окне отладки:
/// кадр ловят в самой игре, когда курсор захвачен и до кнопки в редакторе не дотянуться.
/// </para>
/// </remarks>
[InitializeOnLoad]
public static class PRScreenshotTool
{
    private const string FolderKey = "PRUnitySDK.Screenshots.Folder";
    private const string ScaleKey = "PRUnitySDK.Screenshots.Scale";
    private const string HotkeyEnabledKey = "PRUnitySDK.Screenshots.HotkeyEnabled";
    private const string HotkeyKey = "PRUnitySDK.Screenshots.Hotkey";

    /// <summary>
    /// Наибольший множитель размера: дальше снимок 1080p весит сотни мегабайт в памяти.
    /// </summary>
    public const int MaxScale = 4;

    // Клавиша опрашивается каждый кадр игры, а настройки редактора лежат в реестре:
    // значения держатся в памяти и пишутся в настройки только при смене.
    private static bool hotkeyEnabled;
    private static KeyCode hotkey;

    static PRScreenshotTool()
    {
        hotkeyEnabled = EditorPrefs.GetBool(HotkeyEnabledKey, true);
        hotkey = (KeyCode)EditorPrefs.GetInt(HotkeyKey, (int)KeyCode.F9);

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        // Домен перезагружается на входе в игру, и к этому моменту она уже запускается.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            InstallHotkey();
    }

    /// <summary>
    /// Папка, куда пишутся снимки.
    /// </summary>
    /// <remarks>
    /// По умолчанию — рядом с <c>Assets</c>, а не внутри: каждый снимок в <c>Assets</c>
    /// редактор импортировал бы как текстуру.
    /// </remarks>
    public static string Folder
    {
        get => EditorPrefs.GetString(FolderKey, DefaultFolder);
        set => EditorPrefs.SetString(FolderKey, string.IsNullOrWhiteSpace(value) ? DefaultFolder : value);
    }

    /// <summary>
    /// Папка по умолчанию: <c>Screenshots</c> в корне проекта.
    /// </summary>
    public static string DefaultFolder =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots"));

    /// <summary>
    /// Во сколько раз снимок больше окна Game.
    /// </summary>
    public static int Scale
    {
        get => Mathf.Clamp(EditorPrefs.GetInt(ScaleKey, 1), 1, MaxScale);
        set => EditorPrefs.SetInt(ScaleKey, Mathf.Clamp(value, 1, MaxScale));
    }

    /// <summary>
    /// Снимать по клавише во время игры.
    /// </summary>
    public static bool HotkeyEnabled
    {
        get => hotkeyEnabled;
        set
        {
            hotkeyEnabled = value;
            EditorPrefs.SetBool(HotkeyEnabledKey, value);
        }
    }

    /// <summary>
    /// Клавиша снимка.
    /// </summary>
    /// <remarks>
    /// По умолчанию F9: её не занимает ни игра, ни редактор. F12 не годится — при
    /// подключённом отладчике Windows останавливает по ней процесс.
    /// </remarks>
    public static KeyCode Hotkey
    {
        get => hotkey;
        set
        {
            hotkey = value;
            EditorPrefs.SetInt(HotkeyKey, (int)value);
        }
    }

    /// <summary>
    /// Файл последнего снимка за этот запуск редактора.
    /// </summary>
    public static string LastPath { get; private set; }

    /// <summary>
    /// Размер окна Game в пикселях.
    /// </summary>
    public static Vector2Int GameViewSize
    {
        get
        {
            Vector2 size = Handles.GetMainGameViewSize();
            return new Vector2Int(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y));
        }
    }

    /// <summary>
    /// Снимает окно Game в файл.
    /// </summary>
    /// <remarks>
    /// Файл появляется в конце кадра, а не к возврату из метода: Unity снимает уже
    /// отрисованный кадр. Вне игры кадр сам не перерисовывается, поэтому окна просят
    /// перерисоваться.
    /// </remarks>
    /// <returns>Путь к файлу, который будет записан.</returns>
    public static string Capture()
    {
        string folder = Folder;
        Directory.CreateDirectory(folder);

        Vector2Int size = GameViewSize * Scale;
        string name = $"{Sanitize(Application.productName)}_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{size.x}x{size.y}.png";
        string path = Path.Combine(folder, name);

        ScreenCapture.CaptureScreenshot(path, Scale);

        if (!EditorApplication.isPlaying)
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

        LastPath = path;
        Debug.Log($"Screenshot: {path}");
        return path;
    }

    /// <remarks>
    /// Пункт меню — способ снять кадр вне игры, не открывая окно отладки, и назначить
    /// на снимок своё сочетание клавиш в настройках редактора (Shortcuts).
    /// </remarks>
    [MenuItem("PRUnitySDK/Capture Screenshot", false, 200)]
    private static void CaptureFromMenu()
    {
        Capture();
    }

    #region Клавиша

    /// <summary>
    /// Метка нашего шага в игровом цикле: по ней он находится, чтобы не встать дважды.
    /// </summary>
    private struct ScreenshotHotkeyStep
    {
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
            InstallHotkey();
        else if (state == PlayModeStateChange.ExitingPlayMode)
            UninstallHotkey();
    }

    /// <summary>
    /// Ставит опрос клавиши в игровой цикл.
    /// </summary>
    /// <remarks>
    /// Именно в цикл, а не в <c>EditorApplication.update</c>: «клавишу нажали» верно только
    /// внутри игрового кадра. Редактор зовёт своё обновление уже после него, когда ввод
    /// сброшен, и клавиша оттуда не видна никогда.
    /// </remarks>
    private static void InstallHotkey()
    {
        SetHotkeyStep(install: true);
    }

    private static void UninstallHotkey()
    {
        SetHotkeyStep(install: false);
    }

    private static void SetHotkeyStep(bool install)
    {
        PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
        PlayerLoopSystem[] phases = loop.subSystemList;

        if (phases == null)
            return;

        for (int index = 0; index < phases.Length; index++)
        {
            if (phases[index].type != typeof(Update))
                continue;

            var steps = new List<PlayerLoopSystem>(phases[index].subSystemList ?? Array.Empty<PlayerLoopSystem>());
            steps.RemoveAll(step => step.type == typeof(ScreenshotHotkeyStep));

            if (install)
            {
                steps.Add(new PlayerLoopSystem
                {
                    type = typeof(ScreenshotHotkeyStep),
                    updateDelegate = PollHotkey,
                });
            }

            phases[index].subSystemList = steps.ToArray();
            loop.subSystemList = phases;
            PlayerLoop.SetPlayerLoop(loop);
            return;
        }
    }

    private static void PollHotkey()
    {
        if (!hotkeyEnabled || !Application.isPlaying)
            return;

        try
        {
            if (Input.GetKeyDown(hotkey))
                Capture();
        }
        catch (InvalidOperationException exception)
        {
            // Проект переведён на новую систему ввода — старый Input бросает на каждом вызове.
            UninstallHotkey();
            Debug.LogWarning($"Screenshot hotkey is unavailable: {exception.Message}");
        }
    }

    #endregion

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Screenshot";

        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return value.Replace(' ', '_');
    }
}
