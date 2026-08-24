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

    private readonly List<PlayerRow> players = new();
    private readonly List<EntityRow> entities = new();
    private readonly List<EntityInstanceRow> entityInstances = new();
    private readonly List<PoolSystemTableData> pools = new();
    private readonly List<FlagResolverRow> flagResolvers = new();
    private readonly List<FlagProviderRow> flagProviders = new();
    private readonly List<InitializationRow> initializationEntries = new();
    private readonly object debugFlagSource = new();

    private Vector2 scroll;
    private string search = string.Empty;
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
    private double initializationTotalMilliseconds;
    private int selectedFlagProviderIndex;
    private int selectedFlagIndex;

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
        EditorApplication.update += AutoRefresh;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        RefreshSnapshot();
    }

    private void OnDisable()
    {
        ClearDebugFlags(false);
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
        {
            EditorGUILayout.HelpBox("PRUnitySDK is not initialized yet.", MessageType.Warning);
            return;
        }

        if (!string.IsNullOrEmpty(snapshotError))
            EditorGUILayout.HelpBox(snapshotError, MessageType.Error);

        var tabs = new (string name, Action draw)[]
        {
            ("Overview", DrawOverview),
            ($"Initialization ({initializationEntries.Count})", DrawInitialization),
            ($"Players ({players.Count})", DrawPlayers),
            ($"Entities ({entityInstances.Count})", DrawEntities),
            ($"Pools ({pools.Count})", DrawPools),
            ($"Flags ({flagResolvers.Count})", DrawFlags)
        };

        DrawTabsHeader(compact, tabs);

        bool tableView = SelectedTabIndex != 0;
        scroll = EditorGUILayout.BeginScrollView(scroll, tableView, true);
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
