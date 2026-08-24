using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class PRDebugEditor : ExtendedEditorWindow
{
    private const string AutoRefreshKey = "PRDebugEditor.AutoRefresh";
    private const string RefreshIntervalKey = "PRDebugEditor.RefreshInterval";

    private readonly List<PlayerRow> players = new();
    private readonly List<EntityRow> entities = new();
    private readonly List<PoolSystemTableData> pools = new();
    private readonly List<FlagResolverRow> flagResolvers = new();
    private readonly List<InitializationRow> initializationEntries = new();

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

    [MenuItem("PRUnitySDK/Tools/Debug Window")]
    public static void ShowWindow()
    {
        var window = GetWindow<PRDebugEditor>("PRUnitySDK Debug");
        window.minSize = new Vector2(720f, 360f);
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
        DrawToolbar();

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

        scroll = EditorGUILayout.BeginScrollView(scroll);
        Tabs(
            ("Overview", DrawOverview),
            ($"Initialization ({initializationEntries.Count})", DrawInitialization),
            ($"Players ({players.Count})", DrawPlayers),
            ($"Entities ({entityTotal})", DrawEntities),
            ($"Pools ({pools.Count})", DrawPools),
            ($"Flags ({flagResolvers.Count})", DrawFlags));
        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
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
}
