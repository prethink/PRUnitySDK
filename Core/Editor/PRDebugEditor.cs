using System.Linq;
using UnityEditor;
using UnityEngine;

public partial class PRDebugEditor : ExtendedEditorWindow
{
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
        nextUpdate = EditorApplication.timeSinceStartup + 1.0;
        EditorApplication.update += AutoRefresh;
        //UpdateStateGame();
    }

    private void AutoRefresh()
    {
        if (EditorApplication.timeSinceStartup >= nextUpdate)
        {
            nextUpdate = EditorApplication.timeSinceStartup + 1.0;
            Repaint();
        }
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

    private void DrawPoolSystem()
    {
        EditorGUILayout.LabelField("PoolSystem", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Root", GUILayout.Width(150));
        EditorGUILayout.LabelField("Категория", GUILayout.Width(150));
        EditorGUILayout.LabelField("Всего", GUILayout.Width(100));
        EditorGUILayout.LabelField("На сцене", GUILayout.Width(100));
        EditorGUILayout.LabelField("Спрятанных", GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
        foreach (var item in PRUnitySDK.Managers.ObjectPool.GenerateReport())
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(item.Type, GUILayout.Width(150));
            EditorGUILayout.LabelField(item.Category, GUILayout.Width(150));
            EditorGUILayout.LabelField(item.TotalCount.ToString(), GUILayout.Width(100));
            EditorGUILayout.LabelField(item.ShowCount.ToString(), GUILayout.Width(100));
            EditorGUILayout.LabelField(item.HideCount.ToString(), GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
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
        if (showPlayerList)
        {
            var players = tracker.Players;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("HumanId", GUILayout.Width(80));
            EditorGUILayout.LabelField("Имя", GUILayout.Width(150));
            EditorGUILayout.LabelField("Команда", GUILayout.Width(100));
            EditorGUILayout.LabelField("Очков", GUILayout.Width(100));
            EditorGUILayout.LabelField("Убийств", GUILayout.Width(60));
            EditorGUILayout.LabelField("Смертей", GUILayout.Width(60));
            EditorGUILayout.LabelField("Статус", GUILayout.Width(100));
            EditorGUILayout.LabelField("Действие", GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            foreach (var player in players)
            {
                Texture icon = player.Info.GetIcon().texture;
                //string isAliveStatus = player.IsAlive ? L.Tr(PlayerTranslateKeys.ALIVE_KEY) : L.Tr(PlayerTranslateKeys.DEAD_KEY);
                //string isBot = player.PlayerType == PlayerType.AI ? L.Tr(PlayerTranslateKeys.BOT_KEY) : L.Tr(PlayerTranslateKeys.HUMAN_KEY);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(icon, GUILayout.Width(16), GUILayout.Height(16));
                EditorGUILayout.LabelField($"{player.HumanId}", GUILayout.Width(58));
                EditorGUILayout.LabelField(player.Info.GetName(), GUILayout.Width(150));
                EditorGUILayout.LabelField(player.PlayerTeam.Name, GUILayout.Width(100));
                EditorGUILayout.LabelField(player.Points.ToString(), GUILayout.Width(100));
                EditorGUILayout.LabelField(player.Kills.ToString(), GUILayout.Width(60));
                EditorGUILayout.LabelField(player.Deaths.ToString(), GUILayout.Width(60));
                //EditorGUILayout.LabelField(isAliveStatus, GUILayout.Width(100));
                //if (!player.IsAlive && GUILayout.Button(L.Tr(PlayerTranslateKeys.REVIVE_KEY), GUILayout.Width(70)))
                //{
                //    player.Revive();
                //}
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
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
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Всего сущностей", GUILayout.Width(170));
        EditorGUILayout.LabelField("На сцене", GUILayout.Width(70));
        EditorGUILayout.LabelField("Спрятанных", GUILayout.Width(100));
        EditorGUILayout.LabelField("Спрятанных в pool", GUILayout.Width(170));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(existsEntities.ToString(), GUILayout.Width(170));
        EditorGUILayout.LabelField(onSceneEntities.ToString(), GUILayout.Width(70));
        EditorGUILayout.LabelField(hideEntities.ToString(), GUILayout.Width(100));
        EditorGUILayout.LabelField(onPoolEntities.ToString(), GUILayout.Width(170));
        EditorGUILayout.EndHorizontal();

        showEntityDetails = EditorGUILayout.Foldout(showEntityDetails, $"EntityDetails ({tracker.GetEntitiesCount()})");
        if (showEntityDetails)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Тип сущности", GUILayout.Width(170));
            EditorGUILayout.LabelField("Всего", GUILayout.Width(70));
            EditorGUILayout.LabelField("На сцене", GUILayout.Width(70));
            EditorGUILayout.LabelField("Спрятанных", GUILayout.Width(100));
            EditorGUILayout.LabelField("Спрятанных в pool", GUILayout.Width(170));
            EditorGUILayout.EndHorizontal();

            var entityDetails = tracker.RegisteredEntity.OrderByDescending(x => x.Value);
            foreach (var entity in entityDetails)
            {
                var onSceneEntity = tracker.GetExactEntityOnSceneCount(entity.Key);
                var inPoolEntity = tracker.GetExactEntityInPoolCount(entity.Key);
                var hideEntity = entity.Value - onSceneEntity;

                Texture icon = EditorGUIUtility.IconContent("d_Prefab Icon").image;
                GUILayout.BeginHorizontal();
                GUILayout.Label(icon, GUILayout.Width(16), GUILayout.Height(16));
                EditorGUILayout.LabelField(entity.Key.ToString(), GUILayout.Width(150));
                EditorGUILayout.LabelField(entity.Value.ToString(), GUILayout.Width(70));
                EditorGUILayout.LabelField(onSceneEntity.ToString(), GUILayout.Width(70));
                EditorGUILayout.LabelField(hideEntity.ToString(), GUILayout.Width(100));
                EditorGUILayout.LabelField(inPoolEntity.ToString(), GUILayout.Width(170));
                GUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndVertical();
    }
}
