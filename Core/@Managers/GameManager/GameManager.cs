using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public partial class GameManager : MonoBehaviourSingletonBase<GameManager>, IReadySignalProvider
{
    #region Поля и свойства

    /// <summary>
    /// Игровые настройки.
    /// </summary>
    /// <remarks>
    /// Скрыты от сериализации Unity: при перезагрузке домена редактор восстанавливает
    /// и приватные поля, сам вызывая конструктор, а тот читает настройки SDK через
    /// <c>Resources.Load</c> — во время сериализации это запрещено.
    /// </remarks>
    [field: NonSerialized]
    private GameSettings gameSettings { get; set; }

    /// <summary>
    /// Данные проекта.
    /// </summary>
    /// <remarks>
    /// Скрыты от сериализации Unity по той же причине: конструктор запускает хуки модулей.
    /// </remarks>
    [field: NonSerialized]
    private ProjectData projectData { get; set; }

    /// <summary>
    /// Глобальные настройки сессии.
    /// </summary>
    public GlobalGameSettingsSession GameSettingsSession { get; private set; }

    private IGameDataStorage gameDataStorage { get; set; }

    /// <summary>
    /// Кулдаун записи важных, но частых событий, секунды.
    /// </summary>
    /// <remarks>
    /// Площадка принимает ограниченное число записей: у Яндекса это 100 запросов за 5 минут,
    /// дальше запись отклоняется — в том числе запись покупки. Раз в пять секунд — не больше
    /// 60 записей за те же 5 минут, остальное остаётся покупкам и подаркам.
    /// </remarks>
    public const long FrequentSaveCooldownSeconds = 5;

    private bool isInitialize;
    private bool isSaving;

    /// <summary>
    /// Частая запись попала в кулдаун и ждёт своей очереди.
    /// </summary>
    /// <remarks>
    /// Снимается любой удачной полной записью: она уносит и то, ради чего запись просили.
    /// </remarks>
    private bool frequentSavePending;
    private Coroutine deferredFrequentSave;
    private long saveCooldownCounter;
    private SynchronizationContext synchronizationContext;
    private readonly object saveDiagnosticsLock = new();
    private int activeSaveOperationCount;
    private bool activeSaveOperationFailed;
    private GameSaveState saveState = GameSaveState.NotStarted;
    private DateTime? saveCreationTimeUtc;
    private DateTime? lastSaveTimeUtc;
    private bool hasLoadedSave;

    /// <summary>
    /// Состояние save-операций в текущей игровой сессии.
    /// </summary>
    public GameSaveState SaveState
    {
        get
        {
            lock (saveDiagnosticsLock)
                return saveState;
        }
    }

    /// <summary>
    /// UTC-время последнего сохранения. После успешной загрузки восстанавливается
    /// из storage; null означает, что дата ещё неизвестна.
    /// </summary>
    public DateTime? LastSaveTimeUtc
    {
        get
        {
            lock (saveDiagnosticsLock)
                return lastSaveTimeUtc;
        }
    }

    /// <summary>
    /// UTC-время создания текущего save или null, если storage не предоставляет метаданные.
    /// </summary>
    public DateTime? SaveCreationTimeUtc
    {
        get
        {
            lock (saveDiagnosticsLock)
                return saveCreationTimeUtc;
        }
    }

    /// <summary>
    /// True when the current session successfully loaded an existing save.
    /// </summary>
    public bool HasLoadedSave
    {
        get
        {
            lock (saveDiagnosticsLock)
                return hasLoadedSave;
        }
    }

    /// <summary>
    /// Whole seconds remaining before a regular save can start.
    /// </summary>
    public long SaveCooldownRemainingSeconds
    {
        get
        {
            long cooldownSeconds = GetStorageSettings().SaveCooldownSeconds;
            if (cooldownSeconds <= 0)
                return 0;

            long elapsedSeconds = PRTime.Instance.CurrentRealSecond - saveCooldownCounter;
            return Math.Max(0, cooldownSeconds - elapsedSeconds);
        }
    }

    #endregion

    #region MonoBehaviour

    private void Awake()
    {
        this.RunMethodHooks(MethodHookStage.PreAwake);

        this.InitializeGameManager();

        this.RunMethodHooks(MethodHookStage.PostAwake);
    }

    private void Start()
    {
        this.RunMethodHooks(MethodHookStage.PreStart);
        this.RunMethodHooks(MethodHookStage.PostStart);
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        PRLog.WriteDebug(this, $"{nameof(OnApplicationPause)} pauseStatus - {pauseStatus}", new PRLogSettings() { LevelDebug = 9 });

        PRUnitySDK.PauseManager.SetProjectPaused(pauseStatus, this);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        PRLog.WriteDebug(this, $"{nameof(OnApplicationFocus)} pauseStatus - {hasFocus}", new PRLogSettings() { LevelDebug = 9 });

        var requiredPause = !hasFocus;
        PRUnitySDK.PauseManager.SetFocusPaused(requiredPause, this);
    }

    public void OnPageVisibilityChange(int isHiddenInt)
    {
        if (!PRUnitySDK.DeviceInfo.IsIOS())
            return;

        bool isHidden = isHiddenInt == 1;
        PRLog.WriteDebug(this, $"WebGL Visibility Changed. Hidden: {isHidden}", new PRLogSettings() { LevelDebug = 5 });

        // Скрытая страница — повод поставить паузу, поэтому isHidden передаётся как есть:
        // SetFocusPaused принимает признак «нужна пауза», как и в OnApplicationFocus.
        PRUnitySDK.PauseManager.SetFocusPaused(isHidden, this);
    }

    #endregion

    #region Методы

    public void InitializeGameManager()
    {
        if (isInitialize)
            return;

        synchronizationContext = SynchronizationContext.Current;

        gameDataStorage = PRUnitySDK.GameDataStorage;
        bool loadedExistingSave = gameDataStorage.TryLoad();
        CaptureLoadedSaveInfo(loadedExistingSave);
        bool isRequiredFirstInitialize = !loadedExistingSave;
        gameDataStorage.ReadySignal.SubscribeOnReady(() =>
        {
            LoadingData();

            if (isRequiredFirstInitialize)
                InitializeDefaultData();

            AutoSaveHandler();

            GameplayEvents.RaiseGameReady();
            readySignal.SetReady();
            isInitialize = true;
        });
    }

    private void InitializeDefaultData()
    {
        var defaultSettings = PRUnitySDK.Settings.Default;

        gameSettings.Sensitivity = defaultSettings.Sensitivity;
        gameSettings.InvertHorizontalInput = defaultSettings.InvertHorizontalInput;
        gameSettings.InvertVerticalInput = defaultSettings.InvertVerticalInput;
        gameSettings.MasterVolume = defaultSettings.MasterVolume;
        gameSettings.MusicVolume = defaultSettings.MusicVolume;
        gameSettings.EffectVolume = defaultSettings.EffectVolume;
        gameSettings.UIVolume = defaultSettings.UIVolume;

        gameSettings.OffEffect = defaultSettings.OffEffect;
        gameSettings.OffSound = defaultSettings.OffSound;
        gameSettings.OffMusic = defaultSettings.OffMusic;

        gameSettings.IsShowCursor = defaultSettings.IsShowCursor;

        StartSaveTask();
    }

    public async void StartSaveTask(bool isUserExecuter = false)
    {
        if (!CanStartSave(isUserExecuter))
            return;

        await InternalSave();
    }

    /// <summary>
    /// Returns whether a full save can start without changing the cooldown.
    /// </summary>
    public bool CanStartSave(bool ignoreCooldown = false)
    {
        if (isSaving)
            return false;

        if (ignoreCooldown)
            return true;

        return SaveCooldownRemainingSeconds <= 0;
    }

    private async Task InternalSave()
    {
        if (isSaving)
            return;

        bool succeeded = false;
        BeginSaveOperation();

        try
        {
            isSaving = true;

            CollectSaveableState();

            await SwitchToMainThread();
            GameplayEvents.RaiseBeforeSaveEvent();
            gameDataStorage.UpdateProjectData(projectData);
            gameDataStorage.UpdateGameSettings(gameSettings);
            gameDataStorage.Save();
            succeeded = true;
            frequentSavePending = false;

            GameplayEvents.RaiseSaveEvent();
        }
        catch(Exception ex) 
        {
            Debug.LogException(ex);
        }
        finally
        {
            isSaving = false;
            CompleteSaveOperation(succeeded, GetSuccessfulSaveInfo(succeeded));
        }
    }

    /// <summary>
    /// Собирает состояние объектов сцены в данные проекта.
    /// </summary>
    /// <remarks>
    /// Часть данных живёт не в <c>projectData</c>, а в самих объектах: брейнрот на холдере
    /// и накопленные им деньги. Без этого шага на диск ушла бы копия без них.
    /// <para>
    /// Сломавшийся объект не отменяет сохранение остальных: потерять состояние одного
    /// холдера неприятно, потерять всё сразу — гораздо хуже.
    /// </para>
    /// </remarks>
    private void CollectSaveableState()
    {
        foreach (ISaveable saveable in PRUnitySDK.Trackers.Saveables.Collect())
        {
            if (saveable.IsNull())
                continue;

            try
            {
                if (!saveable.TrySaveData())
                    PRLog.WriteWarning(this, $"{saveable.GetType().Name} не отдал состояние — в сохранении останется прежнее.");
            }
            catch (Exception exception)
            {
                PRLog.WriteError(this, $"{saveable.GetType().Name} сорвал сбор состояния: {exception}");
            }
        }
    }

    public Task SwitchToMainThread()
    {
        var tcs = new TaskCompletionSource<bool>();
        synchronizationContext.Post(_ =>
        {
            tcs.SetResult(true);
        }, null);

        return tcs.Task;
    }

    /// <summary>
    /// Сохраняет проектные данные.
    /// </summary>
    /// <remarks>
    /// Кулдаун бережёт диск и облако от частых записей, но подходит не всему. Покупка
    /// за ресурсы или отключение рекламы должны лечь на диск сразу: игрок уже заплатил,
    /// и потерять это при закрытии вкладки нельзя.
    /// </remarks>
    /// <param name="ignoreCooldown">Сохранить не дожидаясь окончания кулдауна.</param>
    public void SaveProjectData(bool ignoreCooldown = false)
    {
        // Полным путём, вместе со сбором состояния сущностей. Часть данных живёт в сцене,
        // а не в projectData: брейнрот на холдере и накопленные им деньги попадают туда
        // только через TrySaveData. Запись без сбора кладёт на диск копию без них
        // и вдобавок сдвигает кулдаун — автосохранение, которое собрало бы состояние,
        // откладывается, и при следующем запуске холдеры оказываются пустыми.
        StartSaveTask(ignoreCooldown);
    }

    /// <summary>
    /// Сохраняет проектные данные с укороченным кулдауном.
    /// </summary>
    /// <remarks>
    /// Для событий, которые жалко терять, но которые идут пачками: новый уровень, награда
    /// платформы. Обычная запись в кулдауне отбрасывается, а полный обход им не подходит —
    /// на каждое событие ушла бы своя запись, и предел площадки кончился бы за минуту.
    /// <para>
    /// Отсчёт общий с остальными сохранениями, от последней успешной записи: сколько бы
    /// источников ни звало метод, записей выходит не больше одной за кулдаун
    /// (<see cref="FrequentSaveCooldownSeconds"/>, см. <see cref="GetFrequentSaveCooldown"/>).
    /// </para>
    /// <para>
    /// Запрос внутри кулдауна не теряется, а откладывается: запись выполнится, как только
    /// кулдаун истечёт. Иначе уровень, взятый через две секунды после прошлой записи, ждал
    /// бы автосохранения, а оно бывает и раз в три минуты.
    /// </para>
    /// </remarks>
    public void SaveFrequentProjectData()
    {
        // Запись уже идёт: данные она возьмёт позже, вместе с только что изменённым.
        if (isSaving)
            return;

        if (GetFrequentSaveRemaining() <= 0)
        {
            StartSaveTask(true);
            return;
        }

        frequentSavePending = true;
        deferredFrequentSave ??= StartCoroutine(SaveWhenFrequentCooldownEnds());
    }

    /// <summary>
    /// Кулдаун частой записи, секунды.
    /// </summary>
    /// <remarks>
    /// Положительный кулдаун из настроек короче — действует он: проект сам разрешил писать чаще.
    /// Ноль в настройках означает «обычный кулдаун выключен», а не «писать на каждое событие»:
    /// предел площадки от настроек проекта не зависит, и частая запись ограничена всегда.
    /// </remarks>
    private long GetFrequentSaveCooldown()
    {
        long configured = GetStorageSettings().SaveCooldownSeconds;

        return configured > 0
            ? Math.Min(FrequentSaveCooldownSeconds, configured)
            : FrequentSaveCooldownSeconds;
    }

    private long GetFrequentSaveRemaining()
    {
        long elapsedSeconds = PRTime.Instance.CurrentRealSecond - saveCooldownCounter;
        return GetFrequentSaveCooldown() - elapsedSeconds;
    }

    /// <summary>
    /// Дожидается конца кулдауна и выполняет отложенную частую запись.
    /// </summary>
    /// <remarks>
    /// Ждёт настоящее время: игра могла встать на паузу окном, а запись всё равно нужна.
    /// Остаток пересчитывается после каждого ожидания — пока запрос ждал, записать могло
    /// что-то другое: тогда запрос уже выполнен либо кулдаун начался заново.
    /// </remarks>
    private IEnumerator SaveWhenFrequentCooldownEnds()
    {
        while (frequentSavePending)
        {
            if (isSaving)
            {
                yield return null;
                continue;
            }

            long remaining = GetFrequentSaveRemaining();

            if (remaining > 0)
            {
                yield return new WaitForSecondsRealtime(remaining);
                continue;
            }

            // Флаг снимается здесь, а не только по удаче: неудачная запись не должна
            // превращать ожидание в повтор каждую секунду. Следующий запрос заведёт его снова.
            frequentSavePending = false;
            StartSaveTask(true);
        }

        deferredFrequentSave = null;
    }

    /// <summary>
    /// Сохраняет настройки игры.
    /// </summary>
    /// <param name="ignoreCooldown">Сохранить не дожидаясь окончания кулдауна.</param>
    public void SaveGameSettingsData(bool ignoreCooldown = false)
    {
        if (!CanStartSave(ignoreCooldown))
            return;

        ExecuteImmediateSave(() => gameDataStorage.UpdateGameSettings(gameSettings, true));
    }

    private void ExecuteImmediateSave(Action saveAction)
    {
        bool succeeded = false;
        BeginSaveOperation();

        try
        {
            saveAction.Invoke();
            succeeded = true;
        }
        finally
        {
            CompleteSaveOperation(succeeded, GetSuccessfulSaveInfo(succeeded));
        }
    }

    private void BeginSaveOperation()
    {
        lock (saveDiagnosticsLock)
        {
            if (activeSaveOperationCount == 0)
                activeSaveOperationFailed = false;

            activeSaveOperationCount++;
            saveState = GameSaveState.Saving;
        }
    }

    private void CompleteSaveOperation(bool succeeded, (DateTime? creationTimeUtc, DateTime? updateTimeUtc) saveInfo)
    {
        lock (saveDiagnosticsLock)
        {
            if (succeeded)
            {
                saveCreationTimeUtc = saveInfo.creationTimeUtc ?? saveCreationTimeUtc;
                lastSaveTimeUtc = saveInfo.updateTimeUtc ?? ToUtc(PRUnitySDK.ServerTime.GetNow());
                saveCooldownCounter = PRTime.Instance.CurrentRealSecond;
            }
            else
                activeSaveOperationFailed = true;

            activeSaveOperationCount = Math.Max(0, activeSaveOperationCount - 1);
            saveState = activeSaveOperationCount > 0
                ? GameSaveState.Saving
                : activeSaveOperationFailed
                    ? GameSaveState.Failed
                    : GameSaveState.Succeeded;
        }
    }

    private void CaptureLoadedSaveInfo(bool loadedExistingSave)
    {
        var saveInfo = loadedExistingSave
            ? GetStorageSaveInfoUtc()
            : (creationTimeUtc: (DateTime?)null, updateTimeUtc: (DateTime?)null);

        lock (saveDiagnosticsLock)
        {
            hasLoadedSave = loadedExistingSave;
            saveCreationTimeUtc = saveInfo.creationTimeUtc;
            lastSaveTimeUtc = saveInfo.updateTimeUtc;
        }
    }

    private (DateTime? creationTimeUtc, DateTime? updateTimeUtc) GetSuccessfulSaveInfo(bool succeeded)
    {
        return succeeded
            ? GetStorageSaveInfoUtc()
            : (null, null);
    }

    private (DateTime? creationTimeUtc, DateTime? updateTimeUtc) GetStorageSaveInfoUtc()
    {
        if (!(gameDataStorage is IGameDataStorageSaveInfo saveInfo))
            return (null, null);

        return (ToUtc(saveInfo.CreationDate), ToUtc(saveInfo.LastUpdateDate));
    }

    private static DateTime? ToUtc(DateTime? date)
    {
        if (!date.HasValue)
            return null;

        return date.Value.Kind == DateTimeKind.Utc
            ? date.Value
            : date.Value.ToUniversalTime();
    }

    public void AutoSaveHandler()
    {
        if (GetStorageSettings().EnabledAutoSave)
            StartCoroutine(AutoSave());
    }

    public void LoadDefaultControlSettings(bool overrideSettings, bool requiredSave = true)
    {
        if (overrideSettings)
            SetDefaultControlSettings();

        else if (gameSettings.Sensitivity == 0)
            SetDefaultControlSettings();

        if (requiredSave)
            StartSaveTask();
    }

    protected void SetDefaultControlSettings()
    {
        //gameSettings.Sensitivity = globalGameSettings.DefaultControlSettings.Sensitivity;
        //gameSettings.InvertHorizontalInput = globalGameSettings.DefaultControlSettings.InvertHorizontalInput;
        //gameSettings.InvertVerticalInput = globalGameSettings.DefaultControlSettings.InvertVerticalInput;
    }

    public IEnumerator AutoSave()
    {
        while (true)
        {
            yield return new WaitForSeconds(GetStorageSettings().AutoSaveSeconds);
            if (!isSaving)
                StartSaveTask();
        }
    }

    private void LoadingData()
    {
        gameSettings = gameDataStorage.GetGameSettings();
        projectData = gameDataStorage.GetProjectData();
    }

    public ProjectData GetProjectData()
    {
        return projectData ?? throw new InvalidOperationException($"{nameof(ProjectData)} is not initialized.");
    }

    public GameSettings GetGameSettings()
    {
        return gameSettings ?? throw new InvalidOperationException($"{nameof(GameSettings)} is not initialized.");
    }

    public GameStorageSettings GetStorageSettings()
    {
        return PRUnitySDK.Settings.GameStorage;
    }

    /// <summary>
    /// Событие старта подготовленной сцены.
    /// </summary>
    /// <param name="scene">Название сцены.</param>
    public void OnStartScene(string scene)
    {
        GameSettingsSession.Reset();
    }

    #endregion

    #region IReadySignalProvider

    protected readonly ReadySignal readySignal = new ReadySignal(typeof(GameManager));

    public IReadySignal ReadySignal => readySignal;

    #endregion
}
