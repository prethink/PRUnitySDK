using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Диагностика загрузочной сцены: показывает, что игра находится именно в ней, и сколько длится каждая стадия.
/// </summary>
/// <remarks>
/// На слабом телефоне запуск идёт секундами, и по одному виду экрана не понять, где именно игра стоит:
/// движок ещё не запустился, загрузчик ждёт площадку, собирается SDK или грузится игровая сцена.
/// <para>
/// Зонд отвечает на это тремя признаками. Фон загрузочной сцены становится цветным — чёрный экран значит,
/// что до неё дело ещё не дошло. Поверх пишется текущая стадия со счётчиком секунд и кадров: счётчик
/// стоит — главный поток занят, идёт — игра ждёт чего-то снаружи. После загрузки игровой сцены в углу
/// остаётся итог по стадиям, чтобы его можно было прочитать с телефона, где консоли нет.
/// </para>
/// <para>
/// Время стадий записывает сам загрузчик (<see cref="Bootstrap.StageStartTimes"/>), а не зонд по кадрам:
/// ожидание площадки и сборка SDK могут пройти подряд, без единого кадра между ними, и опрос в
/// <c>Update</c> приписал бы всё это время первой стадии.
/// </para>
/// <para>
/// Это инструмент проверки, а не экран загрузки для игрока: текст служебный и не переводится.
/// Работает он только в отладочном режиме проекта (<c>PRUnitySDK.Settings.Project.IsDebug</c>).
/// На сцене его нет: загрузчик создаёт зонд сам (<see cref="TryCreate"/>), а включается он и
/// настраивается в настройках проекта (<see cref="BootstrapSettings"/>).
/// </para>
/// </remarks>
public sealed class BootstrapProbe : MonoBehaviour
{
    private bool showOnScreen;
    private Color backgroundColor;
    private float summarySeconds;

    private static readonly string[] StageNames = { "1/3 platform", "2/3 sdk", "3/3 scene" };

    /// <summary>
    /// Сколько самых долгих шагов сборки SDK показывать в итоге.
    /// </summary>
    private const int SlowestCount = 8;

    private Bootstrap bootstrap;
    private GameObject cameraObject;

    /// <summary>
    /// Загрузочная сцена: о её собственной загрузке движок тоже сообщает, и за игровую её принимать нельзя.
    /// </summary>
    private Scene bootScene;

    /// <summary>
    /// Сколько движок работал до первой сцены: загрузка и запуск, которые из игры уже не замерить иначе.
    /// </summary>
    private float engineSeconds;

    private BootstrapStage stage;
    private int frames;

    /// <summary>
    /// Когда движок отдал первый кадр: до этого на экране нет ничего, что бы ни происходило в игре.
    /// </summary>
    private float firstUpdateAt = -1f;

    private float sceneLoadedAt = -1f;
    private float firstFrameAt = -1f;
    private string summary;

    private GUIStyle bigStyle;
    private GUIStyle smallStyle;

    /// <summary>
    /// Создаёт зонд, если он включён в настройках и проект в отладочном режиме.
    /// </summary>
    /// <remarks>
    /// Зовёт загрузчик в самом начале своего <c>Awake</c>: объекта на сцене у зонда нет, чтобы ради
    /// диагностики не приходилось открывать загрузочную сцену.
    /// </remarks>
    public static void TryCreate()
    {
        if (IsAllowed(out _))
            new GameObject(nameof(BootstrapProbe)).AddComponent<BootstrapProbe>();
    }

    private void Awake()
    {
        // Отладочный инструмент: выключенного или в релизном режиме его нет вовсе — ни надписей, ни камеры, ни лога.
        if (!IsAllowed(out BootstrapSettings settings))
        {
            enabled = false;
            Destroy(gameObject);
            return;
        }

        showOnScreen = settings.ProbeOnScreen;
        backgroundColor = settings.ProbeBackgroundColor;
        summarySeconds = settings.ProbeSummarySeconds;

        engineSeconds = Time.realtimeSinceStartup;

        // Зонд переживает смену сцены, чтобы показать итог уже поверх игры.
        bootScene = gameObject.scene;
        DontDestroyOnLoad(gameObject);

        // Раскладка IMGUI не нужна: рисуется одна надпись по готовым координатам.
        useGUILayout = false;

        bootstrap = FindObjectOfType<Bootstrap>();
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (showOnScreen)
            CreateCamera();

        Debug.Log($"[BootstrapProbe] bootstrap scene started, engine {engineSeconds:0.00}s");
    }

    /// <summary>
    /// Зонд включён в настройках, и проект в отладочном режиме.
    /// </summary>
    /// <remarks>
    /// Настройки проекта — ассет, он доступен и до сборки SDK. Нет настроек — режим неизвестен,
    /// и зонд не показывается: служебный текст у игрока хуже, чем пропавшая диагностика.
    /// </remarks>
    private static bool IsAllowed(out BootstrapSettings bootstrap)
    {
        PRSDKSettings settings = PRUnitySDK.Settings;
        bootstrap = settings != null ? settings.Bootstrap : null;

        return bootstrap != null && bootstrap.ProbeEnabled && settings.Project != null && settings.Project.IsDebug;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <remarks>
    /// Камера на отдельном объекте: конвейер рендера добавляет к камере свой компонент, который
    /// не даёт удалить её саму, а объект целиком удаляется без вопросов.
    /// </remarks>
    private void CreateCamera()
    {
        cameraObject = new GameObject("BootstrapProbeCamera");
        cameraObject.transform.SetParent(transform, false);

        var probeCamera = cameraObject.AddComponent<Camera>();
        probeCamera.clearFlags = CameraClearFlags.SolidColor;
        probeCamera.backgroundColor = backgroundColor;
        probeCamera.cullingMask = 0;
        probeCamera.depth = -100f;
        probeCamera.allowHDR = false;
        probeCamera.allowMSAA = false;
    }

    private void Update()
    {
        frames++;

        if (firstUpdateAt < 0f)
        {
            firstUpdateAt = Time.realtimeSinceStartup;
            Debug.Log($"[BootstrapProbe] first frame of the engine at {firstUpdateAt:0.00}s");
        }

        if (sceneLoadedAt >= 0f)
        {
            // Первый кадр игровой сцены: в него попадает всё, что сцена делает на старте.
            if (firstFrameAt < 0f)
            {
                firstFrameAt = Time.realtimeSinceStartup;
                BuildSummary();
            }

            if (Time.realtimeSinceStartup - firstFrameAt >= summarySeconds)
                Destroy(gameObject);

            return;
        }

        if (bootstrap != null)
            stage = bootstrap.Stage;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (sceneLoadedAt >= 0f || scene == bootScene)
            return;

        sceneLoadedAt = Time.realtimeSinceStartup;
        stage = BootstrapStage.LoadingScene;

        if (cameraObject != null)
            Destroy(cameraObject);
    }

    private void BuildSummary()
    {
        IReadOnlyList<float> started = Bootstrap.StageStartTimes;

        // Стадия, в которую загрузчик не заходил, начинается там же, где следующая: её длительность — ноль.
        float sceneStart = started[2] > 0f ? started[2] : sceneLoadedAt;
        float sdkStart = started[1] > 0f ? started[1] : sceneStart;
        float platformStart = started[0] > 0f ? started[0] : engineSeconds;

        summary =
            $"BOOT  engine {engineSeconds:0.0}s\n" +
            $"1 platform {sdkStart - platformStart:0.0}s\n" +
            $"2 sdk {sceneStart - sdkStart:0.0}s\n" +
            $"3 scene {sceneLoadedAt - sceneStart:0.0}s\n" +
            $"first frame {firstFrameAt - sceneLoadedAt:0.0}s\n" +
            $"total {firstFrameAt:0.0}s\n" +
            $"engine frame 1 at {firstUpdateAt:0.0}s" +
            BuildSlowest();

        Debug.Log("[BootstrapProbe] " + summary.Replace("\n", " | "));
    }

    /// <summary>
    /// Самые долгие шаги сборки SDK: что именно занимает стадию <c>2 sdk</c>.
    /// </summary>
    /// <remarks>
    /// Шаги записывает сам SDK (<see cref="PRUnitySDK.InitializationSteps"/>), и они покрывают сборку
    /// целиком: их сумма — чистое время работы, без кадров, отданных между шагами.
    /// </remarks>
    private static string BuildSlowest()
    {
        var steps = new List<KeyValuePair<string, double>>(PRUnitySDK.InitializationSteps);

        if (steps.Count == 0)
            return string.Empty;

        double sum = 0;

        foreach (KeyValuePair<string, double> step in steps)
            sum += step.Value;

        steps.Sort((left, right) => right.Value.CompareTo(left.Value));

        var text = new System.Text.StringBuilder($"\nsdk steps {steps.Count}, work {sum / 1000.0:0.0}s, slowest:");

        for (int index = 0; index < steps.Count && index < SlowestCount; index++)
            text.Append($"\n{steps[index].Key} {steps[index].Value / 1000.0:0.00}s");

        return text.ToString();
    }

    private void OnGUI()
    {
        if (!showOnScreen || Event.current.type != EventType.Repaint)
            return;

        bigStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
        smallStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, fontStyle = FontStyle.Bold };

        float margin = Screen.height * 0.02f;
        var area = new Rect(margin, margin, Screen.width - margin * 2f, Screen.height - margin * 2f);

        if (sceneLoadedAt < 0f)
        {
            float now = Time.realtimeSinceStartup;
            float stageStart = Bootstrap.StageStartTimes[(int)stage];

            // Вверху экрана, а не по центру: в центре стоит экран загрузки.
            string text = $"BOOT {StageNames[(int)stage]}  {now - stageStart:0.0}s  total {now:0.0}s  frame {frames}";

            bigStyle.fontSize = Mathf.Max(14, Screen.height / 28);
            DrawLabel(area, text, bigStyle);
            return;
        }

        if (summary == null)
            return;

        smallStyle.fontSize = Mathf.Max(12, Screen.height / 40);
        DrawLabel(area, summary, smallStyle);
    }

    // Белый текст с чёрной тенью: читается и на цветном фоне, и поверх игровой сцены.
    private static void DrawLabel(Rect rect, string text, GUIStyle style)
    {
        float offset = Mathf.Max(1f, style.fontSize / 14f);

        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height), text, style);

        style.normal.textColor = Color.white;
        GUI.Label(rect, text, style);
    }
}
