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

    #endregion

    #region MonoBehaviour

    /// <inheritdoc />
    private void Awake()
    {
        // Раньше всего остального: диагностика замеряет запуск с самого его начала.
        BootstrapProbe.TryCreate();

        System.Array.Clear(stageStartTimes, 0, stageStartTimes.Length);
        SetStage(BootstrapStage.WaitingPlatform);

        TryOverrideBootstrap();

        if (!isOverriden)
            InitializeSDK();
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
        stageStartTimes[(int)stage] = Time.realtimeSinceStartup;
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
        float sliceStartedAt = Time.realtimeSinceStartup;

        while (steps.MoveNext())
        {
            done++;

            if (loadingScreen != null)
                loadingScreen.SetInitializationProgress(done / (float)total);

            if (Time.realtimeSinceStartup - sliceStartedAt < budget)
                continue;

            yield return null;
            sliceStartedAt = Time.realtimeSinceStartup;
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

        int gameScene = Settings.GameSceneIndex;

        if (loadingScreen != null)
            loadingScreen.LoadScene(gameScene);
        else
            SceneChanger.Instance.SceneChange(gameScene);
    }

    #endregion
}
