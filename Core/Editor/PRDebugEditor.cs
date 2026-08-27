using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class PRDebugEditor : ExtendedEditorWindow
{
    private const string AutoRefreshKey = "PRDebugEditor.AutoRefresh";
    private const string RefreshIntervalKey = "PRDebugEditor.RefreshInterval";
    private const float CompactLayoutWidth = 620f;
    private const float WideContentMinWidth = 700f;
    private const float CompactContentMinWidth = 260f;
    private const float ContentMaxWidth = 1000f;
    private const int EventHistoryCapacity = 200;

    /// <summary>
    /// События, которые агрегируются отдельно и не занимают обычный ring buffer.
    /// Добавляйте сюда другие покадровые event-интерфейсы.
    /// </summary>
    private static readonly HashSet<Type> AggregatedEventTypes = new()
    {
        typeof(IOnUpdateEvent),
        typeof(IOnPRUpdateEvent),
        typeof(IOnRealSecondsEvent),
        typeof(IOnGameSecondsEvent),
        typeof(IPauseStateListener)
    };

    private readonly List<PRDebugProblem> problems = new();
    private readonly List<PRDebugProblem> healthCheckLoadErrors = new();
    private readonly List<IPRDebugHealthCheck> healthChecks = new();
    private readonly List<PlayerRow> players = new();
    private readonly List<EntityRow> entities = new();
    private readonly List<EntityInstanceRow> entityInstances = new();
    private readonly List<PoolSystemTableData> pools = new();
    private readonly List<FlagResolverRow> flagResolvers = new();
    private readonly List<FlagProviderRow> flagProviders = new();
    private readonly List<InitializationRow> initializationEntries = new();
    private readonly List<MonoWindowRow> monoWindows = new();
    private readonly List<BackgroundTaskRow> backgroundTasks = new();
    private readonly List<StatRuleRow> statRules = new();
    private readonly List<TimeScaleRow> timeScaleRows = new();
    private readonly Queue<EventBusRow> eventHistory = new();
    private readonly List<EventBusRow> eventRows = new();
    private readonly Dictionary<Type, EventBusAccumulator> aggregatedEventHistory = new();
    private readonly List<AggregatedEventBusRow> aggregatedEventRows = new();
    private readonly object eventHistoryLock = new();
    private readonly object debugFlagSource = new();

    private Vector2 scroll;
    private string search = string.Empty;

    /// <summary>
    /// Прокрутка списка типов сущностей.
    /// </summary>
    /// <remarks>
    /// Своя у каждого из двух списков вкладки Entities: тип выбирают в верхнем, а смотрят
    /// на экземпляры в нижнем, и общая прокрутка увела бы один из них за край экрана.
    /// </remarks>
    private Vector2 entityTypeScroll;

    /// <summary>
    /// Прокрутка списка экземпляров сущностей.
    /// </summary>
    private Vector2 entityInstanceScroll;

    /// <summary>
    /// Типы сущностей, у которых раскрыта разбивка по видам предметов.
    /// </summary>
    private readonly HashSet<string> expandedEntityTypes = new();

    /// <summary>
    /// Характеристика, выбранная для проверки правил на вкладке Rules.
    /// </summary>
    private int ruleTestStatIndex;

    /// <summary>
    /// Значение, которое прогоняется через правила выбранной характеристики.
    /// </summary>
    private float ruleTestValue = 1f;

    private string snapshotError;
    private bool autoRefresh = true;
    private double refreshInterval = 1d;
    private double nextRefresh;
    private DateTime lastRefreshUtc;
    private PauseSnapshot pause;
    private int humanCount;
    private int aiCount;
    private long entityTotal;
    private long entityOnScene;
    private long entityInPool;
    private GameSaveState saveState;
    private DateTime? saveCreationTimeUtc;
    private DateTime? lastSaveTimeUtc;
    private bool hasLoadedSave;
    private bool canStartSave;
    private long saveCooldownRemainingSeconds;
    private string timeScaleCombineMode = "-";
    private int timeScaleSubscriberCount;
    private bool hasActiveTemporaryTimeScales;
    private bool globalTemporaryTimeScaleActive;
    private float temporaryGlobalTimeScale = 0.5f;
    private float temporaryGlobalDurationSeconds = 8f;
    private double initializationTotalMilliseconds;
    private int selectedFlagProviderIndex;
    private int selectedFlagIndex;
    private volatile bool captureEvents = true;
    private volatile bool eventHistoryDirty;
    private long eventSequence;

    [MenuItem("PRUnitySDK/Tools/Debug Window")]
    public static void ShowWindow()
    {
        var window = GetWindow<PRDebugEditor>("PRUnitySDK Debug");
        window.minSize = new Vector2(280f, 360f);
    }

    private void OnEnable()
    {
        autoRefresh = SessionState.GetBool(AutoRefreshKey, true);
        refreshInterval = SessionState.GetFloat(RefreshIntervalKey, 1f);
        DiscoverHealthChecks();
        EventBus.OnEventRaised += OnEventBusRaised;
        EditorApplication.update += AutoRefresh;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        RefreshSnapshot();
    }

    private void OnDisable()
    {
        ClearDebugFlags(false);
        EventBus.OnEventRaised -= OnEventBusRaised;
        EditorApplication.update -= AutoRefresh;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        SessionState.SetBool(AutoRefreshKey, autoRefresh);
        SessionState.SetFloat(RefreshIntervalKey, (float)refreshInterval);
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            RefreshSnapshot();
    }

    private void AutoRefresh()
    {
        if (eventHistoryDirty)
        {
            eventHistoryDirty = false;
            eventRows.Clear();
            aggregatedEventRows.Clear();
            CaptureEventHistory();
            Repaint();
        }

        if (!autoRefresh || EditorApplication.timeSinceStartup < nextRefresh)
            return;

        RefreshSnapshot();
        Repaint();
    }

    private void OnGUI()
    {
        bool compact = position.width < CompactLayoutWidth;
        DrawToolbar(compact);

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Runtime diagnostics are available in Play Mode.", MessageType.Info);
            return;
        }

        if (!PRUnitySDK.IsInitialized)
            EditorGUILayout.HelpBox("PRUnitySDK is not initialized yet.", MessageType.Warning);

        if (!string.IsNullOrEmpty(snapshotError))
            EditorGUILayout.HelpBox(snapshotError, MessageType.Error);

        var tabs = new (string name, Action draw)[]
        {
            ("Overview", DrawOverview),
            ($"Problems ({problems.Count})", DrawProblems),
            ($"Initialization ({initializationEntries.Count})", DrawInitialization),
            ($"Windows ({monoWindows.Count})", DrawMonoWindows),
            ($"Tasks ({backgroundTasks.Count})", DrawBackgroundTasks),
            ($"Rules ({statRules.Count})", DrawGameRules),
            ($"Events ({eventRows.Count})", DrawEvents),
            ($"Players ({players.Count})", DrawPlayers),
            ($"Entities ({entityInstances.Count})", DrawEntities),
            ($"Pools ({pools.Count})", DrawPools),
            ($"Flags ({flagResolvers.Count})", DrawFlags)
        };

        // Вкладок больше десятка: ряд кнопок при такой ширине либо переносится на
        // несколько строк, либо сжимается до нечитаемого размера, поэтому раздел
        // всегда выбирается списком.
        DrawTabsDropdown(tabs);

        bool tableView = SelectedTabIndex != 0;

        // Вкладка Entities делит высоту между двумя своими списками и прокручивает их
        // сама: внешняя вертикальная прокрутка поверх этого дала бы две полосы подряд.
        bool ownsVerticalScroll = SelectedTabIndex >= 0 && SelectedTabIndex < tabs.Length
                                  && tabs[SelectedTabIndex].draw == DrawEntities;

        scroll = EditorGUILayout.BeginScrollView(scroll, tableView, !ownsVerticalScroll);
        if (!compact)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
        }

        float availableWidth = Mathf.Max(CompactContentMinWidth, position.width - 28f);
        float contentWidth = tableView
            ? compact
                ? CompactContentMinWidth
                : Mathf.Clamp(availableWidth, WideContentMinWidth, ContentMaxWidth)
            : Mathf.Min(availableWidth, ContentMaxWidth);
        EditorGUILayout.BeginVertical(compact && tableView
            ? GUILayout.MinWidth(contentWidth)
            : GUILayout.Width(contentWidth));
        DrawSelectedTab(tabs);
        EditorGUILayout.EndVertical();

        if (!compact)
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar(bool compact)
    {
        if (compact)
        {
            DrawCompactToolbar();
            return;
        }

        CreateHorizontalToolBar(() =>
        {
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(65f)))
                RefreshSnapshot();

            autoRefresh = GUILayout.Toggle(autoRefresh, "Auto", EditorStyles.toolbarButton, GUILayout.Width(45f));
            GUILayout.Label("Interval", GUILayout.Width(45f));
            refreshInterval = Math.Max(0.1d, EditorGUILayout.DoubleField(refreshInterval, GUILayout.Width(42f)));
            GUILayout.Space(8f);

            search = GUILayout.TextField(search ?? string.Empty, EditorStyles.toolbarSearchField,
                GUILayout.MinWidth(140f), GUILayout.MaxWidth(320f));
            if (!string.IsNullOrEmpty(search) && GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(22f)))
                search = string.Empty;

            GUILayout.FlexibleSpace();
            string mode = EditorApplication.isPlaying
                ? (EditorApplication.isPaused ? "PLAY • PAUSED" : "PLAY")
                : "EDIT";
            GUILayout.Label(mode, EditorStyles.miniBoldLabel, GUILayout.Width(90f));
            if (lastRefreshUtc != default)
                GUILayout.Label(lastRefreshUtc.ToLocalTime().ToString("HH:mm:ss"), GUILayout.Width(55f));
        });
    }

    private void DrawCompactToolbar()
    {
        CreateHorizontalToolBar(() =>
        {
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(58f)))
                RefreshSnapshot();

            autoRefresh = GUILayout.Toggle(autoRefresh, "Auto", EditorStyles.toolbarButton, GUILayout.Width(42f));
            GUILayout.Label("s", EditorStyles.miniLabel, GUILayout.Width(10f));
            refreshInterval = Math.Max(0.1d, EditorGUILayout.DoubleField(refreshInterval, GUILayout.Width(42f)));

            GUILayout.FlexibleSpace();
            string mode = EditorApplication.isPlaying
                ? (EditorApplication.isPaused ? "PAUSED" : "PLAY")
                : "EDIT";
            GUILayout.Label(mode, EditorStyles.miniBoldLabel, GUILayout.Width(48f));
        });

        CreateHorizontalToolBar(() =>
        {
            search = GUILayout.TextField(search ?? string.Empty, EditorStyles.toolbarSearchField,
                GUILayout.MinWidth(80f));
            if (!string.IsNullOrEmpty(search) && GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(22f)))
                search = string.Empty;
        });
    }
}
