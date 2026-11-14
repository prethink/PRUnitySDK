using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class Bootstrap : MonoBehaviour, ISDKEvents
{
    #region  Поля и свойства

    /// <summary>
    /// Настройки запуска на случай, когда настроек проекта нет.
    /// </summary>
    private static readonly BootstrapSettings DefaultSettings = new();

#if UNITY_EDITOR
    /// <summary>
    /// Путь активной сцены перед Play, сохранённый через перезагрузку домена редактора.
    /// </summary>
    public static string EditorStartScenePath
    {
        get => UnityEditor.SessionState.GetString("PRUnitySDK.Bootstrap.EditorStartScenePath", string.Empty);
        set => UnityEditor.SessionState.SetString("PRUnitySDK.Bootstrap.EditorStartScenePath", value ?? string.Empty);
    }

    /// <summary>
    /// Сцена, которую Play загрузит вместо открытой в редакторе; пусто — обычный порядок.
    /// </summary>
    /// <remarks>
    /// Для Play-тестов механизмов: им нужна своя чистая сцена, а сцену пользователя открывать
    /// и сохранять незачем — после выхода из игры редактор вернёт её сам. Действует независимо
    /// от <see cref="BootstrapSettings.StartFromEditorScene"/>.
    /// <para>
    /// Одноразовая: <c>Bootstrap</c> снимает её при первой же загрузке сцены, а <c>PlayFromBootstrap</c> — при любом
    /// выходе из Play (остановили раньше загрузки). Ставить прямо перед входом в Play; тот, кто ставит, снимает её и
    /// сам, если Play так и не начался.
    /// </para>
    /// </remarks>
    public static string EditorSceneOverride
    {
        get => UnityEditor.SessionState.GetString("PRUnitySDK.Bootstrap.EditorSceneOverride", string.Empty);
        set => UnityEditor.SessionState.SetString("PRUnitySDK.Bootstrap.EditorSceneOverride", value ?? string.Empty);
    }

    /// <summary>
    /// Следующий запуск игры идёт так, будто площадка не ответила: её сигнал готовности загрузчик пропускает.
    /// </summary>
    /// <remarks>
    /// Для Play-теста запуска: в редакторе площадка отвечает всегда, а проверить нужно ожидание, которое не дождалось.
    /// Одноразовая, как <see cref="EditorSceneOverride"/>: загрузчик снимает её в <c>Awake</c>. Действует, только когда
    /// площадка подключена, — без неё загрузчик никого не ждёт.
    /// </remarks>
    public static bool EditorSilentPlatform
    {
        get => UnityEditor.SessionState.GetBool("PRUnitySDK.Bootstrap.EditorSilentPlatform", false);
        set => UnityEditor.SessionState.SetBool("PRUnitySDK.Bootstrap.EditorSilentPlatform", value);
    }

    /// <summary>
    /// Сколько секунд ждать площадку в следующем запуске вместо настройки проекта; ноль — по настройке.
    /// </summary>
    /// <remarks>
    /// Play-тест ставит короче, чтобы не ждать настоящий срок. Одноразовая, снимается в <c>Awake</c>.
    /// </remarks>
    public static float EditorPlatformWaitOverride
    {
        get => UnityEditor.SessionState.GetFloat("PRUnitySDK.Bootstrap.EditorPlatformWaitOverride", 0f);
        set => UnityEditor.SessionState.SetFloat("PRUnitySDK.Bootstrap.EditorPlatformWaitOverride", value);
    }
#endif

    /// <summary>
    /// Настройки запуска из настроек проекта.
    /// </summary>
    /// <remarks>
    /// Не в полях компонента: загрузочная сцена у SDK общая, а запуск у каждой игры свой, и менять его
    /// приходится, не открывая сцену.
    /// </remarks>
    private static BootstrapSettings Settings
    {
        get
        {
            PRSDKSettings settings = PRUnitySDK.Settings;
            return settings != null && settings.Bootstrap != null ? settings.Bootstrap : DefaultSettings;
        }
    }

    /// <summary>
    /// Признак того, что инициализация SDK была переопределена.
    /// </summary>
    private bool isOverriden;

    /// <summary>
    /// Предотвращает повторную смену сцены при одновременном получении EventBus и ReadySignal.
    /// </summary>
    private bool sceneChangeRequested;

    /// <summary>
    /// Сборка SDK уже запущена: площадка может сообщить о готовности данных больше одного раза.
    /// </summary>
    private bool initializationStarted;

    /// <summary>
    /// Сигнал готовности площадки в этом запуске пропускается (<c>EditorSilentPlatform</c>).
    /// </summary>
    private bool platformSilenced;

    /// <summary>
    /// Сколько секунд ждать площадку в этом запуске; ноль — без предела.
    /// </summary>
    private float platformWaitSeconds;

    /// <summary>
    /// Игра запущена, не дождавшись площадки: её SDK не ответил за отведённое время.
    /// </summary>
    /// <remarks>
    /// Сервисы площадки в таком запуске работают как без неё. Хранилище площадки при этом не читает и не пишет
    /// (<see cref="GameDataStorageBase"/>): сохранение игрока не получено, и если площадка ответит позже, запись
    /// этого запуска легла бы поверх настоящего прогресса. Статическое по той же причине, что и время стадий:
    /// загрузчик исчезает вместе со своей сценой.
    /// </remarks>
    public static bool StartedWithoutPlatform { get; private set; }

    private BootstrapLoadingScreen loadingScreen;

    /// <summary>
    /// Код языка площадки («ru», «en», «tr»), известный до сборки SDK.
    /// </summary>
    /// <remarks>
    /// Экран загрузки появляется раньше, чем SDK узнаёт язык игрока, а подписать его нужно сразу.
    /// Код задаёт интеграция площадки; без неё экран до готовности SDK подписан по-английски.
    /// </remarks>
    private string platformLanguageCode;

    /// <summary>
    /// Чем загрузчик занят сейчас.
    /// </summary>
    public BootstrapStage Stage { get; private set; } = BootstrapStage.WaitingPlatform;

    private static readonly float[] stageStartTimes = new float[3];

    /// <summary>
    /// Когда началась каждая стадия, секунды от запуска движка; ноль — стадия не начиналась.
    /// </summary>
    /// <remarks>
    /// Статическое: загрузчик исчезает вместе со своей сценой, а диагностика читает время уже после.
    /// Записывает его сам загрузчик в момент перехода: стадии могут смениться подряд, без кадра между
    /// ними, и снаружи этого момента не поймать.
    /// </remarks>
    public static IReadOnlyList<float> StageStartTimes => stageStartTimes;

    private static readonly ReadySignal gameShownSignal = new("GameShown");

    /// <summary>
    /// Игра показана игроку: игровая сцена загружена, экран загрузки убран.
    /// </summary>
    /// <remarks>
    /// Статический по той же причине, что и время стадий: загрузчик исчезает вместе со своей сценой
    /// раньше, чем игра показана. Площадке о готовности игры сообщают по этому сигналу, а не по
    /// готовности SDK: между ними ещё загрузка сцены под экраном.
    /// </remarks>
    public static IReadySignal GameShownSignal => gameShownSignal;

    #endregion

    #region MonoBehaviour

    /// <inheritdoc />
    private void Awake()
    {
        // Раньше всего остального: диагностика замеряет запуск с самого его начала.
        BootstrapProbe.TryCreate();

        System.Array.Clear(stageStartTimes, 0, stageStartTimes.Length);
        gameShownSignal.ResetReady();
        StartedWithoutPlatform = false;
        SetStage(BootstrapStage.WaitingPlatform);

        TryOverrideBootstrap();

        platformWaitSeconds = Settings.PlatformWaitSeconds;

#if UNITY_EDITOR
        // Обе подмены одноразовые: снимаются здесь, чтобы следующий обычный Play шёл как всегда.
        platformSilenced = isOverriden && EditorSilentPlatform;
        EditorSilentPlatform = false;

        if (EditorPlatformWaitOverride > 0f)
            platformWaitSeconds = EditorPlatformWaitOverride;

        EditorPlatformWaitOverride = 0f;
#endif

        if (!isOverriden)
            InitializeSDK();
        else if (platformWaitSeconds > 0f)
            StartCoroutine(WaitPlatformRoutine());
    }

    private void OnEnable()
    {
        this.RunMethodHooks(MethodHookStage.PreOnEnable);

        EventBus.Subscribe(this);
        PRUnitySDK.ReadySignal.SubscribeOnReady(OnInitialized);

        this.RunMethodHooks(MethodHookStage.PostOnEnable);
    }

    // Отписываемся от ивента onGetSDKData
    private void OnDisable()
    {
        this.RunMethodHooks(MethodHookStage.PreOnDisable);

        PRUnitySDK.ReadySignal.UnSubscribe(OnInitialized);
        EventBus.Unsubscribe(this);

        this.RunMethodHooks(MethodHookStage.PostOnDisable);
    }

    #endregion

    #region Методы

    /// <summary>
    /// Переводит загрузчик в стадию и запоминает, когда она началась.
    /// </summary>
    private void SetStage(BootstrapStage stage)
    {
        Stage = stage;
        stageStartTimes[(int)stage] = PRTime.WallTime;
    }

    /// <summary>
    /// Перехват метода инициализации SDK для возможности кастомной инициализации.
    /// </summary>
    private void TryOverrideBootstrap()
    {
        var overrideMethod = this.GetMethods<OverrideBootstrapAttribute>().FirstOrDefault();
        overrideMethod?.Invoke(this, null);
    }

    /// <summary>
    /// Инициализация SDK.
    /// </summary>
    /// <remarks>
    /// С экраном загрузки SDK собирается порциями (<see cref="InitializeRoutine"/>): одним вызовом
    /// сборка занимает главный поток на секунды, и до её конца движок не отдаёт ни одного кадра —
    /// игроку нечего показать. Без экрана растягивать незачем, и сборка идёт одним вызовом.
    /// </remarks>
    private void InitializeSDK()
    {
        if (platformSilenced)
        {
            PRLog.WriteWarning(this, "Сигнал площадки пропущен: проверяется запуск без её ответа.");
            return;
        }

        StartSDK();
    }

    /// <summary>
    /// Ждёт площадку отведённое время и, не дождавшись, запускает игру без неё.
    /// </summary>
    /// <remarks>
    /// SDK площадки может не ответить ни успехом, ни ошибкой: скрипт загрузился, а обещание запуска не завершилось.
    /// Ветка «без площадки» у интеграций есть, но попадает в неё только явная ошибка, — без этого сторожа игрок
    /// остался бы на экране загрузки навсегда. Время — настоящее (<see cref="PRTime.WallTime"/>): игра ещё не идёт.
    /// </remarks>
    private IEnumerator WaitPlatformRoutine()
    {
        float deadline = PRTime.WallTime + platformWaitSeconds;

        while (!initializationStarted && PRTime.WallTime < deadline)
            yield return null;

        if (initializationStarted)
            yield break;

        StartedWithoutPlatform = true;
        PRLog.WriteError(this, $"Площадка не ответила за {platformWaitSeconds:0.#} с: игра запускается без неё. " +
                               "Сохранение площадки не прочитано, запись до конца запуска выключена.");
        StartSDK();
    }

    /// <summary>
    /// Начинает сборку SDK. Повторный вызов ничего не делает: площадка может ответить и после сторожа.
    /// </summary>
    private void StartSDK()
    {
        if (initializationStarted)
            return;

        initializationStarted = true;
        SetStage(BootstrapStage.InitializingSDK);

        if (Settings.SpreadInitialization)
            loadingScreen = BootstrapLoadingScreen.TryCreate();

        if (loadingScreen == null)
        {
            PRUnitySDK.InitializeSDK();
            return;
        }

        loadingScreen.Show(platformLanguageCode);
        StartCoroutine(InitializeRoutine());
    }

    /// <summary>
    /// Собирает SDK порциями, отдавая кадр, когда порция исчерпала свой бюджет времени.
    /// </summary>
    /// <remarks>
    /// Порядок сборки тот же, что у <see cref="PRUnitySDK.InitializeSDK"/>: это один и тот же перечень
    /// шагов, просто между ними проходят кадры. Готовым SDK становится на последнем шаге, как и раньше, —
    /// до него общий цикл обновления SDK не идёт, и подписчики готовности ничего не получают.
    /// </remarks>
    private IEnumerator InitializeRoutine()
    {
        // Кадр до начала сборки: экран загрузки должен попасть на экран раньше первой тяжёлой работы.
        yield return null;

        IEnumerator<string> steps = PRUnitySDK.InitializeSDKSteps();
        int total = Mathf.Max(1, PRUnitySDK.InitializationStepCount);
        int done = 0;
        float budget = Settings.FrameBudgetSeconds;
        float sliceStartedAt = PRTime.WallTime;

        while (steps.MoveNext())
        {
            done++;

            if (loadingScreen != null)
                loadingScreen.SetInitializationProgress(done / (float)total);

            if (PRTime.WallTime - sliceStartedAt < budget)
                continue;

            yield return null;
            sliceStartedAt = PRTime.WallTime;
        }
    }

    #endregion

    #region ISDKEvents

    public void OnInitialized()
    {
        if (sceneChangeRequested)
            return;

        sceneChangeRequested = true;
        SetStage(BootstrapStage.LoadingScene);

        // С экраном загрузки сцена грузится под ним; без него — как раньше, под затемнением.
        if (loadingScreen == null)
            loadingScreen = BootstrapLoadingScreen.TryCreate();

#if UNITY_EDITOR
        // Подмена одноразовая: снимается тут же, при первой загрузке. Иначе после сбоя теста следующий
        // обычный Play пользователя снова открыл бы тестовую сцену.
        string sceneOverride = EditorSceneOverride;
        EditorSceneOverride = string.Empty;

        string editorScenePath = !string.IsNullOrEmpty(sceneOverride)
            ? sceneOverride
            : Settings.StartFromEditorScene ? EditorStartScenePath : null;

        if (!string.IsNullOrEmpty(editorScenePath))
        {
            var parameters = new UnityEngine.SceneManagement.LoadSceneParameters(
                UnityEngine.SceneManagement.LoadSceneMode.Single);

            if (loadingScreen != null)
                loadingScreen.LoadScene(() => UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                    editorScenePath, parameters), gameShownSignal.SetReady);
            else
                SceneChanger.Instance.SceneChange(() => UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                    editorScenePath, parameters), gameShownSignal.SetReady);

            return;
        }
#endif

        int gameScene = Settings.GameSceneIndex;

        if (loadingScreen != null)
            loadingScreen.LoadScene(gameScene, gameShownSignal.SetReady);
        else
            SceneChanger.Instance.SceneChange(gameScene, gameShownSignal.SetReady);
    }

    #endregion
}
