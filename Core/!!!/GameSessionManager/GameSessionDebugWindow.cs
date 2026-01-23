//using System.Linq;
//using UnityEditor;
//using UnityEngine;

//#if UNITY_EDITOR

//public class EntityCountWindow : EditorWindow
//{
//    private GameSessionManager gameSessionManager;
//    private ObjectPoolSystem poolSystem;
//    private DiContainer container;
//    private double nextUpdate;
//    private bool showPlayerList = true;
//    private bool showEntityDetails = true;

//    [MenuItem("PRUnitySDK/Game session debug")]
//    public static void ShowWindow()
//    {
//        GetWindow<EntityCountWindow>("Game session debug");
//    }

//    private void OnEnable()
//    {
//        nextUpdate = EditorApplication.timeSinceStartup + 1.0;
//        EditorApplication.update += AutoRefresh;
//        UpdateStateGame();
//    }

//    private void UpdateStateGame()
//    {
//        if (ProjectContext.HasInstance)
//        {
//            container = ProjectContext.Instance.Container;
//            gameSessionManager = container.TryResolve<GameSessionManager>();
//            poolSystem = container.TryResolve<ObjectPoolSystem>();
//        }
//    }

//    private void AutoRefresh()
//    {
//        if (EditorApplication.timeSinceStartup >= nextUpdate)
//        {
//            nextUpdate = EditorApplication.timeSinceStartup + 1.0;
//            Repaint();
//        }
//    }

//    private void OnGUI()
//    {
//        GUILayout.Label("Состояния паузы:");
//        GUI.enabled = false; // Делаем UI неактивным
//        GUILayout.Toggle(PauseManager.IsProjectPaused, nameof(PauseManager.IsProjectPaused));
//        GUILayout.Toggle(PauseManager.IsLogicPaused, nameof(PauseManager.IsLogicPaused));
//        GUILayout.Toggle(PauseManager.IsFocusPaused, nameof(PauseManager.IsFocusPaused));
//        GUILayout.Toggle(PauseManager.IsMusicPaused, nameof(PauseManager.IsMusicPaused));
//        GUILayout.Label(ProjectBus.GameDifficulty.ToString());
//        GUI.enabled = true; // Включаем UI обратно

//        if (gameSessionManager == null || poolSystem == null)
//        {
//            UpdateStateGame();
//        }

//        if (gameSessionManager == null)
//        {
//            EditorGUILayout.LabelField("Debug меню можно использовать только в режиме игры!");
//            return;
//        }
//        GUILayout.Label("Информация по раундам:");
//        GUI.enabled = false;
//        EditorGUILayout.LabelField("Номер раунда:", gameSessionManager.Rounds.CurrentRound.ToString());
//        GUILayout.Toggle(gameSessionManager.Rounds.IsRoundActive, "Идет раунд");
//        GUI.enabled = true; // Включаем UI обратно
//        //EditorGUILayout.LabelField("Session Active:", gameSessionManager.IsActiveSession.ToString());
//        //EditorGUILayout.Space();

//        DrawPlayersInfo();
//        DrawEntitiesInfo();
//        DrawPoolSystem();
//    }

//    private void DrawPoolSystem()
//    {
//        EditorGUILayout.LabelField("PoolSystem", EditorStyles.boldLabel);
//        EditorGUILayout.BeginVertical("box");
//            EditorGUILayout.BeginHorizontal();
//                EditorGUILayout.LabelField("Root", GUILayout.Width(150));
//                EditorGUILayout.LabelField("Категория", GUILayout.Width(150));
//                EditorGUILayout.LabelField("Всего", GUILayout.Width(100));
//                EditorGUILayout.LabelField("На сцене", GUILayout.Width(100));
//                EditorGUILayout.LabelField("Спрятанных", GUILayout.Width(100));
//            EditorGUILayout.EndHorizontal();
//            foreach (var item in poolSystem.GenerateReport())
//            {
//                EditorGUILayout.BeginHorizontal();
//                    EditorGUILayout.LabelField(item.Type, GUILayout.Width(150));
//                    EditorGUILayout.LabelField(item.Category, GUILayout.Width(150));
//                    EditorGUILayout.LabelField(item.TotalCount.ToString(), GUILayout.Width(100));
//                    EditorGUILayout.LabelField(item.ShowCount.ToString(), GUILayout.Width(100));
//                    EditorGUILayout.LabelField(item.HideCount.ToString(), GUILayout.Width(100));
//                EditorGUILayout.EndHorizontal();
//            }
//        EditorGUILayout.EndVertical();
//    }

//    private void DrawPlayersInfo()
//    {
//        EditorGUILayout.LabelField("Players", EditorStyles.boldLabel);
//        EditorGUILayout.BeginHorizontal();
//        EditorGUILayout.LabelField("Людей:", gameSessionManager.PlayerTracker.HumanCount.ToString());
//        EditorGUILayout.LabelField("Ботов:", gameSessionManager.PlayerTracker.AICount.ToString());
//        EditorGUILayout.EndHorizontal();
//        EditorGUILayout.BeginHorizontal();
//        EditorGUILayout.LabelField("Живых:", gameSessionManager.PlayerTracker.AliveCount.ToString());
//        EditorGUILayout.LabelField("Мертвых:", gameSessionManager.PlayerTracker.DeadCount.ToString());
//        EditorGUILayout.EndHorizontal();
//        EditorGUILayout.Space();

//        showPlayerList = EditorGUILayout.Foldout(showPlayerList, $"Player List ({gameSessionManager.PlayerTracker.PlayersCount})");
//        if (showPlayerList)
//        {
//            var players = gameSessionManager.PlayerTracker.Players;
//            EditorGUILayout.BeginVertical("box");
//            EditorGUILayout.BeginHorizontal();
//            EditorGUILayout.LabelField("HumanId", GUILayout.Width(80));
//            EditorGUILayout.LabelField("Имя", GUILayout.Width(150));
//            EditorGUILayout.LabelField("Команда", GUILayout.Width(100));
//            EditorGUILayout.LabelField("Очков", GUILayout.Width(100));
//            EditorGUILayout.LabelField("Убийств", GUILayout.Width(60));
//            EditorGUILayout.LabelField("Смертей", GUILayout.Width(60));
//            EditorGUILayout.LabelField("Статус", GUILayout.Width(100));
//            EditorGUILayout.LabelField("Действие", GUILayout.Width(70));
//            EditorGUILayout.EndHorizontal();

//            foreach (var player in players.OrderByDescending(x => x.IsAlive))
//            {
//                Texture icon = player.Icon.texture;
//                string isAliveStatus = player.IsAlive ? L.Tr(PlayerTranslateKeys.ALIVE_KEY) : L.Tr(PlayerTranslateKeys.DEAD_KEY);
//                string isBot = player.PlayerType == PlayerType.AI ? L.Tr(PlayerTranslateKeys.BOT_KEY) : L.Tr(PlayerTranslateKeys.HUMAN_KEY);

//                EditorGUILayout.BeginHorizontal();
//                GUILayout.Label(icon, GUILayout.Width(16), GUILayout.Height(16));
//                EditorGUILayout.LabelField($"{player.HumanId}", GUILayout.Width(58));
//                EditorGUILayout.LabelField(player.Name, GUILayout.Width(150));
//                EditorGUILayout.LabelField(player.PlayerTeam.Name, GUILayout.Width(100));
//                EditorGUILayout.LabelField(player.Points.ToString(), GUILayout.Width(100));
//                EditorGUILayout.LabelField(player.Kills.ToString(), GUILayout.Width(60));
//                EditorGUILayout.LabelField(player.Deaths.ToString(), GUILayout.Width(60));
//                EditorGUILayout.LabelField(isAliveStatus, GUILayout.Width(100));
//                if (!player.IsAlive && GUILayout.Button(L.Tr(PlayerTranslateKeys.REVIVE_KEY), GUILayout.Width(70)))
//                {
//                    player.Revive();
//                }
//                EditorGUILayout.EndHorizontal();
//            }
//            EditorGUILayout.EndVertical();
//        }
//    }

//    private void DrawEntitiesInfo()
//    {
//        EditorGUILayout.LabelField("Entities", EditorStyles.boldLabel);
//        var existsEntities = gameSessionManager.EntityTracker.GetExistsEntityCount();
//        var onSceneEntities = gameSessionManager.EntityTracker.GetEntityOnSceneCount();
//        var onPoolEntities = gameSessionManager.EntityTracker.GetEntityInPoolCount();
//        var hideEntities = existsEntities - onSceneEntities;


//        EditorGUILayout.BeginVertical("box");
//        EditorGUILayout.BeginHorizontal();
//        EditorGUILayout.LabelField("Всего сущностей", GUILayout.Width(170));
//        EditorGUILayout.LabelField("На сцене", GUILayout.Width(70));
//        EditorGUILayout.LabelField("Спрятанных", GUILayout.Width(100));
//        EditorGUILayout.LabelField("Спрятанных в pool", GUILayout.Width(170));
//        EditorGUILayout.EndHorizontal();

//        EditorGUILayout.BeginHorizontal();
//        EditorGUILayout.LabelField(existsEntities.ToString(), GUILayout.Width(170));
//        EditorGUILayout.LabelField(onSceneEntities.ToString(), GUILayout.Width(70));
//        EditorGUILayout.LabelField(hideEntities.ToString(), GUILayout.Width(100));
//        EditorGUILayout.LabelField(onPoolEntities.ToString(), GUILayout.Width(170));
//        EditorGUILayout.EndHorizontal();

//        showEntityDetails = EditorGUILayout.Foldout(showEntityDetails, $"EntityDetails ({gameSessionManager.EntityTracker.GetEntitiesCount()})");
//        if (showEntityDetails)
//        {
//            EditorGUILayout.BeginHorizontal();
//            EditorGUILayout.LabelField("Тип сущности", GUILayout.Width(170));
//            EditorGUILayout.LabelField("Всего", GUILayout.Width(70));
//            EditorGUILayout.LabelField("На сцене", GUILayout.Width(70));
//            EditorGUILayout.LabelField("Спрятанных", GUILayout.Width(100));
//            EditorGUILayout.LabelField("Спрятанных в pool", GUILayout.Width(170));
//            EditorGUILayout.EndHorizontal();

//            var entityDetails = gameSessionManager.EntityTracker.RegisteredEntity.OrderByDescending(x => x.Value);
//            foreach (var entity in entityDetails)
//            {
//                var onSceneEntity = gameSessionManager.EntityTracker.GetExactEntityOnSceneCount(entity.Key);
//                var inPoolEntity = gameSessionManager.EntityTracker.GetExactEntityInPoolCount(entity.Key);
//                var hideEntity = entity.Value - onSceneEntity;

//                Texture icon = EditorGUIUtility.IconContent("d_Prefab Icon").image;
//                GUILayout.BeginHorizontal();
//                GUILayout.Label(icon, GUILayout.Width(16), GUILayout.Height(16));
//                EditorGUILayout.LabelField(entity.Key.ToString(), GUILayout.Width(150));
//                EditorGUILayout.LabelField(entity.Value.ToString(), GUILayout.Width(70));
//                EditorGUILayout.LabelField(onSceneEntity.ToString(), GUILayout.Width(70));
//                EditorGUILayout.LabelField(hideEntity.ToString(), GUILayout.Width(100));
//                EditorGUILayout.LabelField(inPoolEntity.ToString(), GUILayout.Width(170));
//                GUILayout.EndHorizontal();
//            }
//        }
//        EditorGUILayout.EndVertical();
//    }
//}

//#endif