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
    private const string AllLanguagesKey = "PRUnitySDK.Screenshots.AllLanguages";
    private const string LanguageDelayKey = "PRUnitySDK.Screenshots.LanguageDelay";
    private const string MobileVersionKey = "PRUnitySDK.Screenshots.MobileVersion";

    /// <summary>
    /// Наибольший множитель размера: дальше снимок 1080p весит сотни мегабайт в памяти.
    /// </summary>
    public const int MaxScale = 4;

    /// <summary>
    /// Границы паузы между сменой языка и снимком, секунды.
    /// </summary>
    public const float MinLanguageDelay = 0.1f;

    /// <inheritdoc cref="MinLanguageDelay"/>
    public const float MaxLanguageDelay = 3f;

    /// <summary>
    /// Сколько кадров интерфейсу даётся на перестроение после смены языка.
    /// </summary>
    /// <remarks>
    /// Поверх паузы: на паузе игры или при просевшем кадре секунды идут, а кадры — нет.
    /// Текст меняется сразу, но разметка под новую длину строк пересчитывается кадром позже.
    /// </remarks>
    private const int LanguageSettleFrames = 2;

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
    /// Снимать кадр на каждом языке игры по очереди.
    /// </summary>
    /// <remarks>
    /// Работает только во время игры: язык переключает она сама. Вне игры снимается
    /// один кадр, как без галки.
    /// </remarks>
    public static bool AllLanguages
    {
        get => EditorPrefs.GetBool(AllLanguagesKey, false);
        set => EditorPrefs.SetBool(AllLanguagesKey, value);
    }

    /// <summary>
    /// Снимать кадр дважды: как на компьютере и с мобильным управлением на экране.
    /// </summary>
    /// <remarks>
    /// Работает только во время игры и только если в проекте есть способ показать мобильный
    /// вид (<see cref="IPRScreenshotMobileMode"/>). Вместе с <see cref="AllLanguages"/> даёт
    /// по снимку на каждый язык для обоих видов.
    /// </remarks>
    public static bool MobileVersion
    {
        get => EditorPrefs.GetBool(MobileVersionKey, false);
        set => EditorPrefs.SetBool(MobileVersionKey, value);
    }

    /// <summary>
    /// В проекте есть способ показать игру как на телефоне.
    /// </summary>
    public static bool HasMobileMode => GetMobileModes().Count > 0;

    /// <summary>
    /// Пауза между сменой языка или вида и снимком, секунды.
    /// </summary>
    /// <remarks>
    /// Настоящего времени, а не игрового: окно, ради которого снимают, обычно ставит игру
    /// на паузу. Нужна тому, что догоняет язык не сразу, — анимациям появления текста,
    /// картинкам с подписями, пересчёту списков.
    /// </remarks>
    public static float LanguageDelay
    {
        get => Mathf.Clamp(EditorPrefs.GetFloat(LanguageDelayKey, 0.5f), MinLanguageDelay, MaxLanguageDelay);
        set => EditorPrefs.SetFloat(LanguageDelayKey, Mathf.Clamp(value, MinLanguageDelay, MaxLanguageDelay));
    }

    /// <summary>
    /// Идёт серия снимков: по языкам, по видам или по тем и другим.
    /// </summary>
    public static bool IsCapturingSeries => series != null;

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
    /// <returns>
    /// Путь к файлу, который будет записан; <c>null</c>, если запущена серия —
    /// файлов в ней несколько, и появляются они по ходу серии.
    /// </returns>
    public static string Capture()
    {
        // Повторный запрос посреди серии сбил бы её: языки и виды ещё переключаются.
        if (series != null)
            return null;

        if ((AllLanguages || MobileVersion) && CanCaptureSeries() && TryStartSeries())
            return null;

        return CaptureFrame(CreateStamp(), null);
    }

    /// <summary>
    /// Снимает текущий кадр в файл.
    /// </summary>
    /// <param name="stamp">Метка времени в имени файла; у снимков одной серии она общая.</param>
    /// <param name="tag">Пометка в конце имени — вид и язык кадра серии; пусто — без неё.</param>
    private static string CaptureFrame(string stamp, string tag)
    {
        string folder = Folder;
        Directory.CreateDirectory(folder);

        Vector2Int size = GameViewSize * Scale;
        string suffix = string.IsNullOrEmpty(tag) ? string.Empty : $"_{tag}";
        string name = $"{Sanitize(Application.productName)}_{stamp}_{size.x}x{size.y}{suffix}.png";
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
        {
            UninstallHotkey();

            // Серию обрывает выход из игры: язык и вид возвращаются, пока игра ещё жива.
            FinishSeries();
        }
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

    #region Серия снимков

    /// <summary>
    /// Один кадр серии: на каком виде и языке его снимать.
    /// </summary>
    private readonly struct SeriesShot
    {
        public readonly bool Mobile;

        /// <summary>Код языка; <c>null</c> — язык не меняется.</summary>
        public readonly string Language;

        public SeriesShot(bool mobile, string language)
        {
            Mobile = mobile;
            Language = language;
        }
    }

    /// <summary>
    /// Состояние серии: какой кадр сейчас на экране и чего ждём.
    /// </summary>
    private sealed class CaptureSeries
    {
        public string OriginalLanguage;
        public string Stamp;
        public List<SeriesShot> Shots;
        public int Index = -1;
        public SeriesShot Current;
        public bool HasMobileShots;
        public bool MobileActive;
        public double CaptureTime;
        public int CaptureFrame;
        public bool Captured;
    }

    private static CaptureSeries series;
    private static List<IPRScreenshotMobileMode> mobileModes;

    /// <summary>
    /// Способы показать мобильный вид, найденные в проекте.
    /// </summary>
    /// <remarks>
    /// Ищутся по типу, как вкладки окна отладки: инструмент лежит в ядре и не знает, из чего
    /// в игре собрано сенсорное управление.
    /// </remarks>
    private static List<IPRScreenshotMobileMode> GetMobileModes()
    {
        if (mobileModes != null)
            return mobileModes;

        mobileModes = new List<IPRScreenshotMobileMode>();

        foreach (Type type in TypeCache.GetTypesDerivedFrom<IPRScreenshotMobileMode>())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            try
            {
                if (Activator.CreateInstance(type) is IPRScreenshotMobileMode mode)
                    mobileModes.Add(mode);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Screenshot: mobile mode {type.Name} is unavailable: {exception.Message}");
            }
        }

        return mobileModes;
    }

    private static bool CanCaptureSeries()
    {
        if (!EditorApplication.isPlaying)
            return false;

        // На паузе редактора кадры не идут: серия встала бы на первом же кадре.
        if (EditorApplication.isPaused)
        {
            Debug.LogWarning("Screenshot: editor is paused, only the current frame is captured.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Составляет серию и показывает её первый кадр.
    /// </summary>
    /// <returns><c>false</c> — снимать нечего, кроме текущего кадра: серия не начата.</returns>
    private static bool TryStartSeries()
    {
        bool languages = AllLanguages && PRUnitySDK.LanguageManager != null;
        bool mobile = MobileVersion && HasMobileMode;

        var codes = new List<string>();

        if (languages)
        {
            foreach (LangType language in Enum.GetValues(typeof(LangType)))
                codes.Add(LocalizationUtils.GetLanguageCode(language));
        }
        else
            codes.Add(null);

        // Сначала все языки для компьютера, потом все для телефона: вид меняется один раз.
        var shots = new List<SeriesShot>();

        foreach (string code in codes)
            shots.Add(new SeriesShot(false, code));

        if (mobile)
        {
            foreach (string code in codes)
                shots.Add(new SeriesShot(true, code));
        }

        if (shots.Count <= 1)
            return false;

        series = new CaptureSeries
        {
            OriginalLanguage = languages ? PRUnitySDK.CurrentLang : null,
            Stamp = CreateStamp(),
            Shots = shots,
            HasMobileShots = mobile,
        };

        EditorApplication.update += UpdateSeries;
        ShowNextShot();
        return true;
    }

    /// <summary>
    /// Переключает игру на следующий кадр серии и назначает момент снимка.
    /// </summary>
    private static void ShowNextShot()
    {
        while (++series.Index < series.Shots.Count)
        {
            SeriesShot shot = series.Shots[series.Index];

            SetMobile(shot.Mobile);

            if (shot.Language != null)
            {
                PRUnitySDK.LanguageManager.SwitchLang(shot.Language);

                // Менеджер вправе отказать: язык в проекте не подключён. Снимать тогда нечего —
                // вышел бы второй кадр прежнего языка под чужим именем.
                if (PRUnitySDK.CurrentLang != shot.Language)
                {
                    Debug.LogWarning($"Screenshot: language '{shot.Language}' is unavailable, skipped.");
                    continue;
                }
            }

            series.Current = shot;
            series.Captured = false;
            series.CaptureTime = EditorApplication.timeSinceStartup + LanguageDelay;
            series.CaptureFrame = Time.frameCount + LanguageSettleFrames;
            return;
        }

        FinishSeries();
    }

    private static void UpdateSeries()
    {
        if (series == null || !EditorApplication.isPlaying)
        {
            FinishSeries();
            return;
        }

        if (!series.Captured)
        {
            if (EditorApplication.timeSinceStartup < series.CaptureTime || Time.frameCount < series.CaptureFrame)
                return;

            CaptureFrame(series.Stamp, GetTag(series.Current));
            series.Captured = true;
            series.CaptureFrame = Time.frameCount;
            return;
        }

        // Unity пишет файл в конце кадра, а запрос приходит из обновления редактора — между
        // кадрами игры. Кадр меняется, только когда заказанный точно снят.
        if (Time.frameCount > series.CaptureFrame + 1)
            ShowNextShot();
    }

    /// <summary>
    /// Пометка кадра в имени файла: вид и язык.
    /// </summary>
    /// <remarks>
    /// Вид пишется, только когда в серии есть оба: одиночной серии по языкам хватает кода языка.
    /// </remarks>
    private static string GetTag(SeriesShot shot)
    {
        string device = series.HasMobileShots ? (shot.Mobile ? "mobile" : "pc") : null;

        if (device == null)
            return shot.Language;

        return shot.Language == null ? device : $"{device}_{shot.Language}";
    }

    /// <summary>
    /// Включает и выключает мобильный вид, если он ещё не такой.
    /// </summary>
    private static void SetMobile(bool isMobile)
    {
        if (series.MobileActive == isMobile)
            return;

        series.MobileActive = isMobile;

        foreach (IPRScreenshotMobileMode mode in GetMobileModes())
        {
            try
            {
                if (isMobile)
                    mode.Enter();
                else
                    mode.Exit();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Screenshot: {mode.GetType().Name} failed: {exception.Message}");
            }
        }
    }

    /// <summary>
    /// Завершает серию и возвращает игре вид и язык, с которых она началась.
    /// </summary>
    private static void FinishSeries()
    {
        EditorApplication.update -= UpdateSeries;

        if (series == null)
            return;

        SetMobile(false);

        string original = series.OriginalLanguage;
        series = null;

        if (EditorApplication.isPlaying && PRUnitySDK.LanguageManager != null && !string.IsNullOrEmpty(original))
            PRUnitySDK.LanguageManager.SwitchLang(original);
    }

    #endregion

    private static string CreateStamp() => $"{DateTime.Now:yyyyMMdd_HHmmss_fff}";

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Screenshot";

        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');

        return value.Replace(' ', '_');
    }
}
