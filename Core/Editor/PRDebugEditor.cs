using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class PRDebugEditor : ExtendedEditorWindow
{
    /// <summary>
    /// Интервал автообновления окна в секундах.
    /// </summary>
    private const double RefreshIntervalSeconds = 1.0;

    private double nextUpdate;
    private bool showPlayerList = true;
    private bool showEntityDetails = true;

    [MenuItem("PRUnitySDK/Debug window")]
    public static void ShowWindow()
    {
        GetWindow<PRDebugEditor>("Debug window");
    }

    private void OnEnable()
    {
        nextUpdate = EditorApplication.timeSinceStartup + RefreshIntervalSeconds;
        EditorApplication.update += AutoRefresh;
        //UpdateStateGame();
    }

    private void OnDisable()
    {
        // Без отписки делегат продолжает держать ссылку на уничтоженное окно
        // после его закрытия — Repaint() будет дёргаться на мёртвом объекте до перезагрузки домена.
        EditorApplication.update -= AutoRefresh;
    }

    private void AutoRefresh()
    {
        if (EditorApplication.timeSinceStartup < nextUpdate)
            return;

        nextUpdate = EditorApplication.timeSinceStartup + RefreshIntervalSeconds;
        Repaint();
    }

    private void OnGUI()
    {
        if (!PRUnitySDK.IsInitialized)
        {
            EditorGUILayout.LabelField("PRUnitySDK еще не инициализирован.");
            return;
        }

        DrawPauseState();
        DrawPlayersInfo();
        DrawEntitiesInfo();
        DrawPoolSystem();
    }

    #region Общие хелперы отрисовки таблиц

    /// <summary>
    /// Рисует одну строку таблицы из пар (значение, ширина), не дублируя вызовы EditorGUILayout на каждую колонку.
    /// </summary>
    private static void DrawTableRow(params (string value, float width)[] columns)
    {
        EditorGUILayout.BeginHorizontal();
        foreach (var (value, width) in columns)
            EditorGUILayout.LabelField(value, GUILayout.Width(width));
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Рисует одну строку таблицы с иконкой в первой колонке.
    /// </summary>
    private static void DrawTableRowWithIcon(Texture icon, params (string value, float width)[] columns)
    {
        GUILayout.BeginHorizontal();
        if (icon != null)
            GUILayout.Label(icon, GUILayout.Width(16), GUILayout.Height(16));
        foreach (var (value, width) in columns)
            EditorGUILayout.LabelField(value, GUILayout.Width(width));
        GUILayout.EndHorizontal();
    }

    #endregion

    private void DrawPoolSystem()
    {
        EditorGUILayout.LabelField("PoolSystem", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        DrawTableRow(
            ("Root", 150), ("Категория", 150),
            ("Всего", 100), ("На сцене", 100), ("Спрятанных", 100));

        foreach (var item in PRUnitySDK.Managers.ObjectPool.GenerateReport())
        {
            DrawTableRow(
                (item.Type, 150), (item.Category, 150),
                (item.TotalCount.ToString(), 100),
                (item.ShowCount.ToString(), 100),
                (item.HideCount.ToString(), 100));
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPauseState()
    {
        GUILayout.Label("Состояния паузы:");
        GUI.enabled = false; // Делаем UI неактивным
        GUILayout.Toggle(PRUnitySDK.PauseManager.IsProjectPaused, nameof(PauseManager.IsProjectPaused));
        GUILayout.Toggle(PRUnitySDK.PauseManager.IsLogicPaused, nameof(PauseManager.IsLogicPaused));
        GUILayout.Toggle(PRUnitySDK.PauseManager.IsFocusPaused, nameof(PauseManager.IsFocusPaused));
        GUILayout.Toggle(PRUnitySDK.PauseManager.IsMusicPaused, nameof(PauseManager.IsMusicPaused));
        GUILayout.Toggle(PRUnitySDK.PauseManager.IsTutorialPaused, nameof(PauseManager.IsTutorialPaused));
        GUILayout.Toggle(PRUnitySDK.PauseManager.IsCutScenePaused, nameof(PauseManager.IsCutScenePaused));
        GUI.enabled = true; // Включаем UI обратно
    }

    private void DrawPlayersInfo()
    {
        var tracker = PRUnitySDK.Trackers.Players;
        EditorGUILayout.LabelField("Players", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Людей:", tracker.HumanCount.ToString());
        EditorGUILayout.LabelField("Ботов:", tracker.AICount.ToString());
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        //EditorGUILayout.LabelField("Живых:", gameSessionManager.PlayerTracker.AliveCount.ToString());
        //EditorGUILayout.LabelField("Мертвых:", gameSessionManager.PlayerTracker.DeadCount.ToString());
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        showPlayerList = EditorGUILayout.Foldout(showPlayerList, $"Player List ({tracker.PlayersCount})");
        if (!showPlayerList)
            return;

        var players = tracker.Players;
        EditorGUILayout.BeginVertical("box");

        DrawTableRow(
            ("HumanId", 80), ("Имя", 150), ("Команда", 100),
            ("Очков", 100), ("Убийств", 60), ("Смертей", 60),
            ("Статус", 100), ("Действие", 70));

        foreach (var player in players)
        {
            // GetIcon()/PlayerTeam могут отсутствовать у некорректно настроенного игрока —
            // защищаемся от NRE, чтобы одна битая запись не ломала всё окно.
            var icon = player.Info?.GetIcon().texture;
            var teamName = player.PlayerTeam != null ? player.PlayerTeam.Name : "-";

            //string isAliveStatus = player.IsAlive ? L.Tr(PlayerTranslateKeys.ALIVE_KEY) : L.Tr(PlayerTranslateKeys.DEAD_KEY);
            //string isBot = player.PlayerType == PlayerType.AI ? L.Tr(PlayerTranslateKeys.BOT_KEY) : L.Tr(PlayerTranslateKeys.HUMAN_KEY);

            DrawTableRowWithIcon(icon,
                ($"{player.HumanId}", 58),
                (player.Info?.GetName() ?? "-", 150),
                (teamName, 100),
                (player.Points.ToString(), 100),
                (player.Kills.ToString(), 60),
                (player.Deaths.ToString(), 60));

            //if (!player.IsAlive && GUILayout.Button(L.Tr(PlayerTranslateKeys.REVIVE_KEY), GUILayout.Width(70)))
            //{
            //    player.Revive();
            //}
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawEntitiesInfo()
    {
        EditorGUILayout.LabelField("Entities", EditorStyles.boldLabel);
        var tracker = PRUnitySDK.Trackers.Entities;
        var existsEntities = tracker.GetExistsEntityCount();
        var onSceneEntities = tracker.GetEntityOnSceneCount();
        var onPoolEntities = tracker.GetEntityInPoolCount();
        var hideEntities = existsEntities - onSceneEntities;

        EditorGUILayout.BeginVertical("box");

        DrawTableRow(
            ("Всего сущностей", 170), ("На сцене", 70),
            ("Спрятанных", 100), ("Спрятанных в pool", 170));

        DrawTableRow(
            (existsEntities.ToString(), 170), (onSceneEntities.ToString(), 70),
            (hideEntities.ToString(), 100), (onPoolEntities.ToString(), 170));

        showEntityDetails = EditorGUILayout.Foldout(showEntityDetails, $"EntityDetails ({tracker.GetEntitiesCount()})");
        if (showEntityDetails)
        {
            DrawTableRow(
                ("Тип сущности", 170), ("Всего", 70), ("На сцене", 70),
                ("Спрятанных", 100), ("Спрятанных в pool", 170));

            var icon = EditorGUIUtility.IconContent("d_Prefab Icon").image;
            var entityDetails = tracker.RegisteredEntity.OrderByDescending(x => x.Value);

            foreach (var entity in entityDetails)
            {
                var onSceneEntity = tracker.GetExactEntityOnSceneCount(entity.Key);
                var inPoolEntity = tracker.GetExactEntityInPoolCount(entity.Key);
                var hideEntity = entity.Value - onSceneEntity;

                DrawTableRowWithIcon(icon,
                    (entity.Key.ToString(), 150),
                    (entity.Value.ToString(), 70),
                    (onSceneEntity.ToString(), 70),
                    (hideEntity.ToString(), 100),
                    (inPoolEntity.ToString(), 170));
            }
        }

        EditorGUILayout.EndVertical();
    }
}